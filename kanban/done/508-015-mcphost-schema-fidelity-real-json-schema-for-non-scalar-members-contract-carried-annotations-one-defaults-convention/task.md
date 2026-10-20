# McpHost schema fidelity: real JSON schema for non-scalar members, contract-carried annotations, one defaults convention

## Description

Rewritten 2026-10-20. The first version of this task asked McpHost to reproduce a hand-written MCP
server's `tools/list` byte for byte (options for `additionalProperties`, property order,
defaults-in-description, a deep-equal parity test), so Roslynk could keep its old schema. Steven decided
the fork does not keep that constraint: MCP clients are models that read `tools/list` on every connect, and
the goal is the cleanest Nuru architecture. Only the differences that are real defects stay.

Roslynk task 004 (PR #6) compared McpHost 4.0.0-beta.2's derived tools for its 29 `[Operation]` contracts
with its published ones. Of the differences found, these are defects for any Nuru app:

- **Non-scalar members publish as `string`.** `multi_query.operations` (an object array, bound as one JSON
  value via `--json-args`) becomes `"type": "string"`, so a model has to send JSON inside a string. A
  non-scalar member must publish its real JSON schema (array/object, item schema, required members, enum
  values), derived from the same type the binder deserializes into.
- **Annotations can only come from `EndpointKind`.** `readOnlyHint`/`idempotentHint` are derived; `title`,
  `destructiveHint` and `openWorldHint` cannot be set. Harnesses use these hints for permission prompts and
  auto-approval. Let a contract carry them (prefer `McpOperationBinding` in TimeWarp.Contracts; file the
  timewarp-architecture task if a property is missing) with a `NuruMcpHostOptions` override for apps that do
  not own their contracts.
- **Defaults convention.** McpHost puts defaults in the JSON `default` keyword; the ModelContextProtocol SDK
  convention some servers follow appends `Default: ...` to the description because not every harness shows
  `default` to the model. Check what Claude Code, Codex and Grok actually present to the model, pick one
  convention (or both), and document it. No per-app option unless the check says one is needed.
- **Nullable members** may keep `null` in their type if the binder accepts an explicit `null`; otherwise
  leave them as is and document that omitting means default.

Dropped from the first version: `additionalProperties`/ordering options and the deep-equal parity test
against a hand-written server.

## Requirements

- A sample with an object-array member and a nested object member publishes their real schemas; a
  `tools/call` with that JSON (not a string) binds and runs.
- A contract with `destructiveHint`, `openWorldHint` and `title` publishes them; the host override wins when
  set.
- The defaults decision is recorded with the harness evidence, implemented and tested.
- Docs: McpHost page (schema derivation, annotations, defaults); `neutral-contracts.md` (how a contract
  carries annotations).

## Goal (implementer restatement)

Make McpHost's `tools/list` honest about what `tools/call` accepts, and let a contract say how a harness should
treat its tool.

1. A non-scalar member (bound as one JSON value) publishes the JSON schema of the type the binder deserializes
   into, not `string`. Today the binder rejects a string for such a member, so a model following the schema
   always fails. Source of truth: the generated `NuruJsonMemberContext` type info (same naming, required and
   enum rules as the deserializer), carried in the catalog so `--capabilities` and McpHost agree and McpHost
   stays a pure function of the catalog.
2. A contract carries `title`, `destructiveHint` and `openWorldHint` (plus the kind-derived read-only and
   idempotent hints). TimeWarp.Contracts has no place for them yet (`McpOperationBinding` is `ToolName` only),
   so: BCL `[DisplayName]` for the title, Nuru `[Destructive(bool)]` and `[OpenWorld(bool)]` for the hints, an
   architecture task for the Contracts home, and a `NuruMcpHostOptions` per-tool override that wins.
3. Pick a defaults convention from what Claude Code, Codex and Grok actually show the model; implement, test
   and document it.
4. Decide nullable members from what the binder does with an explicit `null`.

Acceptance:
- A sample with an object-array member and a nested object member lists their real schemas, and a
  `tools/call` passing that JSON (not a string) binds and runs. Test covers it.
- A contract with destructive, open-world and title hints publishes them; a host override wins. Test covers it.
- Defaults decision recorded with harness evidence, implemented, tested.
- Nullable decision recorded and tested.
- Docs: mcp-hosting.md (schema derivation, annotations, defaults), neutral-contracts.md (how a contract carries
  annotations).
- Build, CI tests and `ganda repo audit` green; committed and pushed on the task branch.

## Checklist

- [x] Non-scalar member schemas
- [x] Annotations from the contract plus host override (architecture task filed: timewarp-architecture 391)
- [x] Defaults convention decided from harness evidence and implemented
- [x] Nullable handling decided
- [x] Tests and docs

## Depends on

- 508-004

## Session

- Created: 2532391 (2026-10-19) by the Roslynk cockpit from the Roslynk 004 transport decision.
- Rewritten: 2532391 (2026-10-20) after Steven dropped byte-identical MCP parity for the Roslynk fork.
- Implemented: 2026-10-20 by implementer-claude under `ganda task work`. Filed timewarp-architecture 391
  (Contracts home for the hints). `ganda kanban create-remote` failed (no `kanban-manager` agent), and
  `ganda kanban reserve/claim --repo timewarp-architecture` run from this worktree reserved and claimed id 512
  in timewarp-nuru instead. 512 was released with `--abandon`, and its untracked worktree and local branch
  were removed (nothing was pushed). 391 was then reserved, claimed and published from the
  timewarp-architecture checkout. The commit-time audit flagged `runfile-executable` on the new
  `mcphost-04-schema-fidelity.cs`; `ganda repo audit --fix` set its executable bit.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T12:27:56Z
- Review: grok session `01a15948-5cd1-7ed2-82e2-28012ac95836` (2026-10-20), effort 3, roster general. Round 1 disposition clean.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-20T12:42:55Z

## Results

### Decisions

- **Non-scalar schema source.** The generated capabilities code calls `NuruJsonMemberSchema.Create`
  (core, new). That runs `JsonSchemaExporter` over the `NuruJsonMemberContext` type info the `--json-args`
  binder deserializes with. The catalog carries the result as `OptionCapability.Schema` (`schema` in
  `--capabilities`), so McpHost stays a pure function of the catalog. Names, `required` (C# `required` and
  constructor parameters), nested nullable references, string-enum converters and nested `[Description]`
  all match the deserializer. The member root has `null` only when the member is nullable, because the
  binder rejects `null` otherwise. A catalog with no `schema` falls back to `["object","array"]`, not
  `string`. Before this change the binder rejected the string the schema asked for.
- **Annotations.** Contracts beta.23 `McpOperationBinding` has `ToolName` only, so the hints live on the
  request class:
  - `[System.ComponentModel.DisplayName]` gives the title (BCL, like member `[Description]`).
  - `[Destructive(bool = true)]` (existing attribute, now tri-state) and the new `[OpenWorld(bool = true)]`.
  - The catalog gains `title` and `openWorld`, and `destructive` can now be `false`.
  - McpHost sets `Tool.Title` and `annotations.title`/`destructiveHint`/`openWorldHint`.
  - `NuruMcpHostOptions.ToolAnnotations[toolName]` overrides per hint (non-null wins).
    `NuruMcpToolCatalog.MergeAnnotations` is public.
  - timewarp-architecture 391 asks Contracts for a neutral home.
- **Defaults: both.** Keep the JSON `default` keyword and append `Default: <json>.` to the description.
  Harness evidence, checked 2026-10-20:
  - Claude Code 2.1.304 passes `inputSchema` through as `input_schema`.
  - Codex (`openai/codex` main `b2aa07db`, `codex-rs/tools/src/mcp_tool.rs` →
    `json_schema/types.rs` `JsonSchema`) keeps `type`, `description`, `enum`, `items`, `properties`,
    `required`, `additionalProperties` and the combinators, and drops `default` and `format`.
  - Grok Build 1.0.64 `search_tool` returns the schema as written.

  Only Codex needs the description sentence, but it costs a few words and needs no option. Flags now
  publish `default: false`.
- **Nullable.** The binder accepts an explicit `null` for nullable slots and binds `null`. So a catalog type
  ending in `?` keeps `"null"` (unchanged), and a nullable JSON member's schema root gets `"null"`. Omitting
  a key still means default. A derived `string?` option stays `string`, because its catalog type has no
  `?`. Documented.

### Files

- Core: `capabilities/capabilities-response.cs` (`EndpointCapability.OpenWorld`, `.Title`,
  `OptionCapability.Schema`), `capabilities/nuru-json-member-schema.cs` (new),
  `attributes/destructive-attribute.cs` (tri-state), `attributes/open-world-attribute.cs` (new).
- Generator: `models/route-definition.cs` (`Destructive` bool?, `OpenWorld`, `Title`),
  `extractors/endpoint-extractor.cs` (reads the attributes), `emitters/capabilities-emitter.cs` (hints
  and `Schema`; trailing commas simplified).
- McpHost: `nuru-mcp-tool-catalog.cs` (JSON member schema, defaults in description, annotations, merge),
  `nuru-mcp-host-options.cs` (`ToolAnnotations`), `readme.md`.
- Sample: `samples/endpoints/02-calculator-contracts` (nested `Format` object, `required` `Label`,
  `[DisplayName]`, `[OpenWorld(false)]`, `mcp` route).
- Tests: `tests/timewarp-nuru-mcphost-tests/mcphost-04-schema-fidelity.cs` (new, 6 tests), and the
  `mcphost-01` description expectation.
- Docs: `mcp-hosting.md` (object and array members, defaults with the evidence, nullable members,
  annotations and override), `neutral-contracts.md` (Tool hints section, JSON member `schema`),
  `agent-invocation.md` (`schema`, hint members table), `overview.md`, `skills/tw-nuru/SKILL.md`,
  `changelog.md`.

### Verification

- `./bin/dev build`: succeeded.
- `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs`: 4229 tests, 0 failed
  (multi-mode and standalone), including McpHostSchemaFidelity 6/6.
- McpHost runfiles one by one: `mcphost-01` 16/16, `mcphost-02` 2/2, `mcphost-03` 6/6, `mcphost-04` 6/6.
- `./bin/dev verify-samples`: 66/66.
- `ganda repo audit`: passes all checks (after `--fix` set the executable bit on `mcphost-04`).

### How to validate

Smoke:

```bash
dotnet run samples/endpoints/02-calculator-contracts/calculator-contracts.cs -- --capabilities
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-04-schema-fidelity.cs
```

Expect:

- In `--capabilities`, the `calc total` endpoint has `"openWorld": false` and `"title": "Total numbers"`.
- Its `Notes` option has `"schema": {"type":"array","items":{"type":"object",...,"required":["Label"]}}`.
- Its `Format` option has `"schema": {"type":["object","null"],...}`, with `Decimals` (integer) and
  `Prefix` described from `[Description]`.
- The test run prints `Total: 6 Passed: 6`. That includes the stdio call
  `calc_total {"Values":[1,2],"Notes":[{"Label":"tax","Amount":1.5}],"Format":{"Decimals":1,"Prefix":"$"}}`
  returning `total:$4.5|average:1.5|notes:1`. It also includes a string-encoded `Operations` value being
  rejected with "wrong JSON type".

### Review

- Rounds: 1. Roster: general. Effort: 3 (1199 lines changed).
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0.
- Disposition: **clean**. No wontfix and no escalation. The implementer restatement matches the rewritten task.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

- Related: 508-011 (verbatim text), 508-016 (concurrent calls), Roslynk task "move the daemon, stdio and CLI
  onto the Nuru served app".
- Review: `review/disposition.md` (round 1, clean).
