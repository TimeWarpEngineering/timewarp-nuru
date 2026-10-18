# Round 2 — tests
**Date:** 2026-10-18
**Scope reviewed:** the fix delta `git diff d769f8b1..e316fd32 -- . ':!kanban'` at `e316fd32`, with emphasis on `tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs`, `operation-endpoint-dispatch.cs`, `tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs`, `tests/ci-tests/run-ci-tests.cs`, and `benchmarks/operation-dispatch/`. The review also covers the focused tests, the full CI suite, the benchmark (including `--aot`), the calculator-contracts sample, and smoke runs of samples 02-calculator and 15-httpclient.

## Summary

Every run passed. The full CI suite exits 0. Multi-mode ran 1916 tests: 1910 passed, 6 were skipped, and 0 failed. The standalone phase ran 30 files and 117 tests with 0 failures. The 6 skips already existed on the merge base `5e512c0a`, and the new test files contain no `Skip`. M1, M15, M16 and M21 are fixed.

There are four new issues. One is a bug: the test at `operation-endpoint-derivation.cs:797-798,372` asserts that an omitted member initialized with `= new()` is `null`, but the docs say omission keeps the initializer. Two are suggestions. The M4 test does not reproduce the M4 failure, and the M2 fallback has no automated test (the manual smoke run of it passed). The last is a nit about the benchmark output.

## Runs

- `ganda runfile cache --clear` → cleared 35 entries. The cache was cleared again before the CI run.
- `dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs` → 24/24 passed, exit 0.
- `dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs` → 7/7 passed, exit 0.
- `dotnet run tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs` → 3/3 passed, exit 0.
- `dotnet run tests/ci-tests/run-ci-tests.cs` → exit 0 in 3m00s.
  - Multi-mode: 1916 total, 1910 passed, 0 failed, 6 skipped. `OperationEndpointDerivation` passed 24/24 and `OperationEndpointDispatch` passed 6/6 (the typed HttpClient case is `#if !JARIBU_MULTI`).
  - Standalone: 30 files, 117 tests, 0 failed. This includes `generator-55` (3/3) and `operation-endpoint-dispatch.cs` (7/7, including `Should_construct_a_typed_http_client_on_the_mediator_path`).
- `dotnet build timewarp-nuru.slnx` → succeeded, 0 warnings, 0 errors.
- `dotnet run benchmarks/operation-dispatch/operation-dispatch-overhead.cs` → exit 0. Output: `warmup=50`, `iterations=2000`, `mediator-ticks=6025031`, `direct-ticks=3481945`, `decision=keep-UseDirectInvoke`.
- `dotnet run benchmarks/operation-dispatch/operation-dispatch-overhead.cs -- --aot` → exit 0 in 17s. Output: `mediator-ticks=5750031`, `direct-ticks=3380444`, `aot-mediator-cold-ticks=3660106`, `aot-direct-cold-ticks=2863362`, `aot=ran`. The native binaries are 4,809,264 bytes (mediator) and 4,605,208 bytes (direct). I ran each binary 3 more times by hand: the mediator binary took 6.3, 5.9 and 5.6 ms, and the direct binary took 6.3, 6.2 and 6.6 ms.
- calculator-contracts sample:
  - `calc add --X 2 --Y 3` → `5`, exit 0.
  - `calc round --Value 1.2` → `1`, exit 0.
  - `calc round --Value 1.2 --Mode Up` → `2`, exit 0.
  - `calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'` → `total:6|average:3|notes:1`, exit 0.
  - `calc total --Values 2 --Notes x` → `Error: --Notes is JSON. Pass it with --json-args.`, exit 1.
  - `--capabilities --group-filter calc` → three routes with groupPath `["calc"]`. `IncludeAverage` has `isFlag` false and default `true`. `Notes` has type `json CalculatorContracts.Endpoints.LabeledAmount[]` (no `global::`). `Mode` lists `Nearest`, `Up` and `Down` as allowed values, and its default is `global::CalculatorContracts.Endpoints.RoundingMode.Nearest`.
