# Round 1 — merged findings
**Date:** 2026-09-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: kanban/to-do/456-check-version-warn-when-source-is-multiple-versions-ahead-of-last-release.md:46
- Description: The triage note said "Requirements below are updated accordingly." The rewritten requirements are the section above that note.
- Suggestion: Point at the requirements above, which already drop the git-tag strategy.
- Source: general
- Disposition notes: Fixed on this id. The note now says the requirements above match the single NuGet methodology.

### M2 — Severity: nit — Status: fixed
- File: kanban/to-do/069-global-user-key-binding-profiles.md:15
- Description: The path correction named obsolete `AddReplOptions(...)` as a current way to enable the REPL.
- Suggestion: Name `AddRepl()` and `AddRepl(Action<ReplOptions>)`, and say `AddReplOptions` is obsolete.
- Source: general
- Disposition notes: Fixed on this id. The sentence now matches the `[Obsolete]` message on `AddReplOptions`.

## Duplicates / conflicts

- None (single reviewer).
