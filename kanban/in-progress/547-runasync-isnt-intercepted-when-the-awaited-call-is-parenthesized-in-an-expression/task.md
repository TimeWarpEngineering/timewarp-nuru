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
- Review oracle: round 1 by general (cursor, model Grok 4.7), session not reported, max-turns uncapped (budget turn cap 120), effort 2. Open findings M1, M2, M3. Disposition pending a fix pass — 2026-10-26
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120) — 2026-10-26T09:29:26Z
- 2026-10-26 implement-fix-2 (claude, ganda task work): fixed review round 1 M1, M2, M3 with regressions.
- Review oracle: round 2 by general (cursor, model Grok 4.7), session not reported, max-turns uncapped (budget turn cap 120), effort 2. M1, M2, M3 fixed. Open finding M4. Disposition pending a fix pass — 2026-10-26
- 2026-10-26 implement-fix-3 (claude, ganda task work): fixed review round 2 M4 with regressions.
- Review oracle: round 3 by general (cursor, model Grok 4.7), session not reported, max-turns uncapped (budget turn cap 120), effort 2. M1, M2, M3, M4 fixed. Open finding M5. Disposition pending a fix pass — 2026-10-26

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

### Review round 1 fixes

- **M1** (`dsl-interpreter.cs`): the entry point pass skips a call the statement walk already
  bound (`BoundEntryPointCalls`), so one call site never lands on two apps (that was CS9153,
  "intercepted multiple times"). Declarations and assignments now record the app each variable
  holds (`AppAssignments`), and an identifier receiver resolves to the last assignment before the
  call in source order. With no earlier assignment (a local function, another file), it binds only
  when every assignment gave the variable the same app, else it is left for NURU_I001. Known
  limit, unchanged from before 547: when both arms of an `if` assign the local, a later call binds
  to the app from the arm that comes later in the source (the statement walk already did this for
  `await app.RunAsync(...);`).
- **M2** (`run-async-locator.cs`, `intercept-site-extractor.cs`, `app-extractor.cs`):
  `RunAsyncLocator.TryGetMemberCall` returns the name and receiver for `x.M(...)` and `x?.M(...)`
  (a member binding, receiver taken from the enclosing conditional access). `IsPotentialMatch`,
  `LocateCall`, the entry point pass and the field scan use it. `app?.RunAsync(...)` on a traced
  local is intercepted, and on a parameter it reports NURU_I001.
- **M3** (`nuru-generator.cs`): NURU_I001 is suppressed only for a call in the same
  `RunAsyncLocator.EntryPointScope` (method or other member, or the file's top-level statements) as
  a generator error. A call elsewhere still warns when another app failed.
- Tests: generator-71 adds a reassigned local (statement and parenthesized form on each app) and
  `app?.RunAsync(...)`. generator-72 adds `?.` on a parameter, a reassigned local (no compile
  error, no NURU_I001), and a broken app plus a valid app plus a parameter call (one NURU_I001, on
  the parameter). The no-warn test also covers `app?.RunAsync`. Against the round-1 code,
  generator-71 fails to compile with CS9153 and the three new generator-72 tests fail.

### Review round 2 fixes

- **M4** (`dsl-interpreter.cs`, `AppAt`): source order now counts only assignments inside the
  call's innermost lambda, anonymous method or local function. Those bodies usually run later than
  where they are written, so a call in a closure that captures the local falls to the
  every-assignment rule: bound only when every assignment gave the local the same app, else left for
  NURU_I001. The rule also refuses when a closure assigns the local and the walk did not record that
  assignment (`IsAssignedOutsideWalk`). Without that, the outer walk would bind
  `() => { app = ...Build(); return app.RunAsync(...); }` to the outer app while the lambda's own
  interpretation binds it to its app (CS9153). Straight-line calls still bind in source order.
- Tests: generator-72 adds a lambda and a local function declared between two `Build()`
  assignments and invoked after the second (one NURU_I001 each, on the closure's call), and a lambda
  that assigns the captured local its own app (no compile error, no NURU_I001). Against the round-2
  interpreter all three fail. generator-71 adds the lambda-assigns case at runtime and checks that
  each app's handler wrote to its own terminal. The single-app lambda test stays intercepted.
- Docs: NURU_I001 description and changelog name the captured-local case.

### Verification

- `./bin/dev test`: multi-mode 2128 total, 2122 passed, 6 skipped, 0 failed; standalone 41 files, 0 failed.
- Round 1 fix pass: `./bin/dev test` exit 0 (multi-mode and standalone, 41 standalone files, 0 failed);
  `./bin/dev verify-samples` 68/68; `dotnet build timewarp-nuru.slnx --no-incremental` 0 warnings, 0 errors.
- `./bin/dev verify-samples`: 68/68.
- Round 2 fix pass: `./bin/dev test` exit 0 (multi-mode and standalone, 0 failed);
  `dotnet build timewarp-nuru.slnx --no-incremental` 0 warnings, 0 errors.
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

Expect: generator-71 reports `Total: 11 Passed: 11` (each context, including a reassigned local,
`app?.RunAsync`, and a lambda that assigns its own app, ran its handler with exit 0, so every call
was intercepted once); generator-72 reports `Total: 11 Passed: 11` (NURU_I001 on the parameter, `?.`
parameter and method-result receivers, and on a lambda or local function that captures a local
assigned two apps; still on a parameter call when another app has an extraction error).

### Review

Round 3, effort 2, roster general (turn cap 120, session max-turns uncapped). Delta `d627bb2e..20bd7c36`. The implementer restatement matches the task checklist.

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 3 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

Disposition: pending. M1, M2, M3, and M4 are fixed. Open finding M5 (a closure call the statement walk does not enter is intercepted as the closure's app). No `review/disposition.md` until that is closed.

Paths: `review/review-framework.md`, `review/round-3/general.md`, `review/round-3/merged.md`. Rounds 1 and 2 are unchanged under `review/round-1/` and `review/round-2/`.

## Review

- Round 1: Blocked — open M1, M2, M3
- Round 2: Blocked — open M4
- Round 3: Blocked — open M5
