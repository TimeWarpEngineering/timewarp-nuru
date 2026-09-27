# Round 1 — general
**Date:** 2026-09-26
**Scope reviewed:** same as framework, plus surrounding call sites in `handler-extractor.cs`,
`handler-invoker-emitter.cs`, and `handler-definition.cs`

## Summary

The change adds `HandlerDefinition.HasAsyncModifier` (default `false`) and uses it in
`HandlerInvokerEmitter.EmitExpressionBodyHandler` so an `async` lambda's expression body is emitted
verbatim, while a non-async `Task`-returning body is still wrapped with `await`. Risk is low: the flag
only affects the expression-body delegate path, and block bodies, non-async lambdas, and the call-site
`await __handler_N(...)` are untouched.

Verified:

- All three syntax-only fallbacks compute `isAsync` from the `async` keyword alone, so
  `HasAsyncModifier: isAsync` is correct there. The semantic path uses `methodSymbol.IsAsync`, which is
  distinct from `IsAsync = methodSymbol.IsAsync || returnType.IsTask`.
- Every semantic-path caller builds the final definition via `baseDefinition with { LambdaBodySource, IsExpressionBody }`,
  so the flag survives. No other emitter reads `LambdaBodySource`.
- `ForDelegate`, `ForCommand`, and `ForMethod` factories rely on the `false` default, which is correct
  because none of them carry a lambda body.
- generator-48 is excluded from the CI multi-mode assembly and added to the standalone phase, matching
  the generator-28..45 pattern.
- Re-ran generator-47 (9/9) and generator-48 (9/9) and `ganda repo audit` (pass).

## Issues

None.
