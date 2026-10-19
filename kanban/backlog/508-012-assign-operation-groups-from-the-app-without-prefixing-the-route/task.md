# Assign operation groups from the app without prefixing the route

## Description

**Shelved (Steve, 2026-10-19).** The group is declared once on the contract instead:
`[Operation(..., Group = "...")]` in TimeWarp.Contracts (timewarp-architecture task 388). TimeWarp controls
its own contracts, and an app-side map keyed by operation name would be a second source of truth. Reopen only
when a real consumer must group contracts it does not control; it would then layer an override on top of the
contract group. The "group never prefixes the route" requirement moves to the Nuru follow-up that reads the
catalog group as `groupPath`.

Filed from 508-006 (Roslynk CLI finding 4).

`groupPath` comes only from a base `[NuruRouteGroup]` or `[NuruCliBinding(Group = ...)]` on the request itself. The group prefix is also prepended to the route pattern (for example `symbols get_symbol`).

A CLI that compiles contracts it does not own (linked sources) cannot group them. Grouping also changes the route literal away from the MCP tool name.

Wanted: a group assignment the app declares, which sets `groupPath` for `--group-filter` and the catalog without prefixing the pattern. The shape could be a builder call or an assembly attribute keyed by request type. Another option is a `Group` member on `TimeWarp.Contracts.CliOperationBinding`.

The extractor is `source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs` (group info). McpHost (508-004) reads the same group path.

## Checklist

- [ ] Pick the declaration shape
- [ ] Carry `groupPath` without changing the pattern
- [ ] `--group-filter` honours it
- [ ] Catalog honours it
- [ ] Tests
- [ ] Docs

## Session

- Created: 4094467 (2026-10-19)
- 2026-10-19: shelved to backlog per Steve; superseded by timewarp-architecture 388 (contract-side group).

## Notes

Keep McpHost (508-004) in step: it reads the same group path.
