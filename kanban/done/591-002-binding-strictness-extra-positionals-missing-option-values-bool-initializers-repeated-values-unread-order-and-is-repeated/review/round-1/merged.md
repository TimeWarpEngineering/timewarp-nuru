# Round 1 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 3 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.option-parsing.cs:51
- Description: Every plain flag now consumes a following boolean spelling (`0`, `1`, `yes`, `no`, `on`,
  `off`, `true`, `false`, `enabled`, `disabled`). A positional with that spelling after a flag is taken as the
  flag value: `seek --verbose 0` on `seek --verbose {pos:int?}` binds `verbose = false`, `pos = null`, exit 0;
  with a required positional it becomes `Unknown command`. Not in the task (item 2 concerns the default-true
  bool), untested for the collision, undocumented.
- Suggestion: Remove boolean-token consumption from ordinary flags (the default-true value option already
  covers `--create false`), or narrow it to `true`/`false` and add a test plus a behavior-differences entry.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.properties.cs:359
- Description: `[Parameter(Name = ...)]` is not validated. `Name = "source-file"` makes the matcher declare
  `string source-file` (`CamelCaseName` keeps hyphens) while construction reads `sourceFile`
  (`ToCamelCase`): generated code does not compile and the user sees CS errors, not a NURU diagnostic. The
  fluent pattern path rejects `{source-file}` up front.
- Suggestion: Validate `Name` as an identifier and report a diagnostic, or normalize to one form used by both
  emitters; add a generator test.
- Source: general
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: documentation/user/features/endpoints.md:132
- Description: User guide says `Order` is "inferred from declaration order if omitted" and its example omits
  `Order` on two of three parameters, which now reports NURU_A002 (error) twice.
  `documentation/developer/design/source-generators/endpoint-generator.md:106-111` has the same shape.
- Suggestion: Set `Order` in both examples; say `Order` is required for two or more parameters (NURU_A002)
  and the catch-all is forced last.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.option-attributes.cs:193
- Description: Bare `--create` on a default-true bool is now `Unknown command`, exit 1 (value required).
  The migration guide and skill do not say so; System.CommandLine treats bare `--create` as true.
- Suggestion: Bind `true` for a bare default-true bool, or document the rejection in the concept map and
  behavior differences.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.properties.cs:272
- Description: `ExtractSegmentFromProperty(PropertyDeclarationSyntax, ...)` has no callers; the diff edited it to
  discard diagnostics and `Order`.
- Suggestion: Delete it.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer; none.
