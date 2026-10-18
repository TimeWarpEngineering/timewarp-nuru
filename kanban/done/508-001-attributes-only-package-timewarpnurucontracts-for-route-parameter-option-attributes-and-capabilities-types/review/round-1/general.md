# Round 1 — general
**Date:** 2026-10-18
**Scope reviewed:** `git diff 5e512c0a..f522c6e2`, generator, emitters, models, build task, `NuruCliBinding`, `UseDirectInvoke` (read only, no build)

## Summary

The derivation of operation endpoints holds together. The new model fields are equatable primitives and strings, so no `ISymbol` or `SyntaxNode` reaches the incremental pipeline. The main risk is that this branch makes mediator dispatch the default for every existing `[NuruRoute]` nested-handler endpoint. That breaks apps that have no generated mediator, handlers that depend on a typed HttpClient, and (as generated code) apps that use `UseDirectInvoke()` while also having a non-nested-handler endpoint. Some derived members also produce generated code that does not compile: certain scalar types have no converter, and some initializer defaults are captured as symbol names.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:300 (and :372-375); interceptor-emitter.cs:449-451
- Description: Existing `[NuruRoute]` endpoints that have a nested `Handler` now go through `IMediator.Send` unless the app calls `UseDirectInvoke()`. Before this branch they were always invoked directly. When `hasGeneratedMediator` is false (for example the mediator profile is `Aot` or `Link`, or `TimeWarpMediatorAssembly` is not true), `__GetMediator()` is emitted with a body that throws `InvalidOperationException`. In runtime-DI apps, `GetRequiredService<IMediator>` is called without `AddGeneratedMediator()` registered. In both cases every existing endpoint fails at runtime, which breaks the "existing apps keep working unchanged" requirement.
- Suggestion: Use direct invoke when a nested handler exists and no generated mediator is available (pass `HasGeneratedMediator` into the invoker emitter), or report a build-time diagnostic instead of throwing at runtime.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:474-482
- Description: The source-gen mediator container registers only framework services and `allServices`. Typed HttpClient registrations (`AddHttpClient<TService, TImpl>`) are `HttpClientConfiguration`s, not `ServiceDefinition`s. Direct invoke resolved them through `httpClientConfigurations` (handler-invoker-emitter.cs:556-571), but the mediator container does not register them. A nested handler such as `samples/endpoints/15-httpclient/endpoints/CurrentWeatherEndpoint.cs:27` (`Handler(IOpenMeteoService openMeteo)`) therefore fails when the mediator resolves the handler.
- Suggestion: Register each `HttpClientConfiguration` in `EmitSourceGenMediator`, and add a test for a nested handler with a typed HttpClient dependency under mediator dispatch.
- Status: open

### Issue 3 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/models/generator-model.cs:85-93
- Description: `SourceGenDIRequiresMediator` returns false when every source-gen app calls `UseDirectInvoke()`. But `directInvoke` applies only when `NestedHandlerTypeName` is not null (handler-invoker-emitter.cs:300). An `[Operation]` endpoint, or a `[NuruRoute]` class whose handler is defined elsewhere, still emits `__GetMediator()`, and so does `EmitMissingHandlerStartup`. `EmitSourceGenMediator` is not emitted in that case, so the generated code fails with CS0103.
- Suggestion: Treat a route as needing the mediator when it is a command route and either the app does not use direct invoke or `NestedHandlerTypeName is null`.
- Status: open

### Issue 4 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:682-718 (vs endpoint-extractor.cs:1423-1443)
- Description: `IsScalar` accepts `sbyte`, `ushort`, `uint`, `ulong`, `DateOnly`, `TimeOnly` and `BigInteger`. `GetTypeConstraintFromClrType` has no mapping for any of them, so the generator skips conversion. The local is declared `string?` and assigned straight to the typed property, which fails with CS0029 or CS0266. Repeated arrays of these types fail the same way.
- Suggestion: Add type constraints and converters for these types, or drop them from `IsScalar` so they bind as JSON members.
- Status: open

