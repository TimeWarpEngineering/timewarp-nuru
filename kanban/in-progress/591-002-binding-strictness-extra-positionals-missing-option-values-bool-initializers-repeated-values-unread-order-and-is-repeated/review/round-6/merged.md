# Round 6 — merged findings
**Date:** 2026-11-20
**Sources:** general
**Scope:** fix pass `59102fad..1d29674c`. Carries M1–M10.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 6 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 1 | 2 | 0 |

## Resolved prior

- M1–M8: fixed in earlier rounds.
- M9 (bug): fixed. A string repeated option takes one value per flag when a catch-all follows and no `--` comes
  before it. The emitter emits `if` instead of `while` (`TakesOneValueBeforeCatchAll`). routing-45 pins the
  fluent-docker-run sample (`e=[A=1]|image:nginx|cmd:[echo,hi]`), the repeated `--env` shape,
  `docker --env? {e}* {*cmd}`, typed `-p 80 443`, and the `--` route that keeps the greedy run. The guide and the
  migrate skill describe the rule.
- M10 (suggestion): fixed. The gate record has `ganda runfile cache --clear` (100 entries), CI multi 2332 passed,
  7 skipped, 0 failed, and standalone 50 files, 0 failed.

## Issues

### M11 — Severity: bug — Status: open
- File: tests/timewarp-nuru-tests/routing/routing-45-binding-strictness.cs
- Description: routing-45 was squeezed to stay under the 500-line cap instead of being split. Blank lines came out
  of about 20 existing tests and the command classes. The new tests put each builder chain on one line of more than
  200 characters. The only effect is that the file-size check passes (the "silencing the check" form). The check is
  the 500-line file cap, the one task 581 enforced by splitting files.
- Suggestion: Restore the blank lines. Move the repeated-option cases into a new runfile, for example
  `routing-46-repeated-option-binding.cs`, with its own test class and the same `#if !JARIBU_MULTI` entry point.
  Wrap the long builder chains. Record the `dotnet run --file` count for each file.
- Source: general
- Disposition notes:

### M12 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.option-parsing.cs:283; migration
  guide row at line 226 and checklist item 3; skills/tw-nuru-migrate-system-commandline/SKILL.md:322
- Description: The one-value rule depends on `accepts is null`, which also covers an element with no built-in
  conversion (enum, custom converter). The text says "string" and says a "typed" option still takes every
  converting token.
- Suggestion: Say "string, or a type with no built-in conversion" (for user docs, "string or enum"). Do this in the
  M11 pass.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer, so there is nothing to collapse.
- Round 5 did not flag the one-line builder chains in its new cases. The blank-line removal in this round shows
  why they were written that way, so M11 covers the whole file.
