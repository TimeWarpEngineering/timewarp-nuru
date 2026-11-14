# Round 1 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.type-conversions.cs:223
- Description: An unresolved type constraint still produces the uncompilable output this task exists to stop. `UnknownTypeConstraintValidator` reports `NURU_S010`, and `NuruGenerator` Stage B still emits (`nuru-generator.cs` registers the source after validation). For a positional parameter the no-converter branch writes only a comment and does not declare the variable (`route-matcher-emitter.type-conversions.cs:223-231`); `BuildArgumentListFromRoute` still passes that name (`handler-invoker-emitter.output.cs:286-291`), which is the original CS0103. For an option the same branch assigns `string?` (`route-matcher-emitter.option-conversions.cs:221-225`), which is the original CS0029 against a converted handler parameter. For json-args, `EmitAssignScalarFromString` falls through to `{variable} = {raw}!` (`json-args-emitter.assign.cs:192`) while the slot field is the handler type (`json-args-emitter.bind-methods.cs:198`), the original `at = __raw_at!` CS0029. A typed catch-all that is not a built-in keeps the `string[]` fallback (`route-matcher-emitter.type-conversions.cs:261-265`). generator-76 `Should_report_unknown_constraint_on_parameter_and_option` and `Should_report_alias_that_is_not_a_constant` assert `NURU_S010` only (`generator-76-converter-constraint-alias.cs:189-196` and `:258-262`). They never require `CompileDiagnostics` to be free of CS0103 and CS0029, so the recorded 4/4 pass does not lock the checklist line "Unknown constraint names never produce uncompilable output (diagnostic instead)". No command was run; this is the emitter and test source.
- Suggestion: When the matcher and the enum-handler check both miss, emit a declaration that compiles (the handler CLR type, or another stand-in the handler call can accept) and do not assign a raw string or leave the name undeclared. Keep `NURU_S010` on the route. In generator-76, require the unknown-constraint and non-constant-alias compilations to report `NURU_S010` and to contain no CS0103 or CS0029.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. No overlaps.