### Issue 5 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1031-1047 (consumed at route-matcher-emitter.cs:1607, json-args-emitter.cs:929, capabilities-emitter.cs:331)
- Description: `ExtractPropertyDefaultValueFromSymbol` tries `GetSymbolInfo` before reading the constant value, and derivation now sends every member that has an initializer through this function. Three cases go wrong:
  - **Constructor initializers.** `= new()` or `= new Foo()` resolves to the constructor and is emitted as `global::Ns.Foo.Foo`, which does not compile. A method-call initializer produces a method group, which also does not compile.
  - **Numeric suffixes.** Invariant formatting drops the suffix, so `decimal X = 1.5m` and `float X = 1.5f` are emitted as `1.5` (CS0664).
  - **Named constants.** A `const bool` that is `true` becomes a symbol name, so the bool-true value-option rule is skipped and `--capabilities` shows the symbol name instead of the value.
- Suggestion: Check `GetConstantValue` first. Emit type-correct numeric suffixes. Emit symbol references only for fields and enum members. For any other initializer, use `default!` and leave the default out of capabilities.
- Status: open

### Issue 6 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:64; operation-endpoint-extractor.cs:408-413
- Description: Any class with `[Operation]` becomes an endpoint, even when it has no TimeWarp.Mediator kind. The extractor then falls back to `ICommandHandler<T, Unit>`, so the user gets NURU_H008 (an Error) and an invalid dispatch. The spec defines an endpoint as `[Operation]` plus a mediator kind, so an HTTP-only `[Operation]` type now breaks any project that references Nuru.
- Suggestion: When a class has `[Operation]` and no `[NuruRoute]` and implements no mediator kind, skip it or report a dedicated diagnostic.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:46-64, 73-76
- Description: A class with both `[NuruRoute]` and `[Operation]` matches both `ForAttributeWithMetadataName` pipelines. The routes are deduplicated later (`DistinctBy` at :443), but the diagnostics stream is not, so every diagnostic for that class is reported twice.
- Suggestion: Skip `[NuruRoute]` classes in the `[Operation]` pipeline, or deduplicate by class before splitting routes from diagnostics.
- Status: open

### Issue 8 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:439-555; endpoint-extractor.cs:1110
- Description: `ExtractHandler` calls `FindRenderer` for every endpoint, including every existing `[NuruRoute]` endpoint. `FindRenderer` walks every type in the compilation and in every referenced assembly, including the BCL. Unlike `HasVisibleHandler`, it does not skip System and Microsoft assemblies. `HasVisibleHandler` does a similar walk per handlerless endpoint. The transform reruns on every compilation change, so the cost scales with the number of endpoints on every IDE edit.
- Suggestion: Build the renderer and handler indexes once from `CompilationProvider` as an equatable map. At minimum, skip framework assemblies.
- Status: open

### Issue 9 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:56
- Description: The predicate `node is ClassDeclarationSyntax` excludes `RecordDeclarationSyntax`, so `[Operation] public sealed record FooQuery : IQuery<T>` is ignored with no endpoint and no diagnostic.
- Suggestion: Support record classes, or report a diagnostic.
- Status: open

### Issue 10 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1604-1619
- Description: On the argv path, a required JSON member is declared `default!` and the command runs with null. Only `--json-args` enforces `Required`.
- Suggestion: When the member is required and no `--json-args` was passed, fail with "pass --X via --json-args".
- Status: open

### Issue 11 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:81-82
- Description: When both attributes are present, a `[NuruRoute]` with no `Description` replaces the `[Operation]` description with nothing.
- Suggestion: Use `routeDescription ?? operationDescription`.
- Status: open

### Issue 12 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:272
- Description: The capabilities type is built from `FullyQualifiedFormat`, so the CLI schema shows `json global::Ns.T[]`, which leaks the C# `global::` alias into the catalog.
- Suggestion: Use a format that omits the global namespace.
- Status: open

### Issue 13 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1156, 1203-1240
- Description: The missing-handler check runs before every dispatch, including `--help`, `--version`, `--capabilities` and REPL commands. With NURU_H008 suppressed and a handler really missing, the whole CLI exits 1, not just the affected command.
- Suggestion: Run the check when the affected route is dispatched, or skip it for built-in routes.
- Status: open

### Issue 14 — Severity: nit
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:41-49
- Description: `Should_match_direct_invoke_and_mediator_results` does not compare the two results, despite its name. It also writes to the hard-coded path `/tmp/t508-overhead.txt`, which leaves a side effect in CI and does not work on Windows.
- Suggestion: Assert that both runs give the same output and exit code, and use `Path.GetTempPath()` or drop the file write.
- Status: open
