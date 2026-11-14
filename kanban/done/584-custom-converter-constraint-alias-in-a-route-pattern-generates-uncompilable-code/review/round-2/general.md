# Round 2 — general
**Date:** 2026-11-14
**Scope reviewed:** Product delta since round-1 base `2d49ff0f`: `d3a444e1` and `558ab9d6`. Checked against `task.md` ## Goal (implementer restatement), the round-1 fix-pass paragraph, and the original task. ## Results records the fix-pass build, test, and audit. No full build, test, or audit was run. No targeted command was run.

## Summary

`d3a444e1` closes M1. `UnknownTypeConstraintValidator.IsUnresolved` is the shared rule. The route matcher declares an unresolved positional, optional positional, value option, repeated option, or catch-all as the bound handler type with `default!`. The `--json-args` binder assigns `default!` instead of the raw string. generator-76 now requires those compilations to have no compile errors, and it covers the five shapes M1 named. The recorded gates cover this delta, including the `TimeWarp.Build.Tasks` pin in `558ab9d6`.

The new list rule treats a constraint as a built-in only when `GetBuiltInTryConversion` returns a parser. `uri`, `FileInfo`, and `DirectoryInfo` are built-ins on the scalar path and are rejected on a typed catch-all or repeated option, with a message that says they are not built-in types.

The restatement still matches the original task. The generator-75 file placement noted in ## Learnings is unchanged. No new misread.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/validation/unknown-type-constraint-validator.cs:128
- Description: `IsKnownListElement` accepts a list constraint as a built-in only when `TypeConversionMap.GetBuiltInTryConversion` returns a parser (`unknown-type-constraint-validator.cs:128-137`). That call returns null for `uri`, `fileinfo`, and `directoryinfo` (`type-conversion-map.cs:62`) even though `IsBuiltInType` is true for them (`type-conversion-map.cs:167` and `:169-170`) and the scalar emitters convert them (`route-matcher-emitter.type-conversions.cs:103-204` and `route-matcher-emitter.option-conversions.cs:90`). A typed catch-all or repeated option of those constraints is `IsUnresolved`, so the route reports `NURU_S010` and never reaches the array emitter. `CatchAllReason` and `RepeatedOptionReason` (`unknown-type-constraint-validator.cs:34-38`) say the constraint "is not a built-in type", which is false for these three. The acceptance line is that a built-in constraint stays quiet. Marking them known without a list emitter would restore M1: `EmitBuiltInArrayConversion` returns false and the catch-all falls through to `string[]` (`route-matcher-emitter.type-conversions.cs:270-274`), and the repeated-option path does the same (`route-matcher-emitter.helpers.cs:93-94`). generator-76's known case only locks `{*values:int}` (`generator-76-converter-constraint-alias.cs:235`). `analyzer.md` says a typed catch-all converts built-in element types (`documentation/user/features/analyzer.md:190`). "Built-in" is also used to mean TryParse-only. No command was run; this is the validator, the conversion map, and the emitters.
- Suggestion: Treat every `IsBuiltInType` constraint as a known list element, and emit per-element `Uri.TryCreate`, `FileInfo`, and `DirectoryInfo` conversion on the catch-all and repeated-option paths, using the same construction the scalar emitters use. `{*files:FileInfo}`, `{*urls:uri}`, and `--dir {dir:DirectoryInfo}*` must not report `NURU_S010`, and the generated compilation must have no errors. Add those three list forms to generator-76's known-constraint case. In `glossary.md`, define built-in type as the `TypeConversionMap` set, including Uri, FileInfo, and DirectoryInfo, so the word is not also used to mean TryParse only. Update `analyzer.md` only if the list sentence would still say every built-in converts.
- Status: open
