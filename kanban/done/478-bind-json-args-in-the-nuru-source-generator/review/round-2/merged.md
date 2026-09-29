# Round 2 — merged findings
**Date:** 2026-09-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:341
- Description: Absent `--json-args` keys initialized non-list locals to `default!`, wiping an endpoint option's property default.
- Suggestion: Initialize from `DefaultValueLiteral` or `ParameterDefinition.DefaultValue` for non-flag, non-list slots.
- Source: general
- Disposition notes: Fixed on this id. `DefaultInitializer` supplies the literal. Flags stay `false` and repeated options stay empty, matching the argv matcher. `Should_keep_option_defaults_when_the_json_key_is_absent` checks `title:untitled` and `retries:3` for both argv and JSON.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:491
- Description: A missing required positional with a later reserved literal forced `__litOk = false` before that literal was tested, so the error collapsed to "Unknown command".
- Suggestion: Keep `__litOk` true until a later literal actually fails.
- Source: general
- Disposition notes: Fixed on this id. `Should_name_a_missing_required_positional_when_later_literals_match` expects the missing-`env` error for `promote now`, "Unknown command" for `promote later`, and a successful bind when the JSON object supplies `env`.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:889
- Description: Argv conversion failures were reported with the JSON type-mismatch sentence.
- Suggestion: Use an argv invalid-value sentence that names the key and the expected type and does not echo the value.
- Source: general
- Disposition notes: Fixed on this id. `Should_not_call_an_argv_conversion_failure_a_json_type_error` expects "Invalid value for 'count'" and "Expected int", and rejects "wrong JSON type".

## Resolved prior

- M1, M2, and M3 fixed. No new findings.

## Duplicates / conflicts

- None (single reviewer).
