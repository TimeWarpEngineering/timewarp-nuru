# Expose the generated capabilities catalog as a public runtime API

## Description

`--capabilities` is produced by `capabilities-emitter.cs` as a private static `PrintCapabilities(ITerminal, string? groupFilter)` that builds a `CapabilitiesResponse` literal and writes JSON. The document only exists on stdout. `TimeWarp.Nuru.Search` has to spawn the CLI and parse it (`source/timewarp-nuru-search/services/capabilities-client.cs`). An in-process host (MCP adapter, daemon, tests) has no way to enumerate routes, descriptions, option names, defaults, kinds or examples.

Emit the same literal as a public, lazily built static (working name `NuruApp.Capabilities`, or a generated `INuruCatalog` registered in DI) and make `--capabilities` print from it.

## Requirements

- Public, AOT-safe, reflection-free access to the full `CapabilitiesResponse` for the app, including the group filter overload.
- `--capabilities` output is byte-identical before and after (print from the shared instance).
- Add a return-type descriptor to `EndpointCapability` (name and whether it is `Unit`, primitive, or a complex type serialized as JSON), since hosts that map to another protocol need it and it is missing today.
- `TimeWarp.Nuru.Search` keeps working; optionally gets an in-process path when the CLI is the current app.
- Docs: `agent-invocation.md` and `built-in-routes.md` mention the runtime API.

## Goal (implementer restatement)

An in-process host gets the same `CapabilitiesResponse` that `--capabilities` prints, with no reflection and no subprocess. The public surface is on the `NuruApp` instance, because one assembly can build several apps and each has its own routes: `app.Capabilities` returns the full catalog, and `CapabilitiesResponse.FilterByGroup(group)` returns the group-filtered view with the same dot-separated, case-insensitive prefix rule `--capabilities --group-filter` uses. (A `GetCapabilities(group)` method beside the property trips CA1721, and the filter belongs on the document anyway.) The generator emits the catalog literal once per app as a lazily built static. It attaches it to the app by intercepting that app's `Build()` call, so the catalog exists right after `Build()`. RunAsync and RunReplAsync attach it too, as a fallback. `--capabilities` serializes that same instance.

`EndpointCapability` gets a `returnType` descriptor: the CLR type name and a kind of `unit` (void, `Task`, `Unit`), `primitive` (the types the CLI writes as raw text, ISO 8601, or invariant text), or `json` (everything the CLI serializes as JSON). The kind uses the same classification as the CLI output strategy.

Acceptance:

- `app.Capabilities` and `app.Capabilities.FilterByGroup(group)` work after `Build()` without `RunAsync`, are AOT-safe, and return a cached instance for the unfiltered catalog. Calling them on an app the generator did not intercept throws `InvalidOperationException` with a clear message.
- `--capabilities` and `--capabilities --group-filter X` print exactly the serialized `app.Capabilities` and `app.Capabilities.FilterByGroup(X)` (`CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse`). Apart from the new `returnType` member, which is the one intended addition and is the last property of each endpoint, the document does not change.
- `returnType` is in the DTO and the serializer context. It is nullable, so documents from older CLIs still deserialize.
- `TimeWarp.Nuru.Search` still deserializes the document. The optional in-process path for Search is not done: Search runs as the separate `nuru` tool and reads another app's catalog.
- Tests: the catalog equals the printed JSON with and without a group filter, group filtering, return-type kinds for delegate and endpoint routes, availability right after `Build()`.
- `agent-invocation.md` and `built-in-routes.md` describe the runtime API. The changelog lists it.

## Checklist

- [x] Emitter change and public surface
- [x] Return-type descriptor in `EndpointCapability` and serializer context
- [x] Tests: catalog equals printed JSON; group filter; return types
- [x] Docs

## Notes

- Parent: 508. 508-004 depends on this.
- If 508-001 lands first, the DTOs live in the contracts package; the generated instance still lives in the app assembly.

## Session

- Created: 1479989 (2026-10-17)
- Implemented (2026-10-19): implementer claude-opus-5-5 (headless `ganda task work`). Public surface is `NuruApp.Capabilities` plus `CapabilitiesResponse.FilterByGroup`; a `GetCapabilities(group)` method beside the property failed CA1721. Each app's `Build()` is intercepted to attach the catalog. Apps with an interceptable `Build()` and no `RunAsync()` are now emitted. `ganda runfile cache --clear`, then `tests/ci-tests/run-ci-tests.cs`: multi-mode 1938 total, 1932 passed, 6 skipped, 0 failed; standalone 30 files, 0 failed. `dotnet build timewarp-nuru.slnx`: 0 warnings, 0 errors. `ganda repo audit` passed with nothing to fix.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-18T17:24:59Z
- Reviewer: grok session `01a1500b-9a86-7da0-b791-964e7fe71249` (2026-10-19). Effort 2, roster general. Round 1 wrote `review/round-1/general.md` and `review/round-1/merged.md`. M1 is open. No disposition yet.

## Learnings

