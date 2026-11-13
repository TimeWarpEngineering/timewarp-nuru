# Handlers under AddBehavior fail CS0103: unqualified inlined lambda types

## Description

One remaining generator bug when a route handler runs under `.AddBehavior(...)`; it produces code that doesn't compile (CS0103). Found by task 551 (PR #367) while writing `telemetry-04`.

The method-group `__command` bug originally filed here was fixed by 552 (`CommandClassEmitter.HasCommandClass` now includes `HandlerKind.Method`). What remains:

**Lambda body with an out-of-namespace type:** under `AddBehavior(...)`, the lambda body is inlined without qualifying type names, so `() => Tel04Capture.Probe()` fails CS0103 when `Tel04Capture` is in another namespace than the generated code expects.

## Goal (implementer restatement)

The generator copies a delegate handler's lambda body, as written, into a local function inside
`TimeWarp.Nuru.Generated.GeneratedInterceptor`. Any name in that body that the caller resolved
through its own scope (its namespace, a `using` inside its namespace block, the enclosing class, a
`using static`, or an extension method's namespace) does not resolve there, so the app fails to
compile (CS0103 / CS0246 / CS1061).

Acceptance:

- A lambda handler body that names a type from the caller's namespace compiles and runs, with and
  without `.AddBehavior(...)`, for expression and block bodies.
- The same holds for a handler that also takes a DI service parameter.
- Names are qualified from the SemanticModel (bound symbols), never from syntax-text guessing.
- A regression test runs in the CI multi-mode assembly, so a regression fails the CI build.

## Checklist

- [x] Reproduce in a generator test
- [x] Inlined lambda bodies: emit fully qualified names via the SemanticModel (or don't inline; invoke the delegate), never syntax text
- [x] Check the same inlining without behaviors, and with delegate handlers that take services
- [x] Regression test, runnable in multi-mode

## Notes

- 2026-11-14 groom: dropped the method-group item; 552 shipped that fix.
- 2026-11-14 implement: behaviors were not the cause. The repro failed the same way without
  `.AddBehavior(...)`; any lambda body naming something outside `TimeWarp.Nuru.Generated`'s scope
  broke. `ExtractUserUsings` only copies compilation-unit usings, so usings inside a namespace
  block and the caller's own namespace never reached the generated file.
- Fix: new `LambdaBodyQualifier` (`source/timewarp-nuru-analyzers/generators/extractors/lambda-body-qualifier.cs`)
  rewrites the body before it is stored in `HandlerDefinition.LambdaBodySource`. It binds every
  name with the SemanticModel: types and namespaces become `global::` names, unqualified static
  members get their containing type, and reduced extension calls `x.M(a)` become `global::N.T.M(x, a)`.
  Interpolation holes that gain a `global::` name are parenthesized (a top-level `:` in a hole
  starts the format string). Locals, parameters, instance members, and type parameters stay as written.
- Limits left as is: a private member of the enclosing class still cannot be reached from the
  generated class; a conditional-access extension call (`x?.M()`) and an explicit-type-argument
  extension call whose receiver is generic stay unqualified.
- Seen, not fixed here: NURU_H002 reports `nameof(SomeType)` inside a handler lambda as a captured
  variable named `nameof`. The test uses `typeof(...).Name` instead.

## Results

- `source/timewarp-nuru-analyzers/generators/extractors/lambda-body-qualifier.cs` (new): rewrites an
  inlined handler body from the SemanticModel so every caller-scoped name is fully qualified.
- `handler-extractor.cs`: parenthesized lambdas, simple lambdas, and anonymous methods store the
  qualified body in `LambdaBodySource` instead of `ToFullString()`.
- `tests/timewarp-nuru-tests/generator/generator-74-inlined-lambda-qualified-names.cs` (new, runs in
  CI multi-mode): a namespace type in an expression body with and without a behavior; a block body
  with types, an enum, a nested record, a generic `List<T>`, a const, `typeof`, and an extension
  call inside interpolation holes; a handler that takes a DI service; an unqualified static call
  from inside the enclosing class.
- `generator-48`: expectations now carry `global::System.Threading.Tasks.Task` and `global::Q`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-74-inlined-lambda-qualified-names.cs
```

Expect: `Total: 5`, `Passed: 5`. Before the fix the file did not compile (CS0103 `Gen74Format`,
CS0246 `Gen74Kind`, CS1061 `Gen74Twice`). The generated file
`artifacts/generated/generator-74-inlined-lambda-qualified-names.cs/.../NuruGenerated.g.cs`
shows `__handler_0() => global::TimeWarp.Nuru.Tests.Generator.Gen74InlinedLambdaQualifiedNames.Gen74Probe.Ping();`.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | pass, 0 errors |
| Test | `./bin/dev test` | pass: multi-mode 2195 total, 2189 passed, 6 skipped (generator-74 5/5 in it); standalone 46 files, 0 failed |
| Audit | `ganda repo audit` | first run: pass with 1 advisory (`vscode-window-icon`, `.vscode/settings.json` missing); `ganda repo audit --fix` wrote the ignored local file; rerun: "Repository passes all audit checks." |

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-11-14 implement (claude, ganda task work): merged origin/master, reproduced in generator-74,
  added `LambdaBodyQualifier`, updated generator-48 expectations. Boy-scout: audit `--fix` for the
  `vscode-window-icon` advisory (ignored local file, nothing committed).
