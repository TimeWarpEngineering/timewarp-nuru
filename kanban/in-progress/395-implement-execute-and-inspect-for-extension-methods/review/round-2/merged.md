# Round 2 — merged findings
**Date:** 2026-08-27
**Sources:** general

## Counts (final)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:429
- Description: Re-verified. `ResolveServiceForCommand` fallback includes `!s.IsInternalType`. Command-handler path covered by generator-42 `Should_not_emit_new_or_field_for_internal_impl_on_command_handler`.
- Suggestion: (done)
- Source: general
- Disposition notes: verified on `b034c788` / HEAD vs origin/master

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/referenced-method-decompiler.cs:174
- Description: Re-verified. `FindLibSibling` ranks `lib/` TFMs by closeness to compile stub then compilation TFM; `net*` before `netstandard`. Fail-closed if no real body.
- Suggestion: (done)
- Source: general
- Disposition notes: verified on `b034c788` / HEAD vs origin/master

## Duplicates / conflicts

- None. No new findings on the fix delta.
