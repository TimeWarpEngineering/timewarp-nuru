# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:1015
- Description: `AttributeLocation` uses a referenced type's attribute syntax even when that syntax tree belongs to another compilation. Reporting NURU_H008, NURU_H009, or a route-pattern diagnostic then fails the generator. Roslyn 5.6 throws because the location is not part of the compilation being analyzed. DLL metadata has no syntax, so the current tests stay on the opt-in fallback and pass. An IDE project reference is the shape `ExtractPropertyDefaultValueFromSymbol` already refuses to bind.
- Suggestion: Fall back to the opt-in location when `compilation.ContainsSyntaxTree(reference.SyntaxTree)` is false. Add a `CompilationReference` test that an inaccessible nested handler reports NURU_H008 at the `Map<T>()` call and the generator exception is null.
- Source: general
- Disposition notes: fixed in implement-fix-2. `AttributeLocation` falls back when `!compilation.ContainsSyntaxTree`. Test: generator-59 `Should_report_at_the_map_call_for_a_compilation_reference` (fails with the reviewer's exception when the guard is removed).

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1253
- Description: `MetadataPropertyDefault` treats the first name-matched record constructor parameter as the default even when that parameter has no explicit default. The empty `PropertyDefault` makes a non-nullable member required and puts it in the object initializer, so the compiled initializer does not run and `[DefaultValue]` is ignored. That matches `record Q(string Name)` with no parameterless constructor. It is wrong when `HasCallableParameterlessConstructor` selects `new()`, as in `record Note(string Title) { public Note() : this("untitled") {} }`: `--Title` becomes required and `"untitled"` never applies. A parameter that does have an explicit default is copied; generator-59's `Count = 7` covers that path.
- Suggestion: Copy a parameter only when `HasExplicitDefaultValue` is true. Keep a non-optional parameter required only on the constructor that will actually be called. When that constructor is parameterless, use `default!` and `[DefaultValue]`. Test both the required primary parameter and the parameterless chaining constructor.
- Source: general
- Disposition notes: fixed in implement-fix-2. `MetadataPropertyDefault` uses only the constructor the generated code calls (`CallableParameterlessConstructor`). It copies explicit defaults, keeps a no-default parameter required only without `new()`, and otherwise uses `default!` + `[DefaultValue]`. Test: generator-59 `Should_require_a_record_parameter_only_on_the_called_constructor`.

## Duplicates / conflicts

- No overlap. One reviewer.
