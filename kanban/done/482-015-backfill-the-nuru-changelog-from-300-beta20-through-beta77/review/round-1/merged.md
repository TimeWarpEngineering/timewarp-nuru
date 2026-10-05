# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 5 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: changelog.md:88
- Description: beta.70’s contents are the tree tagged `v3.0.0-beta.69`, not an unknown package whose changes belong under beta.71. That tag’s `Directory.Build.props` version is `3.0.0-beta.70`. NuGet has `3.0.0-beta.70` (published 2026-04-27) and no `3.0.0-beta.69`. The check-version / `IRepoConfigService` / dependency bullets are in that tree. `v3.0.0-beta.69..v3.0.0-beta.71` only adds the per-command `--help` fix, which is already under beta.71.
- Suggestion: Move those bullets onto beta.70. Note the GitHub release tag `v3.0.0-beta.69` and that beta.69 was not published to NuGet. Delete the “cannot be confirmed” sentence.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: changelog.md:328
- Description: beta.26 and beta.25 are the same commit (`ac2ede19`). No NuGet package `3.0.0-beta.26`. The MCP `examples.json` / `GitHubCacheService` changes are only in `v3.0.0-beta.26..v3.0.0-beta.27`. Grouping 26 with 27 attributes that work to a beta that does not contain it. Identical GitHub release notes are not the same ship.
- Suggestion: Give beta.27 its own section. One line for beta.26: same commit as beta.25, not published.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: changelog.md:348
- Description: `--capabilities` shipped in beta.22 (`6a8ae628`, ancestor of `v3.0.0-beta.22`; still present at beta.23). beta.24’s Added line presents the flag as new. `c27be0c4` in the beta.24 range is the source generator emitting per-app capabilities, not the first introduction. Per-command `--help` (`c6fa1d93`) does belong on beta.24.
- Suggestion: Put the flag on beta.22. On beta.24, keep per-command `--help` and describe source-generated per-app `--capabilities` output as the beta.24 change.
- Source: general
- Disposition notes:

### M4 — Severity: bug — Status: open
- File: changelog.md:379
- Description: beta.22 removed public `MapMultiple` (`00e61bef`, three public overloads at beta.20, gone at beta.22). The breaking list records `Map(pattern, handler)` / `MapDefault` and not `MapMultiple`.
- Suggestion: Add a breaking bullet: `MapMultiple` was removed; register each pattern with `Map`.
- Source: general
- Disposition notes:

### M5 — Severity: bug — Status: open
- File: changelog.md:67
- Description: In `v3.0.0-beta.71..v3.0.0-beta.72`, `5bfe6aee` accepts multi-character single-dash options and removes the POSIX grouping heuristic (`-e` matched `-help`). `e3dea0c6` escapes `{`, quotes, and backslashes in generated help text so a description such as `greet {name}` compiles. Neither is in the beta.72 section. beta.76’s Unicode line-separator escape is a later, different fix.
- Suggestion: Add both under beta.72 Fixed.
- Source: general
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: changelog.md:163
- Description: Several `(#NNN)` citations are kanban task ids, not GitHub issues or pull requests. `gh api repos/TimeWarpEngineering/timewarp-nuru/issues/NNN` returns 404 for 349, 351, 356, 357, 360, 372, 381, 382, 385, 387, 389, 396, 403, and 442. Each matches a task commit in the same beta range. The reviewer called out `#442` (merged as PR #191). The same changelog uses `task NNN` when the number is a kanban id and `#NNN` when it is a GitHub number.
- Suggestion: Cite `task NNN` for those ids. For beta.57, cite `task 442` and PR `#191`.
- Source: general
- Disposition notes:

### M7 — Severity: suggestion — Status: open
- File: changelog.md:111
- Description: beta.66’s GitHub release notes include a workflow-command fix that the section omits. `346b44a3` is in `v3.0.0-beta.65..v3.0.0-beta.66` and not in beta.65: workflow handlers call the handler directly instead of a non-intercepted `App.RunAsync()`, and `DevCli.Commands` is renamed `DevCli.Endpoints`.
- Suggestion: Add that bullet under the beta.66 / beta.67 section.
- Source: orchestrator (release notes for `v3.0.0-beta.66` plus commit `346b44a3`)
- Disposition notes:

## Duplicates / conflicts

- general Issue 1–6 kept as M1–M6. Issue 6 is widened to every `#NNN` in the backfill that 404s on this repo, same defect as `#442`.
- M7 is the beta.66 release-note bullet omitted from the section. Same class of drop as M5, different beta, so it stays its own id.
