# NURU_H002 false positive on a nested lambda parameter inside a handler

## Description

**3.0.0 launch gate #4 (optional for the tag, but a reader will hit it).** `NURU_H002` ("Closure detected in
lambda handler", `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs:25`,
emitted from `validation/handler-validator.cs`) fires on a nested lambda's own parameter:

```csharp
.Map("sum {*ids:int}").WithHandler((int[] ids) => ids.Sum(i => (long)i))
```

`i` is declared by the inner lambda, not captured from an enclosing scope, so this is not a closure.
The warning becomes an error under `TreatWarningsAsErrors`, which every TimeWarp repo sets.

## Requirements

- The validator distinguishes symbols declared inside the handler lambda (including nested lambda parameters
  and locals) from captured outer symbols. Use the semantic model (`DataFlowAnalysis.Captured` on the
  handler lambda, or symbol containment), not syntax text.
- Tests: a nested-lambda-parameter case that must not report; a real capture case that still reports.

## Checklist

- [x] Fix in `handler-validator.cs` using semantic containment / data flow
- [x] Generator/analyzer tests for both cases
- [x] `changelog.md` Unreleased: Fixed entry
- [ ] PR merged

## Notes

- Found by task 487 (2026-10-09). The blog has no catch-all example, so it does not block copy; it blocks a
  reader who copies the pattern.

## Session

- Created: claude 2412bd45 (2026-10-09)
- Implemented: claude (ganda task work implement, 2026-10-12)

## Results

`HandlerValidator.DetectClosures` and `DetectAnonymousMethodClosures` used to skip a local or
parameter only when its *name* matched the handler's own parameters or a `VariableDeclarator`
in its body. A nested lambda parameter (`i` in `ids.Sum(i => (long)i)`) is an
`IParameterSymbol` whose name is not in that set, so it was reported as a closure. Pattern
variables (`x is int v`) had the same problem.

The name sets are gone. For `ILocalSymbol`/`IParameterSymbol` the validator now calls
`IsDeclaredWithin(symbol, handler)`, which checks whether any of the symbol's
`DeclaringSyntaxReferences` falls inside the handler's span (symbol containment, no syntax
text). Captured outer locals/parameters, including ones used only inside a nested lambda,
still report.

Files:
- `source/timewarp-nuru-analyzers/validation/handler-validator.cs` — containment check.
- `tests/timewarp-nuru-tests/generator/generator-54-h002-nested-lambda-parameter.cs` — new
  Roslyn-hosted test (3 cases). Fails 3/3 against the old validator, passes 3/3 now.
- `tests/ci-tests/Directory.Build.props` — excludes generator-54 from the multi-mode build
  (same CS0433 analyzer/core collision as generator-28/29/38); run it standalone.
- `changelog.md` — Unreleased Fixed entry.

Verification: `dotnet run tests/ci-tests/run-ci-tests.cs` exit 0, no failures; generator-29
and generator-38 (existing H002 tests) still pass standalone.

### How to validate

Smoke:
```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-54-h002-nested-lambda-parameter.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-38-h002-sole-identifier-body.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-29-h002-named-arguments.cs
```

Expect: generator-54 `Total: 3 Passed: 3` (nested lambda parameter and handler locals or
pattern variables do not report NURU_H002; `ids.Sum(i => i + offset)` reports with the message
`variable(s): offset.`, without `i`); generator-38 and generator-29 all pass.
