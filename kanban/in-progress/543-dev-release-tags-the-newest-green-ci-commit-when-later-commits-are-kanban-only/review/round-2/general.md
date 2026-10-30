# Round 2 — general
**Date:** 2026-10-30
**Scope reviewed:** fix delta `03ff8dbb..61e90dc4` since round-1 `base.sha`. Product files: `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`, `source/timewarp-nuru-devcli/content/any/services/release-guard.cs`, `tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs`.

## Summary

The fix pass closes round 1 M1 and M2. The walk now passes `--no-renames` on `git diff --name-only`, so a move from `source/` into `kanban/` lists both paths. A failed `git show` of the target props exits with that command's stderr before `CheckTargetVersion`. A null target version says the props file is unreadable; the version-mismatch sentence is used only when both versions were read and differ. Nothing new on this delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:571
- Description: Re-checked. The walk diff is `git diff --name-only --no-renames <sha>^1 <sha>`. With git 2.53, a `source/a.cs` → `kanban/a.cs` move prints only `kanban/a.cs` under default rename detection, and prints both `kanban/a.cs` and `source/a.cs` with `--no-renames`. The same pair of paths is printed when `diff.renames=copies`. `FindShippingPath` treats `source/a.cs` as shipping, so the walk stops.
- Suggestion: none remaining
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:410
- Description: Re-checked. `git show` uses `WithNoValidation`. A non-zero exit prints `Error: git show <short>:source/Directory.Build.props failed.` plus stderr and returns before `CheckTargetVersion`. `XmlException` from `PropsVersionReader.Parse` leaves the target version null. `CheckTargetVersion` (`release-guard.cs:276`) then says `source/Directory.Build.props` at the target is unreadable and does not use the "not kanban-only" sentence. That sentence remains only when both versions were read and differ (`release-guard.cs:281`). `Parse` returns null for a missing or blank `<Version>`, which takes the unreadable sentence. The guard-matrix tests assert the two sentences stay apart.
- Suggestion: none remaining
- Status: fixed

## Issues

None.