- Misread: the restatement classifies `returnType` from the awaited CLR type via `GetOutputStrategy`. The task asks for the same classification the CLI uses when it serializes the result (unit, primitive, or JSON). For `OneOf<TSuccess, SharedProblemDetails>` the CLI unwraps the pair and classifies `TSuccess`; the awaited type is the `OneOf`. The implementation follows the restatement, so those endpoints publish the wrapper. See review round 1 M1.

## Results

`NuruApp.Capabilities` returns the generated `CapabilitiesResponse` in process. `CapabilitiesResponse.FilterByGroup(group)` returns the `--group-filter` view. `--capabilities` prints the same cached instance. Each endpoint has a new last member, `returnType`.

- Runtime (`source/timewarp-nuru`):
  - `NuruApp.Capabilities` reads `CapabilitiesProvider`, a `Func<CapabilitiesResponse>` that generated code sets. It throws `InvalidOperationException` when no catalog is attached.
  - `CapabilitiesResponse.FilterByGroup` moves the filter rule out of the emitter: split on `.`, compare segments ignoring case, set `filter.group`.
  - `EndpointCapability.ReturnType` is a nullable `ReturnTypeCapability { Name, Kind }`. `ReturnTypeKind` is `unit`, `primitive`, or `json`, written in camelCase. Both types are in `CapabilitiesJsonSerializerContext`.
- Generator (`source/timewarp-nuru-analyzers`):
  - `CapabilitiesEmitter` emits `BuildCapabilities{suffix}` (the literal, with endpoints as a read-only collection), `GetCapabilities{suffix}` (`LazyInitializer.EnsureInitialized` into a static field), and `PrintCapabilities{suffix}`, which serializes `GetCapabilities()` or `GetCapabilities().FilterByGroup(group)`.
  - `DescribeReturnType` takes the awaited type and drops `global::`. It maps `HandlerInvokerEmitter.GetOutputStrategy` (now `internal`) to a kind: None is `unit`, Json is `json`, and Raw, ISO 8601, and Invariant are `primitive`. `void` and `Task` are named `void`.
  - `DslInterpreter.DispatchBuild` records each `Build()` site and the type that declares that `Build()` (`NuruAppBuilder` or `EndpointBuilder<NuruAppBuilder>`). The site is stored as `BuildInterceptSite` on `IrAppBuilder` and `AppModel.BuildSite`. `InterceptorEmitter` emits one `Build_Intercepted` per site. It calls the original `Build()` and sets `CapabilitiesProvider`. A `HashSet` keeps a shared site from being emitted twice. `ExecuteRouteAsync` sets `app.CapabilitiesProvider ??=` as a fallback.
  - `nuru-generator.cs` used to drop an app with no `RunAsync` or `RunReplAsync` site. It now keeps an app that has a `BuildSite`, so a host that never calls `RunAsync()` still gets the catalog. The full CI suite passed with this change.
- Byte-identical check: four samples were run against the origin-home master (`515a1b46`) and this branch with `--capabilities`: endpoints `03-syntax` (20 endpoints), endpoints `02-calculator` (9), endpoints `14-group-options` (0), and fluent `03-syntax` (25). With the `returnType` members removed by regex, each branch document is byte-identical to master.
- Tests: `tests/timewarp-nuru-tests/capabilities/capabilities-08-runtime-catalog.cs` has 11 tests. They cover the catalog right after `Build()` with no `RunAsync()`, `Build()` on `EndpointBuilder<T>`, the cached instance, printed JSON equal to the serialized catalog with and without a filter, filter rules, delegate return types (`void`, `Task`, `string`, `int`, `Task<int>`, `DateTime`, `int[]`), endpoint return types (`IQuery<string>`, `ICommand<Unit>`, `IQuery<record>`), `returnType` as the last JSON property, round trip plus a legacy document without `returnType`, and the throw when no catalog is attached.
- Docs: `agent-invocation.md` has a `returnType` table and an "In-process catalog" section. `built-in-routes.md` links to it. The changelog has an Added entry. `skills/tw-nuru/SKILL.md` mentions `app.Capabilities` and `returnType`.
- Not done (optional in the task): an in-process path for `TimeWarp.Nuru.Search`. Search is the separate `nuru` tool and indexes another process's catalog. It still deserializes the document, and the search tests pass in CI.
- Pre-existing, not changed: a delegate handler body is copied into generated code as text, so a delegate that returns a type from the test file's namespace (`() => new RtReport(...)`) does not compile (CS0246). The tests use `int[]` for the delegate `json` case and a `[NuruRoute]` endpoint for a record result.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-08-runtime-catalog.cs
dotnet run samples/endpoints/02-calculator/calculator.cs -- --capabilities
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect:

- `capabilities-08-runtime-catalog.cs`: Total 11, Passed 11.
- Calculator `--capabilities`: every endpoint ends with `"returnType"`. `add` is `{"name": "double", "kind": "primitive"}`, `factorial` is `{"name": "TimeWarp.Mediator.Unit", "kind": "unit"}`, and `stats` is `{"name": "EndpointCalculator.Endpoints.StatsResponse", "kind": "json"}`. Apart from those members, the document is byte-identical to master.
- CI runner: multi-mode 1938 total, 1932 passed, 6 skipped, 0 failed. Standalone: 0 failed.
