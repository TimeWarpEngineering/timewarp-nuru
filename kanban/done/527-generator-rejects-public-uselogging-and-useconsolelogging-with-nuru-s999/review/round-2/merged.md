# Round 2 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: documentation/user/features/logging.md:178
- Description: Both logging.md copies now say the generator parses with no logger, so neither the build nor the running app writes the parser, lexer, and compiler messages. The `PatternParser.Parse(pattern, loggerFactory)` sample is unchanged. Confirmed against `PatternParser.TryParse`, the `#if !ANALYZER_BUILD` log calls, and `ANALYZER_BUILD` on the analyzer project. The two copies are identical.
- Source: general
- Disposition notes: fixed by `ffeddcda`. Verified this round.

## Issues

None new.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
