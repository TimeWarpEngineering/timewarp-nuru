# JSON member schema: publish typed IDs and other parsable converter-bound types as strings

## Description

Found in Roslynk task 009 (fork PR TimeWarpEngineering/Roslynk#15, gap N3), 2026-10-23. Steven: "if using
timewarp contracts in Nuru would we ever experience this problem? If so file the Nuru issue but remember Nuru
is primarily for us, we meet our needs first."

`NuruJsonMemberSchema.Create` (`source/timewarp-nuru/capabilities/nuru-json-member-schema.cs`) exports a JSON
member's schema with `JsonSchemaExporter`. When a type has a custom `JsonConverter`, the exporter cannot know
what JSON the converter accepts, so it publishes that node with no `type` (`true` / `{}`). `--capabilities`
and the McpHost `inputSchema` then lose the shape.

**Our case:** timewarp-architecture's typed-ID generator
(`source/analyzers/timewarp-architecture-analyzers/generators/typed-id-source-generator.cs`) puts
`[JsonConverter(typeof(XJsonConverter))]` on every typed ID (`PrincipalId`, `CredentialId`,
`SiteSettingsId`, …). That converter writes the ID as a Guid string, and the type implements
`IParsable<T>`/`ISpanParsable<T>`. Any TimeWarp contract served through Nuru with a typed ID inside an object
or array member (for example `IReadOnlyList<PrincipalId>`, or a details object holding a `PrincipalId`) will
publish an untyped node. No request member does this yet: today typed IDs appear only in Responses. It is the
first thing contracts-through-Nuru will hit.

**Out of scope:** a general schema-override hook for arbitrary converters. One example is Roslynk's lenient
`MultiQueryOperationConverter`, an upstream pattern; Roslynk removed its type-level attribute instead. Do not
build for it unless solving our case gives it for free.

## Requirements

- In the schema transform (`TransformSchemaNode`, or after export), handle a node whose type has a custom
  converter (not a string-enum converter, which already exports correctly) and implements
  `IParsable<TSelf>`. Publish it as `{"type":"string"}`. That matches how Nuru already treats parsable types
  on the CLI, and the typed IDs' JSON form.
  - Add `"format":"uuid"` when it is cheap to detect a Guid-backed ID, for example a public `Guid Value`
    property or the architecture generator's marker. Skip it if detection would need a dependency on
    timewarp-architecture. Nuru must not reference it.
  - Keep `[Description]` and the nullable `["string","null"]` handling.
- Check how a top-level typed-ID **option** (not nested) appears in `--capabilities` and the McpHost schema,
  and how it binds through `--json-args`/MCP arguments. Fix it the same way if it is untyped, or say in
  Results that it is fine.
- Tests: nested in an object, in an array, nullable, and top-level. Use a typed ID shaped like the
  architecture generator's output (a converter writing a Guid string plus `IParsable`), written in the test
  project. Do not reference timewarp-architecture.
- Docs: the agent-invocation / capabilities docs where JSON member schemas are described.
- Ship in a Nuru prerelease.

## Goal (implementer restatement)

A JSON member's published schema (`--capabilities` `schema`, McpHost `inputSchema`) loses its shape where a
type carries its own `[JsonConverter]`: `JsonSchemaExporter` writes `true` for that node, and an array of such
values gets `{"type":"array"}` with no `items`. Typed IDs (custom converter writing a Guid string, plus
`IParsable<TSelf>`) are the case we hit first.

Acceptance:

- A type with a type-level custom `[JsonConverter]` (not the string-enum converter) that implements
  `IParsable<TSelf>` publishes `{"type":"string"}` wherever it appears in a JSON member: as the member root,
  nested in an object, as array/collection items, as dictionary values.
- `"format":"uuid"` is added when the type has a public `Guid Value` property. No timewarp-architecture
  dependency.
- `[Description]` on the property survives; nullable (`T?` struct, nullable reference property, nullable
  root member) publishes `["string","null"]`.
- Detection stays AOT/trim clean: the library is `IsAotCompatible`, so the generator finds these types at
  compile time and passes them to `NuruJsonMemberSchema.Create`; no runtime reflection over interfaces.
- The top-level typed-ID option is checked (capabilities, McpHost schema, `--json-args`/MCP binding) and fixed
  or reported in Results.
- Tests cover nested, array, nullable, top-level with a test-local typed ID. Docs describe the rule.

## Checklist

- [x] Parsable converter-bound types publish as string (uuid when cheap)
- [x] Top-level typed-ID option checked
- [x] Tests
- [x] Docs
- [ ] Released in a Nuru beta (next prerelease; a release is a separate explicit ask, changelog entry under Unreleased)

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- 2026-11-14 implementer-claude (ganda task work): merged origin/master (kanban-only commit) into the branch,
  implemented generator-side detection + runtime transform, fixed the top-level `--json-args` kind gate,
  added mcphost-08 tests, docs and changelog. Audit boy-scout: `ganda repo audit --fix` set the VS Code
  `window.title` (`vscode-window-icon` advisory; `.vscode/settings.json` is gitignored, so no committed change).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-13T23:37:27Z
- Review oracle round 1: Grok 4.7 (grok, vendor xai), body tw-implementation-review, effort 2, roster general, turn cap 120 (cursor session uncapped; no --max-turns flag), session not reported, disposition clean — 2026-11-14. No findings. The restatement matches the task; no misread. See `review/round-1/merged.md` and `review/disposition.md`.
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-13T23:43:34Z

## Review

- Round 1: Done

## Notes

- Consumers: TimeWarp contracts with typed IDs served through Nuru (timewarp-architecture apps, later).
- Roslynk's `multi_query` is not a consumer. Its type-level converter was removed in task 009.

## Results

### What changed

- **Generator** (`operation-endpoint-extractor.json-string-types.cs`, new): for each `[Operation]` JSON member
  the extractor walks the member type (type arguments, array elements, public instance properties of
  non-framework types, base types included) and records each type that has its own `[JsonConverter]`
  (string-enum converter excluded) and implements `IParsable<TSelf>`. `IsUuid` is set when it has a public
  instance `Guid Value`. Carried on `OptionDefinition.JsonStringTypes` (`JsonStringType` record, equatable).
- **Emitter** (`capabilities-emitter.cs`): passes them to `NuruJsonMemberSchema.Create(..., stringTypes: [...],
  uuidTypes: [...])`; members with none emit the old call unchanged.
- **Runtime** (`nuru-json-member-schema.cs`): `Create` gained optional `stringTypes`/`uuidTypes`. The
  transform rewrites an untyped node (`true` or a typeless object) of those types to `{"type":"string"}`
  (+ `"format":"uuid"`), `["string","null"]` for `Nullable<T>` or a nullable reference property, and fills
  missing `items` / `additionalProperties` for collections and dictionaries of them (the exporter omits
  them). `[Description]` runs after, so it is kept. Done at compile time because `timewarp-nuru` is
  `IsAotCompatible` + trim analyzed: no `GetInterfaces()` reflection.
- **Top-level typed-ID option (checked, was broken, fixed)**: an `[Operation]` property of a typed-ID type
  is not a scalar, so it is a JSON member (`type` `json Ns.PrincipalId`). Before: its `schema` was `true`
  (untyped) in `--capabilities` and McpHost, and the `--json-args`/MCP binder's kind gate accepted only an
  object or array, so **every** value was rejected (`wrong JSON type. Expected json`). Now the schema is
  `{"type":"string","format":"uuid"}` and the kind gate (`json-args-emitter.validation.cs`) takes a JSON
  string for a member whose own type is a typed ID (`OptionDefinition.IsJsonStringValue`; expected type
  reads `string`). Argv still rejects JSON members (`--Owner is JSON. Pass it with --json-args.`); accepting
  a typed ID on argv is a separate change, not done here.
- **Tests**: `tests/timewarp-nuru-mcphost-tests/mcphost-08-typed-id-schema.cs` (in CI multi-mode via the
  mcphost glob): test-local Guid-backed `TidPrincipalId` (record struct, converter writing the Guid string,
  `ISpanParsable`) and string-backed reference `TidSlug`. Covers top-level (capabilities option schema +
  McpHost inputSchema, non-null and nullable), array items, nested object with `[Description]`, nullable
  struct, nullable reference without format, nested list, dictionary values, string-enum unchanged, and
  binding through `--json-args` and `NuruMcpToolInvoker` (good value, object rejected with `Expected
  string`, unparsable rejected as a bind error).
- **Docs**: `documentation/user/features/agent-invocation.md` (JSON member schema section),
  `documentation/user/features/neutral-contracts.md` (derivation table), `changelog.md` (Unreleased / Fixed).
- Out of scope, unchanged: other custom converters (e.g. Roslynk-style lenient converters) still publish
  untyped; property-level `[JsonConverter]` is not detected.
- Release: not cut. Version stays `4.0.0-beta.13` (released); ships in the next Nuru beta when one is asked for.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | pass, 0 warnings, 0 errors |
| Test | `./bin/dev test` (after `ganda runfile cache --clear`) | pass: multi-mode 2208 total, 2202 passed, 6 skipped, 0 failed; standalone 47 files, 0 failed |
| Audit | `ganda repo audit` | pass, 1 advisory (`vscode-window-icon`, gitignored `.vscode/settings.json`); `ganda repo audit --fix` fixed it, all checks pass |

### How to validate

Smoke:

```bash
ganda runfile cache --clear
cd tests/timewarp-nuru-mcphost-tests && dotnet run mcphost-08-typed-id-schema.cs
```

Expect: `Passed: 3` (`Should_publish_a_top_level_typed_id_as_a_uuid_string`,
`Should_publish_typed_ids_nested_in_arrays_objects_and_dictionaries`,
`Should_bind_typed_ids_through_json_args_and_mcp_arguments`). The `Owner` schema is
`{"type":"string","format":"uuid"}`, `Members.items` is the same, `Details.properties.Backup` is
`["string","null"]`, and `tid-assign --json-args '{"Owner":"<guid>",...}'` exits 0.

### Review

Round 1 (2026-11-14): tw-implementation-review, effort 2, roster general, Grok 4.7, turn cap 120 (cursor uncapped; no `--max-turns` flag), session not reported. Disposition clean. Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0. The restatement matches the task; no misread. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
