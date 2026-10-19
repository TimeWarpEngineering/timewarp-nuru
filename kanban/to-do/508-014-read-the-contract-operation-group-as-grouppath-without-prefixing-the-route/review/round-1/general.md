# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/508-014-read-the-contract-operation-group-as-grouppath-wit` vs `master` (commit `1b8041d6`)

## Summary

`[Operation(Group)]` from TimeWarp.Contracts 2.0.0-beta.23 is read as metadata-only `groupPath`. A non-blank group is split on `.`, space, or tab, the route pattern and command stay the operation name, and a `[NuruRouteGroup]` prefix plus `[NuruCliBinding(Group)]` are not applied on top. A blank group keeps the previous prefix order. `FilterByGroup` uses the same split, so `--group-filter` and McpHost `GroupFilter` match that path. McpHost tool names stay the pattern's literal words, which keeps `get_symbol` from becoming `symbols_get_symbol`. Route-group patterns already include their prefix (`FullPattern`), so dropping the old groupPath prepend does not rename those tools. `nuru search` no longer prints `groupPath` in front of a pattern that already contains the route prefix.

`## Goal (implementer restatement)` matches the task: pin 2.0.0-beta.23, metadata `groupPath`, no route or command prefix, blank keeps today's order, one filter, no second map, Design region updated. No misread.

Checked against `OperationAttribute.Group` in the 2.0.0-beta.23 XML docs (purpose metadata, `.` or space, blank is ungrouped, hosts must not prefix the route). Re-ran the three validation files after clearing the runfile cache: derivation 40/40, mcphost-01 16/16, search-02 5/5.

## Issues

<!-- None. -->
