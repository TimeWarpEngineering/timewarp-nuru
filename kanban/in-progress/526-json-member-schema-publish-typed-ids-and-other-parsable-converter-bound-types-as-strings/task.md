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

## Checklist

- [ ] Parsable converter-bound types publish as string (uuid when cheap)
- [ ] Top-level typed-ID option checked
- [ ] Tests
- [ ] Docs
- [ ] Released in a Nuru beta

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)

## Notes

- Consumers: TimeWarp contracts with typed IDs served through Nuru (timewarp-architecture apps, later).
- Roslynk's `multi_query` is not a consumer. Its type-level converter was removed in task 009.
