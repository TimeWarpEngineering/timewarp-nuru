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

## Goal (implementer restatement)

Pin TimeWarp.Contracts `2.0.0-beta.23` (published; it carries `OperationAttribute.Group`) and teach
the operation extractor to read `[Operation(Group = "...")]` as metadata.

Acceptance:

- A non-blank `Group` splits on `.` or space into the capabilities `groupPath`. The route pattern,
  the command a user types, and the McpHost tool name stay the operation name. No
  `[NuruRouteGroup]` or `[NuruCliBinding(Group)]` prefix is added on top of it.
- A blank or omitted `Group` keeps today's behavior: `[NuruRouteGroup]` prefix wins, then
  `[NuruCliBinding(Group)]`, else ungrouped.
- `--capabilities --group-filter` matches that path whether the filter is dotted or spaced.
- McpHost `GroupFilter` publishes the operation by the same `groupPath` (test), with no second map.
- The extractor Design region describes the new precedence.

## Checklist

- [x] Wait for the TimeWarp.Contracts release that contains `OperationAttribute.Group`, then pin it
- [x] Non-blank `[Operation(Group = ...)]` becomes `groupPath` and does not change the route pattern or the command name
- [x] Blank or omitted `Group` keeps today's `[NuruRouteGroup]` / `[NuruCliBinding(Group)]` behavior
- [x] `--group-filter` matches dotted and spaced segments of that path
- [x] McpHost (508-004) tool filtering sees the same path, with a test and no second map
- [x] Extractor Design region updated

## Session

- Created: 3433621 (2026-10-19)
- Filed: timewarp-architecture 388 (2026-10-19), the contract-side group. 508-012 stays shelved.
- 2026-10-20 implement (claude, headless task work): TimeWarp.Contracts 2.0.0-beta.23 is on NuGet and
  its XML docs carry `OperationAttribute.Group`, so the block is cleared. Pinned it and implemented.
  CI suite 4142 tests: 4130 passed, 12 skipped, 0 failed. `ganda repo audit` passed with no fixes needed.

## Notes

- Parent: 508. Related: 508-004 (McpHost reads `groupPath`), 508-012 (shelved app-side groups), timewarp-architecture 388.
- Roslynk 003-001 declares the five groups on the operation contracts after the same package ships. This task is what makes those groups show up as `groupPath`.

## Results

`[Operation(Group = ...)]` is now read as metadata-only `groupPath`.

- `Directory.Packages.props`: TimeWarp.Contracts `2.0.0-beta.21` to `2.0.0-beta.23`.
- `RouteDefinition.GroupPath` (new, metadata-only) plus `CatalogGroupPath` (that path, else the
  `GroupPrefix` words). The capabilities emitter writes `groupPath` from `CatalogGroupPath`.
  `GroupPrefix`, the route matcher, aliases, and help are unchanged for operation groups.
- Extractor: `ResolveRouteIdentity` reads `Operation.Group`. A non-blank group is split on `.`, space,
  or tab into `GroupPath` and drops the `[NuruRouteGroup]` prefix and aliases. The group type hierarchy
  stays, so `DiscoverEndpoints(typeof(...))` still includes it. `[NuruCliBinding(Group)]` is not
  applied. A blank group runs the old `[NuruRouteGroup]` then `[NuruCliBinding]` path. The Design
  region is updated.
- `CapabilitiesResponse.FilterByGroup` splits the filter on `.` or space (as `nuru search` already
  did), so `--group-filter a.b` and `"a b"` match the same path. A blank filter keeps every endpoint.
- McpHost `GetToolName` no longer inserts `groupPath` words. The catalog pattern already holds any
  route-group prefix, and an operation group must not rename the tool (`get_symbol` stays `get_symbol`).
  The filter is the existing `FilterByGroup`. There is no second map.
- Boy-scout fix: `nuru search` result headers printed `groupPath` before a pattern that already
  holds the route-group prefix, so grouped routes showed it twice. The header now prints the pattern.
  The search-02 test now asserts that.
- Docs: `neutral-contracts.md` Groups order, `mcp-hosting.md` tool name and filter split, changelog.

Not done (out of scope): help output still groups sections by route prefix only. Operation groups do
not become help categories.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs
dotnet run tests/timewarp-nuru-search-tests/search-02-version-formatter.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect:

- derivation: 40/40 pass, including `Should_route_an_operation_group_by_the_operation_name_only`
  (`t514dotted` runs, `t514ops t514dotted` exits 1),
  `Should_let_the_operation_group_win_over_route_group_and_cli_binding`,
  `Should_keep_cli_binding_group_when_the_operation_group_is_blank`,
  `Should_list_the_operation_group_as_group_path` (`["t514ops","symbols"]` from both dotted and spaced),
  and `Should_filter_operation_groups_by_dotted_or_spaced_segments`.
- mcphost-01: 16/16 pass, including `Should_filter_by_operation_group_without_renaming_the_tool`
  (`GroupFilter` `mh-symbols`, `mh-symbols.read`, and `MH-SYMBOLS READ` each publish only `mh-get-symbol`).
- search-02: 5/5 pass. CI: 0 failed.
