# Round 2 — merged findings
**Date:** 2026-11-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:48
- Description: Re-verified on `95dc80b8`. The recipe says positional order is property declaration order, and that `Order` is unread and `NURU_A002` is unreported on 4.0.0-beta.14. Samples and the skill no longer set `Order`. Filed as task 591 item 11.
- Suggestion: none further
- Source: general
- Disposition notes: Fixed in the round-1 fix pass. Round 2 re-verified against `ExtractParameterFromAttribute` and the `GetMembers()` walk.

### M2 — Severity: bug — Status: fixed
- File: skills/tw-nuru-migrate-system-commandline/SKILL.md:174
- Description: Re-verified. Step 4 returns `Unit.ValueTask` from the synchronous template. `Unit.Value` is mentioned only for an `async` `Handle`.
- Suggestion: none further
- Source: general
- Disposition notes: Fixed in the round-1 fix pass. Round 2 re-verified.

### M3 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:249
- Description: Re-verified. The normalizer stops at `--` and copies the separator and the tail unchanged. The skill says the same.
- Suggestion: none further
- Source: general
- Disposition notes: Fixed in the round-1 fix pass. Round 2 re-verified by reading the sample.

### M4 — Severity: suggestion — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:55
- Description: Re-verified. Repeat is the array or collection type. `IsRepeated` is documented as unread. Filed as task 591 item 12.
- Suggestion: none further
- Source: general
- Disposition notes: Fixed in the round-1 fix pass. Round 2 re-verified against `ExtractOptionFromAttribute` and `IsRepeatedOptionType`.

### M5 — Severity: suggestion — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:164
- Description: Re-verified. Endpoint properties list includes `Version` and excludes `FileInfo`, `DirectoryInfo`, and `IPAddress`. Glossary `built-in type` notes the narrower endpoint subset. Filed as task 591 item 13.
- Suggestion: none further
- Source: general
- Disposition notes: Fixed in the round-1 fix pass. Round 2 re-verified against `GetTypeConstraintFromClrType` and `TypeConversionMap`.

### M6 — Severity: nit — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:750
- Description: Re-verified. Stray `**` is gone. The enum sentence is its own paragraph under item 9.
- Suggestion: none further
- Source: general
- Disposition notes: Fixed in the round-1 fix pass. Round 2 re-verified.

## Duplicates / conflicts

- Single reviewer. Prior M1–M6 carried forward with status `fixed`. No new findings.
