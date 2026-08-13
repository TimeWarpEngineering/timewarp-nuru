# Round 3 — general
**Date:** 2026-08-13
**Scope reviewed:** fix commit f8c3936b (M5 + app-extractor diagnostics fix). Read the full delta
plus surrounding code: dsl-interpreter.cs (DispatchWithExample, ResolveArgumentAt,
ExtractStringArgumentAt, InterpretWithDiagnostics/InterpretTopLevelStatementsWithDiagnostics),
app-extractor.cs (ExtractFromBuildCall end to end), extraction-result.cs, build-locator.cs
(IsConfirmedBuildCall), nuru-generator.cs (pipeline wiring, CreateGeneratorModelWithValidation,
Stage A/B split), diagnostic-descriptors.handler.cs (NURU_H005), and the CI wiring
(tests/ci-tests/Directory.Build.props, run-ci-tests.cs). Ran generator-40 (1/1), help-08 (8/8),
capabilities-06 (4/4). Empirically verified both ends of the M5 branch with temporary in-repo
runfiles (deleted after; tree clean): empty-literal command builds and skips silently; const
(non-literal) command fails the build with
`error NURU_S999: WithExample() requires a command string literal` at the invocation site.

## Summary
M5 is correctly fixed with a genuine three-way branch, and the app-extractor change — the higher-
blast-radius part of this commit — holds up under scrutiny: it changes only the Diagnostics
payload on an already-(null-Model) result for semantically confirmed Nuru Build() calls, the sole
consumer of that path already handles null-Model results with diagnostics, and the zero-
diagnostics case degenerates to exactly the old Empty value. The generator-40 test genuinely
pins both fixes independently: a regression of the M5 branch zeroes the S999 count while a
regression of the Failure(diagnostics) fix zeroes both counts. No new issues found.

## Prior findings
- M5: **verified-fixed** — `DispatchWithExample` (dsl-interpreter.cs:1576-1593) now resolves the
  command ArgumentSyntax first via new `ResolveArgumentAt` and branches three ways: argument
  missing → throw ("requires a command string", only reachable in non-compiling code since
  `command` is a required parameter); present-but-not-a-string-literal → throw ("requires a
  command string literal") surfacing as NURU_S999, restoring parity with `DispatchWithAlias`;
  empty literal (`Length == 0`) → silent skip returning `routeBuilder`, preserving M2. Both
  live ends verified empirically end-to-end (build error with accurate invocation span in the
  message vs. clean build with no Examples section). Description keeps the lenient
  WithDescription-precedent behavior, explicitly documented at the call site. The refactor is
  behavior-identical for every other caller: `ExtractStringArgumentAt` is now exactly
  `ResolveArgumentAt` + `ExtractLiteralStringValue`, with the same null-args guard, same
  name-preferred loop, same `Count <= index` positional fallback, same literal-only value
  extraction — and `ExtractStringArgument` (WithDescription/WithAlias/WithName) still passes
  `parameterName = null`, skipping the name loop entirely. Verbatim and raw string literals
  remain `StringLiteralExpression`, so they still extract as before.

## app-extractor.cs scrutiny (item 2)

All four adversarial questions checked; no defect found:

- **Empty vs Failure semantic safety:** `ExtractionResult.Empty` is `new(null, [])` and
  `Failure(diags)` is `new(null, diags)` — same shape, different payload. The changed line is
  the only difference between old and new behavior, and it is reached only after (a)
  `IsPotentialMatch` and (b) `BuildLocator.IsConfirmedBuildCall`, which I verified is semantic:
  it resolves the invocation's method symbol and requires name `Build` with return type
  `NuruApp` (build-locator.cs:52-75). Unrelated/conditional/non-standard `Build()` shapes that
  are not Nuru calls still return Empty at step 2 (app-extractor.cs:238-240), unchanged. A
  confirmed Nuru Build() whose interpretation yields no model was *already* broken pre-fix (no
  interceptor emitted → "RunAsync was not intercepted" at runtime), so surfacing its collected
  diagnostics cannot penalize a working app. The one theoretical newly-erroring case — a
  confirmed Build() chain that is never actually run — was latently broken by the same
  reasoning and erroring on it is the intended design.
- **Consumer handling:** `ExtractFromBuildCall` has exactly one caller
  (nuru-generator.cs:42, verified by repo-wide grep). The implementer's claim is accurate:
  `CreateGeneratorModelWithValidation` collects `result.Diagnostics` from every
  ExtractionResult before it filters on `result.Model is null` (nuru-generator.cs:350-353 vs
  364), and when `uniqueApps` is empty it still returns
  `GeneratorModelWithDiagnostics(null, allDiagnostics)` (line 392), which Stage A's
  RegisterSourceOutput reports unconditionally before its own null-Model early-return
  (lines 148-155). No consumer branches on Empty-vs-Failure identity — they only read Model
  and Diagnostics — so no spurious-diagnostic or double-report path exists.
- **Failure with zero diagnostics:** `Failure([])` produces `(null, [])`, structurally
  identical to Empty, so a confirmed Build() that yields neither model nor diagnostics (e.g.
  the chain lives somewhere the interpreter never walks) behaves exactly as before. The
  ExtractionResult doc-comment calls the (null, empty) state "should not happen", but that
  state was precisely what the old `Empty` return produced on this path — the fix strictly
  reduces how often it occurs.
- **Incrementality:** diagnostics carry Roslyn Locations, so a Failure-bearing result won't
  compare equal across runs — but it feeds `.Collect()` into the deliberately uncached Stage A
  (per the M4 stage-split comments), and the cacheable `NuruGeneratorModel`/Stage B path
  derives from `Model` only, which is unchanged (still null). Healthy apps never reach this
  fallback, so generator-37's pinned caching behavior is unaffected — confirmed by the full
  suite runs.

## generator-40 test (item 3)

Genuinely pins both fixes, independently falsifiable: the `greet` route's H005 (handler
parameter mismatch, caught locally by TryDoneRoute per
handler-parameter-mismatch-exception.cs so interpretation continues) is collected BEFORE the
`status` route's non-literal `.WithExample(SomeHelper.ExampleCommand)` throws and aborts the
chain via the interpreter's outer catch (dsl-interpreter.cs:98-102, which returns
`(null, CollectedDiagnostics)` — verified the drop really did happen downstream in the old
`Empty` fallback, not in the interpreter). If the M5 branch regresses to silent-skip, the chain
completes, a model is produced, and the S999 assertion (count 1, message contains
"command string literal") fails; if the app-extractor fix regresses to Empty, both the S999 and
H005 assertions fail. Harness matches the generator-29/31 Roslyn-hosted pattern (TPA
references, CSharpGeneratorDriver, JARIBU_MULTI guard, TestTag, ModuleInitializer), executable
bit set (100755), no debug leftovers, and CI wiring is complete and consistent: excluded from
multi-mode compilation with the documented CS0433 rationale (Directory.Build.props:36-37, 52)
and added to the standalone list (run-ci-tests.cs:33). Passes standalone (1/1).

## Issues

No new issues found.
