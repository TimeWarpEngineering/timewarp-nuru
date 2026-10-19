# Read the contract operation group as groupPath without prefixing the route

## Description

timewarp-architecture 388 puts an optional purpose group on `[Operation]`:

`[Operation("get_symbol", Group = "symbols")]`

`Group` is metadata. Blank (`IsNullOrWhiteSpace`) means ungrouped. Segments are separated by `.` or space, the same split `--group-filter` already uses. The group is not a route prefix and not a command prefix: the CLI command stays the operation name (`get_symbol`, not `symbols get_symbol`).

This task teaches the Nuru operation extractor to read that contract group as `groupPath`. 508-012 (app-side group map) stays shelved. Do not add a second map keyed by operation name.

The extractor is `source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs`. Today a containing `[NuruRouteGroup]` wins, otherwise `[NuruCliBinding(Group)]`, otherwise the endpoint is ungrouped. `CliOperationBinding` stays pattern-only.

For an `[Operation]` endpoint whose `Group` is non-blank:

- That value is `groupPath`.
- The route pattern and the command name stay the operation name. Do not prepend the group.
- Do not also apply `[NuruCliBinding(Group)]` or a `[NuruRouteGroup]` prefix on top of it. The contract group wins.

When `Operation.Group` is blank, keep today's `[NuruRouteGroup]` then `[NuruCliBinding(Group)]` behavior so existing Nuru routes do not change.

McpHost (508-004, done) already filters tools from the same `groupPath`. This task does not add a second filter. A test shows an operation group is the path 508-004 reads.

Blocked until TimeWarp.Contracts that contains `OperationAttribute.Group` is published. That is timewarp-architecture 388, source `2.0.0-beta.23` (NuGet latest at filing is `2.0.0-beta.22`). Pin that package in the same commit that reads the property.

## Checklist

- [ ] Wait for the TimeWarp.Contracts release that contains `OperationAttribute.Group`, then pin it
- [ ] Non-blank `[Operation(Group = ...)]` becomes `groupPath` and does not change the route pattern or the command name
- [ ] Blank or omitted `Group` keeps today's `[NuruRouteGroup]` / `[NuruCliBinding(Group)]` behavior
- [ ] `--group-filter` matches dotted and spaced segments of that path
- [ ] McpHost (508-004) tool filtering sees the same path, with a test and no second map
- [ ] Extractor Design region updated

## Session

- Created: 3433621 (2026-10-19)
- Filed: timewarp-architecture 388 (2026-10-19), the contract-side group. 508-012 stays shelved.

## Notes

- Parent: 508. Related: 508-004 (McpHost reads `groupPath`), 508-012 (shelved app-side groups), timewarp-architecture 388.
- Roslynk 003-001 declares the five groups on the operation contracts after the same package ships. This task is what makes those groups show up as `groupPath`.
