# Harden DSL Interpreter Against Invalid User Code

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M6, M7, M9).

## Description

The DSL interpreter and builders crash or degrade badly on ordinary user mistakes /
partial code (they run inside Roslyn on broken code constantly):

- **M6**: `source/timewarp-nuru-analyzers/interpreter/dsl-interpreter.cs:91,162,227` —
  the fail-soft catch is only `catch (InvalidOperationException)`, yet the code
  recursively evaluates arbitrary partial syntax. Any NullReferenceException /
  ArgumentException escapes → generator transform crash (AD0001). Should catch broadly
  (excluding OperationCanceledException) at these fail-soft boundaries.
- **M7**: `source/timewarp-nuru-analyzers/extractors/builders/route-definition-builder.cs:228` —
  a handler parameter matching no route segment (e.g.
  `.Map("greet").WithHandler((string name)=>...)...`) throws InvalidOperationException.
  The live path converts it into a generic **NURU_S999 "DSL Interpretation Error"** and
  aborts ProcessBlock, silently dropping every other route in the block. Should surface a
  targeted param-mismatch diagnostic for just that route. If reached via the
  non-diagnostic `Interpret`/`InterpretTopLevelStatements` entry points there is no
  try/catch at all → hard crash.
- **M9**: `dsl-interpreter.cs` — `DispatchWithDescription` (~:942), `DispatchWithAlias`
  (~:1500), `DispatchWithGroupPrefix` (~:887) lack the `IsDslBuilderMethod` guard that
  `DispatchWithName` (~:963) has; an unrelated `x.WithDescription("...")` on a non-Nuru
  object throws → bogus NURU_S999 + dropped statements.

## Checklist

- [ ] Broaden fail-soft catches (M6), preserving cancellation
- [ ] Targeted diagnostic for handler-param/route-segment mismatch; other routes survive (M7)
- [ ] Add IsDslBuilderMethod guard to WithDescription/WithAlias/WithGroupPrefix dispatch (M9)
- [ ] Tests for each scenario (mistaken handler, unrelated fluent API, partial code)
- [ ] `ganda runfile cache --clear` + run CI tests
