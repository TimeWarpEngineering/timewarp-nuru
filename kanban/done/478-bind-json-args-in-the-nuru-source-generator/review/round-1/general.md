# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** branch `task/478-bind-json-args-in-the-nuru-source-generator` vs `origin/master`. Design of record: task 457 `research/recommendation.md` follow-up 1 (bind `--json-args`). Invocation object and docs stay on task 479.

## Summary

`--json-args` is peeled before `IsConfigArg` and user routes, attached forms exit 1, and the JSON path selects by literal count without `ComputedSpecificity`. Binding goes through `TypeConversionMap` and `EnumTypeConverter`. The stdin, TTY, REPL, tie, and built-in-skip behavior matches the recommendation. Two selection/bind gaps remain: an absent key drops an option's property default, and a required positional omitted beside a later literal is reported as an unknown command. An argv conversion failure is also labeled as a JSON type error.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:338
- Description: On the bind path every non-list value starts as `default!` and is assigned only when argv or JSON supplies the key. The existing matcher initializes an option from `OptionDefinition.DefaultValueLiteral` (the endpoint property initializer). `ship --json-args '{"env":"prod"}'` therefore nulls `Title = "untitled"` and zeroes `Retries = 3`. The recommendation says absent keys leave the existing default, and this path is supposed to assign the same locals the matcher already assigns.
- Suggestion: Initialize the generated local from `DefaultValueLiteral` (or `ParameterDefinition.DefaultValue`) when the slot is not a flag or a list. Leave flags at `false` and repeated options at an empty list, matching the matcher.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:491
- Description: When a required positional is missing from argv and JSON and a later literal is still reserved, the attempt sets `__litOk = false` before that literal is checked. `promote now --json-args '{}'` against `promote {env} now` exits 1 with "Unknown command" even though both literals match and the only problem is the missing `env` value. Recommendation match rule 5 says to name that failure.
- Suggestion: Record the missing-value failure and still check later literals. A later literal mismatch clears `__litOk`, so the failure is reported only when the literals actually match.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:901
- Description: `EmitFill` uses the JSON type-mismatch text for the argv branch too. `deploy --count nope --json-args '{}'` tells the caller the key has the wrong JSON type. The bad token is on argv; the JSON object is `{}`.
- Suggestion: Keep the JSON wording for JSON values. For an argv conversion failure, say the value is invalid and name the key and the expected type. Do not echo the value.
- Status: open