- 02-calculator (nested handlers, `[NuruRoute]`):
  - Default build: `add 2 3` → `5`, exit 0. The generated code dispatches through the mediator (19 `__GetMediator` references). `divide 6 0` throws the sample's own `DivideByZeroException` (exit 134); the handler throws on purpose.
  - `dotnet run samples/endpoints/02-calculator/calculator.cs -p:TimeWarpMediatorAssembly=false -- add 2 3` → `5`, exit 0. The generated code has 0 `__GetMediator` references and calls `new AddCommand.Handler()` directly. This is the M2 fallback, and it works.
- 15-httpclient: `--help` exits 0. `weather London` prints the weather report and exits 0, so there is no DI resolution error. The generated mediator container registers `IOpenMeteoService` with `new OpenMeteoService(__httpClient_IOpenMeteoService)`.
- Skips: `git diff 5e512c0a..e316fd32 --stat -- tests` touches only `tests/Directory.Build.targets`, `tests/ci-tests/Directory.Build.props`, `run-ci-tests.cs`, and the three new test files. The 6 skips are in `repl-23-key-binding-profiles.cs` (4, task 069), `repl-37-quoted-strings.cs` (1, PSReadLine tab) and `routing-24-enum-option-parameters.cs` (1, IsConfigArg). All 6 exist on `5e512c0a`. `grep Skip` on the three new files finds nothing.

## Prior findings

| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | Neither file calls `.DiscoverEndpoints()`. `CreateApp` maps 21 types with `.Map<T>()` (derivation.cs:30-56), and each dispatch app uses `.Map<T>()`. The full CI run has 0 multi-mode failures, and `OperationEndpointDerivation` passed 24/24 in multi-mode. The one `DiscoverEndpoints()` left is inside the Roslyn-hosted source string in generator-55, which runs standalone. |
| M15 | fixed | `Should_run_mediator_behaviors_on_the_mediator_path` expects the pipeline count to be 1, and `Should_skip_mediator_behaviors_when_direct_invoke_is_set` expects 0 (dispatch.cs:49-78). If `UseDirectInvoke()` did nothing, the second test would fail. The `/tmp` write is gone. Timing lives in `benchmarks/operation-dispatch/operation-dispatch-overhead.cs`, which measures ticks after a warm-up and has an `--aot` cold-start mode. I ran `--aot` (see Runs); the task had recorded AOT as "not run". Remaining measurement weaknesses are in Issue 4. |
| M16 | fixed | `ShouldMatchExceptName` compares the raw JSON value of every option property except `name` (derivation.cs:438-459). The test also compares the sorted endpoint-level key sets of `t508quote` and `t508attr` (derivation.cs:320-324). If any property value differed between the derived option and its attribute twin, the test would fail. |
| M21 | fixed | The test splits `terminal.ErrorOutput` and expects line 0 to equal `https://example.test/t508 Bad input` (dispatch.cs:147-149). It then expects the next call to exit 0, which covers the `Environment.ExitCode` reset. |

## Issues

