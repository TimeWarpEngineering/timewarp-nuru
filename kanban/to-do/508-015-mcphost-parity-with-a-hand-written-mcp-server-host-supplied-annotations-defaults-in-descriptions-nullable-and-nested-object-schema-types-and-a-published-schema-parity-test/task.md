# McpHost parity with a hand-written MCP server: host-supplied annotations, defaults in descriptions, nullable and nested-object schema types, and a published-schema parity test

## Description

Roslynk task 004 (TimeWarpEngineering/Roslynk PR #6, 2026-10-19) was the first attempt to host a real
product's MCP surface on `TimeWarp.Nuru.McpHost` 4.0.0-beta.2 instead of a hand-written MCP server. It
kept its hand-written server (`WithRoslynkTools` over the ModelContextProtocol SDK) because the tools
McpHost derives from the same 29 `[Operation]` contracts do not match the tools Roslynk already publishes,
and Roslynk's constraint is byte-identical schema and text for existing MCP clients. The probe derived
McpHost's tools from the command's own catalog (`NuruMcpServer.CreateTools` →
`NuruMcpToolCatalog.CreateTools`) and compared them field by field with the published ones:

| Published field | McpHost equal | Difference |
| --- | --- | --- |
| Tool name | 29/29 | none |
| Tool description | 29/29 | none |
| Input schema | 0/29 | `additionalProperties: false` added; property order is Nuru's, not declaration order |
| Annotations | 0/29 | `title`, `destructiveHint: false/true` and `openWorldHint` dropped |
| Property description | 69/139 | 52 defaults move into JSON `default`; the hand-written server appends them to the description |
| Property type | 128/139 | `multi_query.operations` (an object array, JSON-only member) becomes `string`; nullable members lose `null` |

Text: McpHost returns Nuru's rendering, which 508-011 will make verbatim; that part is already tracked.

The schema side is this task. McpHost should be able to publish what a careful hand-written server
publishes, so a product can switch hosts without its clients seeing a different `tools/list`.

## Requirements

- Annotations: let the app supply per-tool `title`, `destructiveHint`, `openWorldHint` (and keep the
  `EndpointKind`-derived `readOnlyHint`/`idempotentHint`). Prefer reading them from the neutral contract
  (`McpOperationBinding` in TimeWarp.Contracts, filed there if a property is missing) with a host-side
  override hook (`NuruMcpHostOptions`) for apps that do not own their contracts.
- Defaults: an option to render a member's default into the property description (`Default: ...`, the
  ModelContextProtocol SDK convention Roslynk's `WithRoslynkTools` follows) instead of, or in addition to,
  the JSON `default` keyword. Document which one clients honour.
- Types: a nullable member publishes `"type": ["<t>", "null"]` (or `anyOf`) as the SDK does; a non-scalar
  member (object array such as `multi_query.operations`, the 508-010 wire contract has the same case)
  publishes the member's real JSON schema from the JSON context, not `string`.
- Schema shape options: `additionalProperties: false` and property ordering (declaration order) become
  options with documented defaults, so a host can match an existing published schema exactly.
- Parity test: a test that builds the ModelContextProtocol SDK's tool list for a sample with `[Operation]`
  requests (mirroring Roslynk's 002 shape: camelCase members, `[Description]`, `[DefaultValue]`, nullable
  and object-array members, annotations) and asserts McpHost's `tools/list` is deep-equal. Use Roslynk's
  `Morris.Roslynk.Cli.Tests/CapabilitiesContractTests.cs` and the 004 kitchen's probe as the reference.
- Docs: the McpHost page lists the knobs and states the parity guarantee; `neutral-contracts.md` says how a
  contract carries annotations.

## Non-goals

- Verbatim text rendering and the problem renderer (508-011).
- Group handling (508-012 / 508-014 / architecture 388).
- Changing Roslynk; the Roslynk host switch is a Roslynk follow-up once this and 508-011 are released.

## Checklist

- [ ] Annotation source decided (contract property vs host options) and implemented
- [ ] Defaults-in-description option
- [ ] Nullable and non-scalar member schema types
- [ ] `additionalProperties` / ordering options
- [ ] Parity test green; McpHost docs updated
- [ ] Roslynk follow-up noted in Results (switch `roslynk stdio` to McpHost when 508-011 is also out)

## Depends on

- 508-004

## Session

- Created: 2532391 (2026-10-19) by the Roslynk cockpit from the Roslynk 004 transport decision
  (PR #6 body, section "Transport decision").

## Notes

- Related: Roslynk 003 (CLI, merged PR #5), Roslynk 004 (host unification, PR #6), nuru 508-004 (McpHost,
  done), 508-010 (daemon wire contract), 508-011 (verbatim renderer), 507 (reverse bridge gate).
