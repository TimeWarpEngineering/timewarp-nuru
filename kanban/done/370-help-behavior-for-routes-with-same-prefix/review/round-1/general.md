# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** commit `019c7ecd2566` — `RouteHelpEmitter.EmitSharedPrefixHelpChecks`, the skip in `EmitPerRouteHelpCheck`, the call order in `EmitMethodBody`, `RouteMatcherEmitter` passing the covered set, per-route help docs, and `help-01-per-route-help.cs`.

## Summary

`GetLiteralPrefix` is unchanged: group-prefix words, then pattern literals, stopping at the first parameter or option. Routes with the same prefix list are grouped with an ordinal key. A prefix with one route still emits the single-route check beside that route's matcher. A prefix with two or more emits one exact list pattern (`literals` plus `--help` or `-h`) before group-summary checks, prints each route with the existing per-route layout, highest `ComputedSpecificity` first, and returns 0. Those routes are then skipped by reference so the same arguments are not handled again. `deploy status` and `deployment` are different literal sequences. A `worktree`-style group, where each subcommand adds another literal, still reaches the group summary. Re-ran `help-01-per-route-help.cs` (19 passed) and `help-04-group-level-help.cs` (6 passed). No bugs, suggestions, or nits.

## Issues

<!-- none -->