### Issue 1 — Severity: bug
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:797-798 (asserted at :372); source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs (the `default!` branch); documentation/user/features/neutral-contracts.md:17
- Description: The M6 fix maps an object-creation or invocation initializer to `default!`. The generated code declares `T508Fresh items = default!;` and assigns `Items = items` (generated `NuruGenerated.g.cs:7562,7878`). The member's own `= new()` initializer is therefore overwritten with `null` whenever the option is omitted. The test asserts `items:null`, so it locks in that behavior. The docs (line 17) say that for `string?` or a non-nullable member with an initializer, "Omission keeps the initializer". The requirement says defaults come from initializers. The same code path applies to a non-nullable member, so `public List<Foo> Items { get; set; } = new();` would be `null` in the handler. That is a common shape, and the result is a null-reference failure in a member declared non-nullable. Only `= []` and array creation keep their value (`T508Note[] notes = [];`). The test uses a nullable member, which hides the non-nullable case.
- Suggestion: Preserve the initializer for an unrepresentable default. For example, do not assign that member in the object initializer when no value arrives from argv or `--json-args`, so the property initializer runs. Then change the test to expect `items:set` and add a non-nullable `= new()` member. If dropping the initializer is intended, document it as an exception to the line 17 rule and diagnose a non-nullable member that would become `null`.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:80-94
- Description: M4 was a compile failure (CS0103: `__GetMediator()` not emitted) that occurred only when every source-gen app in the compilation called `UseDirectInvoke()`. `Should_keep_an_external_handler_on_the_mediator_when_direct_invoke_is_set` lives in a file whose other apps (`MediatorApp`, the trace and render apps) do not call `UseDirectInvoke()`. The pre-fix `SourceGenDIRequiresMediator` already returned true in that compilation, and the CI multi-mode compilation is the same. The test would still pass if the `generator-model.cs` change were reverted. Round 1 asked for a standalone test with that combination. Reading `RouteDispatchesThroughMediator` (generator-model.cs:101-113), the fix looks correct, but no test guards it.
- Suggestion: Add a Roslyn-hosted case to generator-55, or a small standalone runfile, in which the only app calls `UseDirectInvoke()` and maps an external-handler `[Operation]`. Assert that the output compiles, or that the generated text contains `__GetMediator`.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs (no case); source/timewarp-nuru-analyzers/generators/models/generator-model.cs:101-113
- Description: M2 (a nested handler must use the 3.0 direct call when no generated mediator exists) has no automated test. Every runfile test compilation gets `TimeWarpMediatorAssembly=true` and profile `Host` from `TimeWarp.Mediator.Generators.props`, so every test runs with a generated mediator. I checked the fallback by hand (`-p:TimeWarpMediatorAssembly=false` on 02-calculator: exit 0, direct `new Handler()`, no `__GetMediator`). A regression would still reach users unnoticed, and M2 was the round-1 finding that broke existing apps.
- Suggestion: Add a standalone runfile with `#:property TimeWarpMediatorAssembly=false` (or `TimeWarpMediatorProfile=Aot`) that runs a nested-handler `[NuruRoute]` and expects exit 0 and its output. Alternatively, add a generator-55-style case that asserts the generated text has no `__GetMediator` and contains `.Handler()`.
- Status: open

### Issue 4 — Severity: nit
- File: benchmarks/operation-dispatch/operation-dispatch-overhead.cs:27,113-117,155-169
- Description: The benchmark has four weaknesses:
  - It prints `decision=keep-UseDirectInvoke` as a constant, not a result computed from the numbers.
  - The tick values are `Stopwatch.ElapsedTicks` (nanoseconds on Linux), but the output does not give the unit. A reader who assumes `TimeSpan` ticks reads 6,025,031 as 0.6 s instead of 6.0 ms (about 3.0 µs per call through the mediator against 1.7 µs direct).
  - The AOT cold start is one sample per binary, and the mediator binary always runs first. One run here gave 3.66 ms against 2.86 ms, but three more runs of each binary were about 6 ms with no consistent difference. So the recorded single-sample gap is noise.
  - The `Mode` enum default in `--capabilities` is `global::...RoundingMode.Nearest`, while `allowedValues` is `Nearest`. Results says "Enum member defaults stay member names", and no test asserts an enum `defaultValue`. Attribute-declared options format the default the same way (this predates the task), so parity holds.
- Suggestion: Print the unit (or µs per call), compute the decision or drop that line, take the median of several cold starts in alternating order, and record the AOT figures and binary sizes in the task Results. Optionally, add a test assertion that pins the enum `defaultValue` format.
- Status: open
