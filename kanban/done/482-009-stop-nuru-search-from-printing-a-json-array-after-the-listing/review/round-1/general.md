# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit `cb2561e3` (S-1 search listing stdout)

## Summary

`SearchQuery` is `IQuery<Unit>`, and every handler path returns `default`, matching `IndexListQuery`. `EmitResultOutput` maps `Unit` to `OutputStrategy.None` and emits no serializer call. The rebuilt `NuruGenerated.g.cs` stores `global::TimeWarp.Mediator.Unit result` and returns `Environment.ExitCode` for the search route. No `--json` (or other machine-readable) option exists on the search endpoints, so none was removed. `search-05-search-query-output` seeds `index.db` under a temporary `HOME` / `USERPROFILE` (the path `DatabasePath.GetIndexPath` uses) and runs `TimeWarp.Nuru.Search.dll`. The hit stdout ends at the description line; the miss stdout is exactly `No results found.`. `InternalsVisibleTo` matches the runfile assembly name, and `tests/ci-tests/Directory.Build.props` already compiles `timewarp-nuru-search-tests/**/*.cs` and references the search project. `dotnet run tests/timewarp-nuru-search-tests/search-05-search-query-output.cs` passed 2/2.

## Issues

No issues.
