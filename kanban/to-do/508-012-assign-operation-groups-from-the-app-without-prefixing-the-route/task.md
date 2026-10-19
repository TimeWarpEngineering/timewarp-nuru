# Assign operation groups from the app without prefixing the route

## Description

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

## Notes

Keep McpHost (508-004) in step: it reads the same group path.
