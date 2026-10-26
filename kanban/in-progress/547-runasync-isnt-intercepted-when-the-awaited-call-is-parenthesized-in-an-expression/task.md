# RunAsync isn't intercepted when the awaited call is parenthesized in an expression

## Description

The generator does not intercept `app.RunAsync(...)` when the awaited call is wrapped in parentheses and used as an expression, for example:

```csharp
(await app.RunAsync(["work"])).ShouldBe(0);
```

It compiles, then fails at runtime with "RunAsync was not intercepted". Assigning to a local first works:

```csharp
int exitCode = await app.RunAsync(["work"]);
exitCode.ShouldBe(0);
```

Both forms are ordinary C#, so the generator should intercept both. Found by the 544 implementer while writing `generator-70-per-app-logger-factory.cs`, which uses the local-variable form as a workaround.

## Goal (implementer restatement)

`app.RunAsync(...)` must be intercepted wherever it appears as an ordinary C# invocation, not only
in the statement shapes the DSL interpreter walks (`await x;`, `var y = await x;`, `return await x;`).
Today `(await app.RunAsync(["work"])).ShouldBe(0)` compiles and then throws "RunAsync was not
intercepted" at runtime.

Acceptance criteria:

- The generator finds `RunAsync` / `RunReplAsync` call sites by the invocation's method symbol
  (`NuruApp.RunAsync`), wherever they sit in the scope that builds the app, and registers them on
  the app their receiver resolves to.
- Covered contexts, each with a regression test: parenthesized `(await app.RunAsync(...)).X()`,
  argument to a method, `return await`, ternary, `.ConfigureAwait(false)`, expression-bodied member.
- A `RunAsync` invocation the generator still cannot intercept (receiver not traceable to a
  `Build()` call, e.g. a `NuruApp` parameter) is reported at compile time as a warning, not left
  as a runtime failure. Method-group conversions are not invocations and stay out of scope.
- Existing suites stay green; `ganda repo audit` passes.

## Checklist

- [x] Find where the generator locates `RunAsync` call sites and why a parenthesized `await` (or other expression contexts) is missed; match by the invocation's method symbol, not by the parent syntax shape
- [x] Check other contexts too: argument to a method, `return await app.RunAsync(...)`, ternary, `.ConfigureAwait(false)`, expression-bodied member
- [x] If some context truly can't be intercepted, report a compile-time diagnostic instead of a runtime failure
- [x] Regression test covering each context

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-10-26 implementer (claude, ganda task work): root cause found, fix + NURU_I001 + tests landed.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-26T09:09:50Z

## Results

### Root cause

`DslInterpreter` walks the statements around a `Build()` call and registers a `RunAsync()` site
only when `EvaluateExpression` reaches the call. It evaluated invocations, member access,
identifiers, `await` and assignments, and returned null for everything else. In
`(await app.RunAsync(args)).ShouldBe(0)` the receiver of `ShouldBe` is a parenthesized expression,
so the walk stopped there. A non-DSL method's arguments, ternary branches, lambdas, and the
`...Build().RunAsync()` chain in an expression-bodied member (where only the `Build()` call itself
was evaluated) were missed the same way.

### Fix

- `dsl-interpreter.cs`: after the walk, `RegisterEntryPointCalls` scans the scope (method body, top-level
  statements, or the enclosing member for an expression-bodied member) for every invocation whose
  symbol is `NuruApp.RunAsync` / `NuruApp.RunReplAsync`, and registers it on the app its receiver
  resolves to. Receivers resolve only through state the walk already produced (`VariableState`,
  `BuildCalls`), so no builder chain is evaluated twice. `EvaluateExpression` also unwraps
  parentheses and `!`.
- `ir-app-builder.cs`: `AddInterceptSite` ignores a site it already has (the walk and the new
  pass can both find one call).
- New warning **NURU_I001** (`diagnostic-descriptors.interception.cs`, `nuru-generator.cs`,
  `run-async-locator.cs`): any `NuruApp.RunAsync()` invocation no app intercepts (receiver is a
  parameter, another method's return value, ...) is reported at compile time instead of throwing at
  runtime. A method group is not an invocation and is not reported (repl-48 relies on that). The
  warning is skipped when the generator already reported an error. `RunReplAsync` is not covered:
  its stub is the documented "call AddRepl()" path.
- Tests: `generator-71-run-async-call-contexts.cs` (runtime, CI multi-mode) runs RunAsync in 8
  contexts: parenthesized receiver, method argument, both ternary branches, `.ConfigureAwait(false)`
  (plain and parenthesized), `return await`, expression-bodied member, lambda + `app!`, parenthesized
  `Build()` chain. `generator-72-run-async-not-intercepted-diagnostic.cs` (Roslyn-hosted, standalone,
  added to `CiTestExcludes` and `run-ci-tests.cs`) checks NURU_I001 for a parameter and a method
  result, and no NURU_I001 for the traced contexts, a method group, or an app with an extraction error.
- With the interpreter change reverted, generator-71 fails to compile with 10 NURU_I001 warnings,
  one per call site the old generator missed (only the non-parenthesized `ConfigureAwait` and the
  `return await` cases were already intercepted).
- Docs: changelog Added/Fixed entries, one line in `skills/tw-nuru/SKILL.md`,
  `AnalyzerReleases.Unshipped.md` row.

### Verification

- `./bin/dev test`: multi-mode 2128 total, 2122 passed, 6 skipped, 0 failed; standalone 41 files, 0 failed.
- `./bin/dev verify-samples`: 68/68.
- `dotnet build timewarp-nuru.slnx --no-incremental`: 0 warnings, 0 errors (no NURU_I001 in repo code).
- `ganda repo audit`: passes. The push hook flagged `runfile-executable` for the two new test runfiles;
  `ganda repo audit --fix` set their executable bit.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-71-run-async-call-contexts.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-72-run-async-not-intercepted-diagnostic.cs
```

Expect: generator-71 reports `Total: 8 Passed: 8` (each context ran its handler with exit 0, so
every call was intercepted); generator-72 reports `Total: 5 Passed: 5` (NURU_I001 on the parameter
and method-result receivers only).

