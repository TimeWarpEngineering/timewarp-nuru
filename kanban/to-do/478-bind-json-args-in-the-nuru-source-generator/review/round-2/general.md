# Round 2 — general
**Date:** 2026-09-29
**Scope reviewed:** review-fix delta on task 478. Re-checked M1–M3 against `json-args-emitter.cs` and `routing-33-json-args.cs`.

## Summary

M1 now initializes a non-flag, non-list local from `DefaultValueLiteral` or `ParameterDefinition.DefaultValue`, so an absent key keeps the endpoint property default. M2 no longer clears `__litOk` before later literals are tested, so a missing positional is named only when those literals match. M3 uses a separate argv invalid-value sentence and still does not echo the value. No new defects on the fix delta.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:341
- Description: M1. Absent keys were `default!`.
- Suggestion: Initialize from the recorded default literal.
- Status: fixed

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:491
- Description: M2. A reserved later literal forced `__litOk = false` before that literal was checked.
- Suggestion: Leave `__litOk` for the literal check.
- Status: fixed

### Issue 3 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:889
- Description: M3. Argv conversion failures used the JSON type-mismatch sentence.
- Suggestion: Name the key and expected type without calling it a JSON type error and without echoing the value.
- Status: fixed
