# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix commit `11cc4e85` since round-1 base `ac001e9f`. `AttributeLocation`, `MetadataPropertyDefault`, `CallableParameterlessConstructor`, and generator-59.

## Summary

Round 1's two bugs are fixed on this delta, and the new code does not add another defect. `AttributeLocation` uses the opt-in site when the attribute syntax tree is not in this compilation. `MetadataPropertyDefault` copies a record parameter default only from the constructor `new()` calls, and a parameter with no default stays required only when that constructor does not exist. generator-59 passed 9/9 in this review, including the two new cases. The implementer restatement still matches the original task.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:1040
- Description: `AttributeLocation` returns the fallback when `ApplicationSyntaxReference` is missing or `compilation.ContainsSyntaxTree` is false. `ResolveRouteIdentity` passes the compilation through. NURU_H008, route-pattern validation, and NURU_H009 all use that location. generator-59 `Should_report_at_the_map_call_for_a_compilation_reference` reports NURU_H008 in `app.cs`. `Generate` asserts the generator exception is null.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1245
- Description: `CallableParameterlessConstructor` is the constructor `new()` binds to: a zero-parameter constructor, otherwise the first accessible constructor whose parameters are all optional or params. `MetadataPropertyDefault` reads only that constructor when it exists, copies an explicit parameter default, and otherwise returns `default!` plus `[DefaultValue]`. With no such constructor, a matching parameter with no explicit default is still required. generator-59 `Should_require_a_record_parameter_only_on_the_called_constructor` keeps `--Name` required on `RefRecordQuery` and keeps `RefNoteQuery.Title` optional with `__commandDefaults.Title`, for both a DLL reference and a CompilationReference.
- Status: fixed

## Issues

No new issues.
