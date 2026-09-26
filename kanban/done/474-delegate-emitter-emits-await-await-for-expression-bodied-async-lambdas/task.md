# Delegate emitter emits await await for expression-bodied async lambdas

## Description

Found by 443-001 (2026-09-25): Nuru's delegate emitter produces `await await …` for an expression-bodied
async lambda whose body starts with `await`, for example `async (ISender s) => await s.Send(q)`. The
generated code does not compile, so 443-001's test used block bodies instead. With Nuru handlers now
injecting `ISender`, this shape is the natural one to write.

## Requirements

- The emitter produces valid code for expression-bodied async lambdas whose body is an `await` expression,
  including `ConfigureAwait`, parenthesized, and nested-await forms.
- Block-bodied and non-async lambdas are unchanged.
- Generator tests covering each shape, including `async (ISender s) => await s.Send(q)` end to end.

## Checklist

- [x] Emitter fixed
- [x] Generator tests for each shape
- [x] End-to-end ISender lambda test

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.
- Implementation review: effort 1, roster `general`, 1 round, disposition `clean`. Artifacts under `review/`.
  Session: ganda task-work review oracle (Cursor implementer-cursor profile, headless).

## Results

### Root cause

`HandlerDefinition.IsAsync` is true both for `async` lambdas and for non-async lambdas that return a
`Task`, and `HandlerInvokerEmitter.EmitExpressionBodyHandler` prepended `await ` to the expression body
whenever `IsAsync` was set. For `async (ISender s) => await s.Send(q)` that produced
`async Task<string> __handler_N(...) => await await s.Send(q);`. It also broke async bodies that don't
start with `await`, for example `async s => (await s.Send(q)).ToUpperInvariant()` or
`async s => $"[{await s.Send(q)}]"`, which became `await (<non-awaitable>)`.

### Fix

- `HandlerDefinition` gains an optional `HasAsyncModifier` flag (default `false`, so existing call sites
  are unchanged). `HandlerExtractor` sets it from `IMethodSymbol.IsAsync` on the semantic path and from
  the lambda's or anonymous method's `async` keyword on the syntax-only fallbacks.
- `HandlerInvokerEmitter.EmitExpressionBodyHandler` emits an async lambda's expression body verbatim
  inside the `async` local function. Only a non-async `Task`-returning body (for example
  `() => Task.FromResult(1)` or `(ISender s) => s.Send(q)`) is still wrapped as `=> await <body>`.
  This applies to both the value-returning and the void (`Task`) paths.
- Block bodies, non-async lambdas, and the call-site `await __handler_N(...)` are unchanged.

### Tests

- `tests/timewarp-nuru-tests/generator/generator-47-async-expression-body-lambdas.cs`: runtime tests,
  included in the CI multi-mode assembly, so the generator's output for every shape must compile. They
  cover `async (string name, ISender s) => await s.Send(new Gm47GreetQuery(name))` end to end under
  source-gen DI and runtime DI, plus `.ConfigureAwait(false)`, a parenthesized await, nested awaits,
  await inside an interpolated string, a void `IPublisher.Publish` body, a non-async `Task`-returning
  body, and an async block body. 9/9 pass.
- `tests/timewarp-nuru-tests/generator/generator-48-async-expression-body-emission.cs`: a Roslyn-hosted
  generator test (CI-excluded and run in the standalone phase, like generator-28..45) that asserts the
  exact emitted local-function text for each shape. It also covers `async (string name) => name.ToUpperInvariant()`
  (async body with no await). 9/9 pass. With the old emitter restored, 6/9 fail (every async shape), so
  the test catches the regression.
- Full gate: `./bin/dev build` is clean. `dotnet run tests/ci-tests/run-ci-tests.cs` exits 0: the
  multi-mode run has 1747 total, 1740 passed, 7 skipped, and every standalone phase passes.
  `ganda repo audit` passes. It had been failing on a missing `bin/dev`, which is gitignored; running
  `ganda repo audit --fix` built it.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
cd tests/timewarp-nuru-tests/generator
dotnet run generator-47-async-expression-body-lambdas.cs
dotnet run generator-48-async-expression-body-emission.cs
```

Expect: both runs report `Total: 9` and `Passed: 9` with exit code 0. generator-47 compiling at all
proves that `async (ISender s) => await s.Send(q)` and the other expression-bodied async shapes produce
valid generated code. generator-48's assertions confirm the emitted local functions contain the body
verbatim (for example `) => await s.Send(new Q(name));`) and never `await await`.

### Review disposition

- **Outcome:** clean. 1 round, effort 1, reviewer roster: `general`.
- **Final counts:** bug 0, suggestion 0, nit 0. None open, fixed, or wontfix.
- The reviewer confirmed that `HasAsyncModifier` is set correctly on every delegate extraction path and
  survives the `with` copies. It also confirmed that only async expression-body emission changes. On
  re-run, generator-47 and generator-48 each passed 9/9 and `ganda repo audit` passed.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.
