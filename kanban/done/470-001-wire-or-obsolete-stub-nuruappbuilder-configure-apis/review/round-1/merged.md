# Round 1 — merged findings
**Date:** 2026-09-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:231
- Description: `IsPerCommandHelpRoute` uses `OriginalPattern.Contains("--help")` / `FullPattern.Contains("--help")` (Ordinal). Because `--help` is a prefix of other long-form names, a user route such as `run --helper` or `run --help-all` is classified as a per-command help route. `ShowPerCommandHelpRoutes` defaults to false, so those commands are omitted from CLI `--help` even though they are not help routes. The later `OptionDefinition.LongForm is "help"` walk is the correct check and already covers documented patterns like `blog --help?`.
- Suggestion: Drop the substring `Contains` (or require a token boundary: `--help` followed by end-of-pattern, whitespace, or optional `?`). Keep the `LongForm == "help"` segment test. Add a regression that maps `--helper` and asserts it still appears in default `--help`.
- Source: general
- Disposition notes: Dropped substring `Contains`. Classification is only `OptionDefinition.LongForm is "help"` with `!ExpectsValue`. Regression `Should_list_helper_option_that_is_not_a_help_route` added to help-10. help-10: 7/7 passed.

## Duplicates / conflicts

- None (single general reviewer).
