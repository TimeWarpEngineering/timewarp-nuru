# Round 2 — general
**Date:** 2026-10-05
**Scope reviewed:** fix delta on `changelog.md` for round-1 M1–M7, re-checked against the same tags and NuGet facts

## Summary

The fix delta moves beta.70’s shipped notes onto beta.70, splits beta.26 from beta.27, puts `--capabilities` and `MapMultiple` on beta.22, adds the two beta.72 user-facing fixes and the beta.66 workflow bullet, and cites kanban ids as `task NNN`. Re-read of those sections matches the tag ancestry used in round 1. No new defect in the fix delta.

## Resolved prior

- M1 fixed. beta.70 (changelog.md:88) carries the check-version / `IRepoConfigService` / dependency bullets and states the GitHub tag is `v3.0.0-beta.69` with no beta.69 package. beta.69 (line 97) points at beta.70. beta.71 still has only the per-command `--help` fix.
- M2 fixed. beta.27 (line 333) has the MCP bullets. beta.26 (line 338) is one line: same commit as beta.25 (`ac2ede19`), no NuGet package.
- M3 fixed. beta.22 Added lists `--capabilities` (#157). beta.24 keeps per-command `--help` (task 356) and says the source generator emits per-app `--capabilities` output, with the flag introduced in beta.22.
- M4 fixed. beta.22 Changed has a breaking `MapMultiple` bullet.
- M5 fixed. beta.72 Fixed has the single-dash option bullet and the `{` / quote / backslash help-escape bullet.
- M6 fixed. The 404 `#NNN` citations in the backfill now say `task NNN`. beta.57 cites `task 442, #191`. PR 191 is “v3.0.0-beta.57: Fix Unit type ambiguity in source generator”.
- M7 fixed. The beta.66 / beta.67 Fixed list includes the direct workflow-handler call and the `DevCli.Endpoints` rename. beta.67 remains a same-notes republish of beta.66, which is the commit that contains `346b44a3`.

## Issues

No issues.
