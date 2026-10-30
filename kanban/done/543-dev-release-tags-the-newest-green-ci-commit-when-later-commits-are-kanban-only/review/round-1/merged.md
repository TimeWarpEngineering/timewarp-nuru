# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:565
- Description: `git diff --name-only <sha>^1 <sha>` lists only a rename's post-image. A commit that moves `source/a.cs` to `kanban/a.cs` looks kanban-only, and the walk can tag an older green commit that still contains the shipping file. Verified with git 2.53: `--name-only` prints `kanban/a.cs`; `--name-only --no-renames` prints `kanban/a.cs` and `source/a.cs`.
- Suggestion: Pass `--no-renames` on that diff so `FindShippingPath` sees both paths. Do not depend on the operator's `diff.renames` setting.
- Source: general
- Disposition notes: fixed in fix pass 2026-10-30. The walk diff passes `--no-renames`, with a comment. Checked in a temp repo with `diff.renames=true`: a `source/a.cs` → `kanban/a.cs` move lists both paths.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:404
- Description: A failed `git show` of `source/Directory.Build.props` at the target drops stderr. `CheckTargetVersion` (`release-guard.cs:273`) then says the version is unreadable because the commits are not kanban-only and tells the operator to wait for CI. Unparseable XML takes that same sentence.
- Suggestion: Surface `git show` stderr as a git error. Say the props file is unreadable when parse fails. Use the version-mismatch sentence only when both versions were read and differ.
- Source: general
- Disposition notes: fixed in fix pass 2026-10-30. A failed `git show` now prints `Error: git show <short>:source/Directory.Build.props failed.` with stderr and exits 1. `CheckTargetVersion` with a null version says the props file is unreadable (does not parse or has no `<Version>`). The "not kanban-only" sentence is used only when both versions were read. Tests updated in `release-01-guard-matrix.cs`.

## Duplicates / conflicts

- No overlapping findings. One reviewer.
