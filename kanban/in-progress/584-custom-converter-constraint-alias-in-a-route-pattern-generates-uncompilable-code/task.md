# Custom converter ConstraintAlias in a route pattern generates uncompilable code

## Description

`IRouteTypeConverter.ConstraintAlias` is public API ("email" for `{to:email}`), but the generator never reads it:
`dsl-interpreter.behaviors.cs` builds every `CustomConverterDefinition` with `ConstraintAlias: null`. A route
that uses the alias compiles to broken code with no diagnostic. Found by task 529's consumer lane
(`generator-75-consumer-surface-no-ivt`):

```csharp
.AddTypeConverter(new PointConverter())          // TargetType = typeof(Point), ConstraintAlias => "point"
.Map("point {at:point}").WithHandler((Point at) => at.X).AsQuery().Done()
```

Generated output:

- `int result = __handler_8(at);`: CS0103, `at` is never declared on the positional path
- `at = __ar_at!;` / `at = __raw_at!;` in the JSON/option path: CS0029, string to `Point`

`{at:Point}` (the type name) works.

## Goal (implementer restatement)

A route constraint that names a custom converter's `ConstraintAlias` (`{at:point}`) must bind through that
converter, the same as the type-name form (`{at:Point}`). A constraint the generator cannot resolve must stop
the build with a Nuru diagnostic that names the parameter and constraint, not with CS0103/CS0029 inside
generated code.

Acceptance:

- The generator reads `ConstraintAlias` from the converter's source at compile time (a constant string from an
  expression-bodied property or a getter `return`), so the existing alias match in the emitters works.
- A typed parameter or option whose constraint is not a built-in type, not a registered converter (target type,
  simple name, alias, or `XConverter` convention), and not an enum handler parameter reports a new error
  diagnostic at the route.
- Regression tests: the alias form compiles as a consumer (generator-75) and the new diagnostic fires for an
  unknown constraint and stays quiet for alias, type-name, built-in and enum forms.
- `ganda runfile cache --clear`, then `./bin/dev build` and `./bin/dev test` green.

Fix pass for review round 1 (M1): reporting NURU_S010 is not enough. The generator still emits, so a
route NURU_S010 reports must also produce generated code that compiles. The only error the build shows is
NURU_S010, never CS0103 (an undeclared positional) or CS0029 (a string assigned to the handler's type) on the
positional, option, repeated-option, catch-all, or `--json-args` paths. generator-76 must assert that the
unknown-constraint and non-constant-alias compilations have no compile errors.

## Checklist

- [x] Decide: read `ConstraintAlias` at compile time (a literal-returning property body), or report a diagnostic for an unknown constraint instead of emitting broken code (both)
- [x] Unknown constraint names never produce uncompilable output (diagnostic instead)
- [x] Regression test (alias form, and an unknown constraint)
- [x] `ganda runfile cache --clear`, then `./bin/dev test` green

## Review

- Round 1: Blocked — open M1 (fix pass implemented; see Results › Fix pass)

## Notes

- The samples define aliases (`email`, `semver`) but route only by type name, so nothing exercised the alias form.
- Filed from task 529 (2026-11-14).

## Session

- Created: 1411579 (2026-11-14)
- Implement (implementer-claude, 2026-11-14): merged origin/master (land kanban 583) before the first commit.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 639e1b10-5a47-443f-8400-c7679b5f13e6, max-turns 500, Done — 2026-11-14T06:07:31Z
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7), session not reported, max-turns uncapped (no --max-turns flag; bound 200), round 1, open finding M1 — 2026-11-14
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200), artifacts general.md, merged.md — 2026-11-14T06:21:23Z
- Implement-fix-2 (implementer-claude, claude-opus-5-5, 2026-11-14): fixed round-1 M1. Unresolved constraints now emit compilable fallback declarations, and list converters report NURU_S010. The audit flagged a new TimeWarp.Build.Tasks beta; I fixed it with audit --fix.

## Results

Both options from the checklist, together:

- **Alias read at compile time.** `DispatchAddTypeConverter` now reads `ConstraintAlias` and `TargetType`
  from the converter source (new partial `generators/interpreter/dsl-interpreter.converters.cs`). It accepts an
  expression body, an auto-property initializer, or a getter with one expression or one `return`, and walks
  base types and explicit interface implementations. `ConstraintAlias` must be a compile-time constant string
  (literal or `const`), read with `SemanticModel.GetConstantValue`. `TargetType => typeof(X)` gives the fully
  qualified target, so a namespaced target type is no longer guessed as `global::<ConverterNameMinusSuffix>`.
  A generic converter still takes its target from the first type argument. A converter from a referenced
  assembly has no syntax and keeps the old behaviour (no alias).
- **NURU_S010 Unknown type constraint (Error).** New `validation/unknown-type-constraint-validator.cs`, run from
  `ModelValidator`. It covers Delegate and Method routes. A typed parameter or a value option (not a flag, not a
  JSON value) is known when its constraint is a built-in type (`TypeConversionMap`), names a registered
  converter, or binds an enum handler parameter. Anything else reports at the `Map(...)` pattern with the
  subject (`Parameter 'at'` / `Option '--at'`), the constraint, and the route. Endpoint routes
  (`HandlerKind.Command`) get their constraints from property CLR types and are not checked.
- **One matcher.** New `generators/emitters/custom-converter-matcher.cs` (`CustomConverterMatcher.Find`).
  The route-matcher positional and option emitters, the json-args binder, and the validator now share it,
  replacing three copies of the same four-way match (target type, simple name, alias, `XConverter`
  convention).
