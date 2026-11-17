# Round 1 — merged findings
**Date:** 2026-11-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-nuru-verify/SKILL.md:105
- Description: The skill still says `CS1061` and `CS0117` are broken unless the receiver is a host extension point (`IServiceCollection`, `ILoggingBuilder`). The script now also drops `CS1061` whose receiver is a built-in keyword type (`bool`, `byte`, `sbyte`, `char`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `float`, `double`, `decimal`, `string`, optionally `?` or `[]`). `CS0117` on those types is unchanged. `object` and `dynamic` stay blamed on purpose. Someone following the skill will treat a Shouldly-style `CS1061` on `bool` as a doc defect, which is the false blame this task removes. The script comment at `nuru-consumer-check.sh:320` already states the new rule. The skill is the other copy of that contract, and this diff does not update it.
- Suggestion: Update the `broken` bullet so it matches `nuru_errors`. Host extension points still cover `CS1061` and `CS0117`. Built-in keyword receivers cover `CS1061` only. Say that `object` and `dynamic` stay blamed.
- Source: general
- Disposition notes: fixed in the round-2 fix pass. The `broken` bullet in `skills/tw-nuru-verify/SKILL.md` now says `CS1061` (not `CS0117`) on a built-in keyword receiver (list matches `nuru_errors`, optional `?`/`[]`) is not blamed, and that `object`, `dynamic`, and package types stay blamed.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
