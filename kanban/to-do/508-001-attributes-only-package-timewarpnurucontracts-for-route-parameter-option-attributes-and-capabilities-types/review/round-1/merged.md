# Round 1 — merged findings
**Date:** 2026-10-18
**Sources:** general, tests, plan_alignment
**Reviewed commit:** `f522c6e2` (vs merge-base `5e512c0a`)

The orchestrator checked the main claims against the code:
- `generator-model.cs:85-98`, `handler-invoker-emitter.cs:300,372`, `interceptor-emitter.cs:449-485` — confirmed.
- `operation-endpoint-extractor.cs:682-718` and `endpoint-extractor.cs:1031-1047` — confirmed.
- `diagnostic-descriptors.handler.cs` (NURU_H008 `Error`) — confirmed.
- The CI log shows multi-mode `Failed: 6`.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 8 | 0 | 0 |
| suggestion | 9 | 0 | 0 |
| nit | 4 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:33; tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:31,61,87,112,127
- Description: The new tests build their apps with `.DiscoverEndpoints()`. In CI multi-mode, a `[NuruRoute("")]` endpoint from another test file matches the inputs these tests expect to be rejected. `dotnet run tests/ci-tests/run-ci-tests.cs` exits 1: multi-mode ran 1909 tests, 1897 passed, **6 failed** (all in `OperationEndpointDerivationTests`). The repo rule says CI tests must use `.Map<TEndpoint>()`.
- Suggestion: Use `.Map<T508…>()` for each needed type in both files, then re-run the full CI suite after `ganda runfile cache --clear`.
- Source: tests, plan_alignment (Issue 9)
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:300,372-375; interceptor-emitter.cs:449-451
- Description: Mediator dispatch is now the default for existing `[NuruRoute]` nested-handler endpoints. When no generated mediator is available (`hasGeneratedMediator` false: mediator profile `Aot`/`Link`, or `TimeWarpMediatorAssembly` not true), `__GetMediator()` throws `InvalidOperationException`. In runtime-DI apps, `GetRequiredService<IMediator>` runs without `AddGeneratedMediator()`. Existing endpoints that worked on 3.0.0 now fail at runtime, which breaks "existing apps keep working unchanged". The change is not in the changelog.
- Suggestion: Fall back to direct invoke for nested-handler endpoints when no generated mediator exists, or keep direct invoke as the default for nested-handler `[NuruRoute]` endpoints and send only `[Operation]` endpoints through the mediator. Alternatively, diagnose at build time. Record the dispatch-default change in the changelog.
- Source: general (Issue 1), plan_alignment (Issue 2)
- Disposition notes:

### M3 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:474-482
- Description: The source-gen mediator container registers only framework services and `ServiceDefinition`s. Typed HttpClient registrations (`AddHttpClient<TService,TImpl>`, which are `HttpClientConfiguration`s) are not registered there. Direct invoke used to resolve them (handler-invoker-emitter.cs:556-571). A nested handler that depends on a typed client, such as `samples/endpoints/15-httpclient` `Handler(IOpenMeteoService)`, fails to resolve under mediator dispatch.
- Suggestion: Register each `HttpClientConfiguration` in `EmitSourceGenMediator`, plus anything else `ResolveServiceForCommand` special-cases. Add a test, and smoke-run sample 15.
- Source: general (Issue 2), plan_alignment (Issue 2)
- Disposition notes:

### M4 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/models/generator-model.cs:85-98
- Description: When every source-gen app calls `UseDirectInvoke()`, `SourceGenDIRequiresMediator` returns false and `__GetMediator()` is not emitted. Endpoints without a nested handler (`[Operation]` with an external handler, `[NuruRoute]` with an external handler) and `EmitMissingHandlerStartup` still call it, so the generated code fails with CS0103. This contradicts the documented "an endpoint with no nested handler still goes through the mediator".
- Suggestion: Require the mediator when any route of a direct-invoke app has `NestedHandlerTypeName is null`. Add a standalone test that combines `UseDirectInvoke()` with an external-handler operation.
- Source: general (Issue 3), plan_alignment (Issue 1)
- Disposition notes:

### M5 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:682-718 (vs endpoint-extractor.cs:1423-1443)
- Description: `IsScalar` accepts `sbyte`, `ushort`, `uint`, `ulong`, `DateOnly`, `TimeOnly` and `BigInteger`, but `GetTypeConstraintFromClrType` has no mapping for them. The generated local is `string?` and is assigned straight to the typed property (CS0029/CS0266). Arrays of these types fail the same way.
- Suggestion: Add type constraints and converters for these types, or drop them from `IsScalar` so they bind as JSON members. Add a derivation test for each type that stays scalar.
- Source: general (Issue 4)
- Disposition notes:

### M6 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1031-1047 (consumed at route-matcher-emitter.cs:1607, json-args-emitter.cs:929, capabilities-emitter.cs:331)
- Description: Derived initializer defaults are captured incorrectly in three ways:
  - `GetSymbolInfo` runs before the constant value, so `= new()` / `= new Foo()` becomes `global::Ns.Foo.Foo`, a method call becomes a method group, and a `const bool` that is `true` becomes a symbol name. That skips the bool-true value-option rule and shows the symbol name in `--capabilities`.
  - Invariant formatting drops numeric suffixes, so `decimal X = 1.5m` and `float X = 1.5f` are emitted as `1.5` (CS0664).
- Suggestion: Read `GetConstantValue` first and emit type-correct literal suffixes. Emit symbol references only for fields and enum members. For any other initializer, use `default!` and omit the default from capabilities. Add tests for `= new()`, `decimal` and `float` initializers, and a `const bool` initializer.
- Source: general (Issue 5)
- Disposition notes:

### M7 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:64; operation-endpoint-extractor.cs:408-413
- Description: Any `[Operation]` class becomes a Nuru endpoint, even when it implements no TimeWarp.Mediator kind. The extractor falls back to `ICommandHandler<T, Unit>` and reports NURU_H008 (an Error), so a project that references Nuru and has an HTTP-only `[Operation]` type stops building. The task defines an endpoint as `[Operation]` plus a mediator kind.
- Suggestion: Skip `[Operation]` classes (without `[NuruRoute]`) that implement no `IQuery<T>`, `ICommand<T>` or `IIdempotentCommand<T>`, or report a dedicated non-error diagnostic. Add a test.
- Source: general (Issue 6)
- Disposition notes:

### M8 — Severity: bug — Status: open
- File: documentation/user/features/neutral-contracts.md:53; source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs:105-112
- Description: The docs say a handler that is not visible "is still accepted at build time" and that startup then exits 1. The code reports NURU_H008 as `DiagnosticSeverity.Error`, so the build fails unless an Error is suppressed. The requirement is "a diagnostic … where it can tell, otherwise a startup error", which matters for the CLI-forwarding / daemon split this task exists for. Results says "the route still exists so startup can report a missing registration", which is only true after suppression. generator-55 checks only that the startup text is emitted.
- Suggestion: Make NURU_H008 a Warning so the startup path is reachable by default, or keep it an Error and correct the docs and Results to say suppression is required. Align `AnalyzerReleases.Unshipped.md` with the chosen severity.
- Source: plan_alignment (Issue 3)
- Disposition notes:

### M9 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:46-64,73-76
- Description: A class with both `[NuruRoute]` and `[Operation]` matches both `ForAttributeWithMetadataName` pipelines. Routes are deduplicated (`DistinctBy` at :443) but diagnostics are not, so each of that class's diagnostics is reported twice.
- Suggestion: Skip `[NuruRoute]` classes in the `[Operation]` pipeline, or deduplicate by class before splitting the stream.
- Source: general (Issue 7)
- Disposition notes:

### M10 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:439-555; endpoint-extractor.cs:1110
- Description: For every endpoint, including every existing `[NuruRoute]` endpoint, `FindRenderer` walks all types in the compilation and in every referenced assembly, including the BCL. `HasVisibleHandler` does a similar walk per handlerless endpoint. Both rerun on each compilation change, which costs IDE responsiveness in large apps.
- Suggestion: Build the renderer and handler indexes once from `CompilationProvider`. At minimum, skip System and Microsoft assemblies in `FindRenderer`.
- Source: general (Issue 8), plan_alignment (Issue 5)
- Disposition notes:

### M11 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:528
- Description: The requirement says a *registered* `IResponseRenderer<TResponse>` is honored. The code constructs any *visible* renderer that has a public parameterless constructor. A DI-registered renderer is ignored, and a renderer that has constructor dependencies is silently skipped.
- Suggestion: Resolve the renderer from registered services, or document "visible, parameterless" and prefer a registered renderer.
- Source: plan_alignment (Issue 5)
- Disposition notes:

### M12 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:56
- Description: The `[Operation]` predicate `node is ClassDeclarationSyntax` excludes records. `[Operation] sealed record FooQuery : IQuery<T>` is silently ignored.
- Suggestion: Support record classes, or report a diagnostic.
- Source: general (Issue 9)
- Disposition notes:

### M13 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1604-1619
- Description: On the argv path, a required JSON member is `default!` and the handler runs with null. Only `--json-args` enforces `Required`.
- Suggestion: When the JSON member is required and there is no `--json-args`, fail the call with "pass --X via --json-args".
- Source: general (Issue 10)
- Disposition notes:

### M14 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:223
- Description: `GetMembers()` returns only declared members, so public settable properties inherited from a base request are not derived. The task and docs say "every public settable member".
- Suggestion: Walk base types, skipping group marker bases, or document the declared-only rule.
- Source: plan_alignment (Issue 7)
- Disposition notes:

### M15 — Severity: suggestion — Status: open
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:41-105
- Description: The overhead measurement and the direct-invoke test are weak:
  - The measurement is one in-process JIT run at ms resolution. The mediator run goes first, both 30-call loops round to 0 ms, and there is no AOT or cold-start figure.
  - `Should_match_direct_invoke_and_mediator_results` uses a `[NuruRoute]` endpoint, asserts only `direct:ok`, and would pass if `UseDirectInvoke` did nothing.
  - The test writes the hard-coded path `/tmp/t508-overhead.txt` from a CI test.
- Suggestion: Assert a behavior that differs between the two paths, for example a mediator pipeline behavior that runs only under mediator dispatch. Move the timing to a benchmark runfile that measures in ticks with warm-up and AOT cold start. Record the numbers and the keep-the-opt-out decision in the task or docs. Drop the `/tmp` write.
- Source: general (Issue 14), tests (Issue 2), plan_alignment (Issue 4)
- Disposition notes:

### M16 — Severity: suggestion — Status: open
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:295-300
- Description: The capabilities parity check compares only the key names of one option. It does not compare values (`type`, `required`, `isFlag`, `isRepeated`, `defaultValue`, `description`) or endpoint-level shape.
- Suggestion: Compare every option property except `name`, and compare the endpoint-level key sets of a derived endpoint and its attribute twin.
- Source: tests (Issue 3)
- Disposition notes:

### M17 — Severity: suggestion — Status: open
- File: changelog.md:10
- Description: The changelog has one `### Changed` line. It leaves out the new public API (`UseDirectInvoke()`, `[NuruCliBinding(Group)]`), NURU_H008, renderer and problem rendering, the `Environment.ExitCode` reset, external-handler `[NuruRoute]` endpoints, and the dispatch default change (M2).
- Suggestion: Add an `### Added` section, and record the behavior changes under `### Changed`.
- Source: plan_alignment (Issue 6)
- Disposition notes:

### M18 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1156,1203-1240
- Description: The missing-handler check runs on every `ExecuteRouteAsync`, before `--help`, `--version`, `--capabilities` and REPL commands. One unregistered operation makes the whole CLI exit 1.
- Suggestion: Check when the affected route is dispatched, or once per process after the built-ins.
- Source: general (Issue 13), plan_alignment (Issue 8)
- Disposition notes:

### M19 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:81-82
- Description: A `[NuruRoute]` override with no `Description` wipes out the `[Operation]` description.
- Suggestion: Use `routeDescription ?? operationDescription`.
- Source: general (Issue 11)
- Disposition notes:

### M20 — Severity: nit — Status: open
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:272
- Description: The JSON member capabilities type uses `FullyQualifiedFormat`, so the catalog shows `json global::Ns.T[]`.
- Suggestion: Omit the `global::` alias.
- Source: general (Issue 12)
- Disposition notes:

### M21 — Severity: nit — Status: open
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:132
- Description: The requirement puts the problem type and title on the *first* stderr line. The test uses `ErrorContains`, which passes no matter which line holds the text.
- Suggestion: Assert that stderr line 0 equals `https://example.test/t508 Bad input`.
- Source: tests (Issue 4)
- Disposition notes:

## Duplicates / conflicts

- M1: tests Issue 1 + plan_alignment Issue 9 (the nit was raised to bug because the CI suite actually fails).
- M2: general Issue 1 + the mediator-default part of plan_alignment Issue 2. M3: general Issue 2 + the HttpClient part of plan_alignment Issue 2.
- M4: general Issue 3 + plan_alignment Issue 1.
- M10: general Issue 8 + the scan-cost part of plan_alignment Issue 5. M11: the registered-vs-visible part of plan_alignment Issue 5.
- M15: general Issue 14 + tests Issue 2 + plan_alignment Issue 4.
- M18: general Issue 13 + plan_alignment Issue 8.
- general Issue 7 (duplicate diagnostics) was recorded as suggestion M9: the only effect is duplicated reporting.

## Verdict

21 open findings (8 bugs). A fix pass is needed on this task id, followed by a round-2 re-review. The implementer either fixes each open finding or marks it `wontfix` here with a rationale. All bugs must be fixed, or resolved by a human decision. Disposition is not written yet.