- Docs: `documentation/user/features/analyzer.md` (NURU_S010 section; S009/S010 added to the table) and
  `documentation/user/reference/supported-types.md` (how aliases are read). `AnalyzerReleases.Unshipped.md` lists
  NURU_S010.
- Tests: new `tests/timewarp-nuru-tests/generator/generator-76-converter-constraint-alias.cs`, Roslyn-hosted and
  standalone-only. It is registered in `tests/ci-tests/Directory.Build.props` excludes and in
  `tests/ci-standalone/run-standalone-tests.cs`. It has four cases:
  1. Alias routes `{at:point}`, `{at:point?}`, and `--at {at:point}`, plus the type-name form and a namespaced
     `SpotParser` (getter-block alias from a `const`, no `Converter` suffix). The generator and the compile of
     its output must both report zero errors.
  2. An unknown positional constraint and an unknown option constraint each report NURU_S010.
  3. Built-in, enum, type-name, alias, and typed catch-all constraints report no NURU_S010.
  4. A computed (non-constant) alias reports NURU_S010.

  I set `ConstraintAlias: null` back temporarily and confirmed that cases 1 and 3 fail with NURU_S010 on the
  alias routes. generator-75 was not extended because it sits at the 500-line TWA0038 limit.

### Fix pass (review round 1, M1)

- `UnknownTypeConstraintValidator.IsUnresolved(route, parameter|option, converters)` is now the one rule. The
  validator reports from it, and the emitters use it to choose a fallback declaration. A route NURU_S010 reports
  still emits code that compiles:
  - Positional, optional positional, value option, and repeated option: new partial
    `route-matcher-emitter.unresolved-constraints.cs` declares `{handler parameter type} x = default!;`. The
    handler parameter is found by the same rules `BuildArgumentListFromRoute` uses: position for parameters,
    name match for options. When no handler parameter binds the value, the raw capture is kept (`string?` /
    `string[]`).
  - `--json-args`: a `Slot.Unresolved` flag. `EmitAssignScalarFromString` and `EmitAssignListFromStrings`
    assign `default!` and do not assign the raw string.
- List values: a typed catch-all has built-in element parsers only, and a repeated option adds enums. Neither
  has a custom-converter path. A converter constraint there (`{*points:point}`, `--to {to:point}*`) used to pass
  the validator and still hit CS0029. It now reports NURU_S010. The message gives the reason: the descriptor
  takes a fourth `{3}` argument, and the subject reads `Catch-all parameter 'x'`.
- generator-76: the unknown-constraint case now covers positional, optional positional, option, catch-all, and
  repeated-option routes (5× NURU_S010). It and the non-constant-alias case require zero compile errors in the
  generated output. I disabled the fallback declaration temporarily and confirmed that both cases fail on
  `compileErrors`.
- Docs: `analyzer.md` NURU_S010 section notes the list-value limits, and that NURU_S010 is the only error.

### How to validate

- Smoke: `ganda runfile cache --clear && dotnet run tests/timewarp-nuru-tests/generator/generator-76-converter-constraint-alias.cs`
- Expect: 4/4 pass. The alias routes compile with zero errors. Unknown, list-converter, and non-constant
  constraints report `NURU_S010` at the `Map(...)` pattern, and the generated code still compiles with no
  CS0103 or CS0029.

### Gates

Fix pass (round 1 M1):

- `./bin/dev build`: passed (0 warnings, 0 errors).
- `ganda runfile cache --clear` then `./bin/dev test`: passed. CI multi-mode: 2227 total, 2221 passed, 6 skipped,
  0 failed. Standalone: 48 files, 0 failed (generator-76 4/4).
- `ganda repo audit`: failed once on `package-pin-currency`. TimeWarp.Build.Tasks 2.0.1-beta.1 was published
  today. I fixed it with `ganda repo audit --fix` (Directory.Packages.props 2.0.0 → 2.0.1-beta.1, boy-scout,
  own commit). After the fix, `./bin/dev build` passed (0 warnings, 0 errors). `./bin/dev test` passed (2227
  total, 2221 passed, 6 skipped; standalone 48 files, 0 failed). The audit rerun passed (Repository passes
  all audit checks).

Implement pass:

- `./bin/dev build`: passed (0 warnings, 0 errors).
- `ganda runfile cache --clear` then `./bin/dev test`: passed. CI multi-mode: 2227 total, 2221 passed, 6 skipped,
  0 failed. Standalone: 48 files, 0 failed (generator-76 included).
- `ganda repo audit`: passed (Repository passes). One advisory, non-blocking warning was already there and is
  not from this change: `vscode-window-icon` (`.vscode/settings.json` is missing). The first commit's hook flagged
  `runfile-executable` on the new generator-76 runfile. I fixed it with `chmod +x` in a follow-up commit.

### Review

Round 1 (general, effort 3): M1 says an unknown constraint still emits CS0103/CS0029 next to `NURU_S010`. This pass fixes it (see Fix pass above). The round-2 review writes the disposition. See `review/round-1/merged.md`.

## Learnings

The restatement says the alias regression "compiles as a consumer (generator-75)". The original checklist only requires a regression test for the alias form and an unknown constraint. generator-75 is the lane that found the bug, and it sits at the TWA0038 line cap. generator-76 compiles the alias routes and checks `NURU_S010`. That placement matches the original checklist. The restatement over-specified the file. The restatement's "diagnostic instead of CS0103/CS0029" matches the original checklist; the miss there is M1, not a misread.

