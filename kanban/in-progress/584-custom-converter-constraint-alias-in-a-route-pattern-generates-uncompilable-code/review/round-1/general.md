# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** Product delta of `69ac1455` against `a52e46d6` (`origin/feature/generator-fixes-2026-11-14`), read against `task.md` ## Goal (implementer restatement) and the original task. Recorded gates in ## Results were taken as the build, test, and audit record. No full build, test, or audit was run. No targeted command was run; the unknown-constraint paths were read in the emitters.

## Summary

`69ac1455` reads `ConstraintAlias` and `TargetType` from converter source and shares `CustomConverterMatcher` across the route matcher, option conversions, the json-args binder, and the new `NURU_S010` validator. generator-76's alias case requires the generated compilation to have zero errors, so `{at:point}`, `{at:point?}`, `--at {at:point}`, the type-name form, and the namespaced `SpotParser` alias are covered. ## Results records build, test, and audit as pass, including that standalone file.

An unknown constraint now reports `NURU_S010`, and Stage B still emits the old fallback. The positional path does not declare the handler variable (CS0103). The option path declares it as `string?` (CS0029). The json-args path assigns the raw string to the handler type (CS0029). generator-76's unknown-constraint cases assert the diagnostic only, so the recorded 4/4 pass does not cover the "diagnostic instead of uncompilable output" checklist item.

The three-dot diff against `master` also contains the task 583 and 586 product files. They are already on `a52e46d6` and are not part of `69ac1455`. Task 583's disposition accepted that stack. This review does not reopen it.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.type-conversions.cs:223
- Description: An unresolved type constraint still produces the uncompilable output this task exists to stop. `UnknownTypeConstraintValidator` reports `NURU_S010`, and `NuruGenerator` Stage B still emits (`nuru-generator.cs` registers the source after validation). For a positional parameter the no-converter branch writes only a comment and does not declare the variable (`route-matcher-emitter.type-conversions.cs:223-231`); `BuildArgumentListFromRoute` still passes that name (`handler-invoker-emitter.output.cs:286-291`), which is the original CS0103. For an option the same branch assigns `string?` (`route-matcher-emitter.option-conversions.cs:221-225`), which is the original CS0029 against a converted handler parameter. For json-args, `EmitAssignScalarFromString` falls through to `{variable} = {raw}!` (`json-args-emitter.assign.cs:192`) while the slot field is the handler type (`json-args-emitter.bind-methods.cs:198`), the original `at = __raw_at!` CS0029. A typed catch-all that is not a built-in keeps the `string[]` fallback (`route-matcher-emitter.type-conversions.cs:261-265`). generator-76 `Should_report_unknown_constraint_on_parameter_and_option` and `Should_report_alias_that_is_not_a_constant` assert `NURU_S010` only (`generator-76-converter-constraint-alias.cs:189-196` and `:258-262`). They never require `CompileDiagnostics` to be free of CS0103 and CS0029, so the recorded 4/4 pass does not lock the checklist line "Unknown constraint names never produce uncompilable output (diagnostic instead)". No command was run; this is the emitter and test source.
- Suggestion: When the matcher and the enum-handler check both miss, emit a declaration that compiles (the handler CLR type, or another stand-in the handler call can accept) and do not assign a raw string or leave the name undeclared. Keep `NURU_S010` on the route. In generator-76, require the unknown-constraint and non-constant-alias compilations to report `NURU_S010` and to contain no CS0103 or CS0029.
- Status: open
