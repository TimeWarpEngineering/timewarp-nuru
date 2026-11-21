# Improve Nuru generated startup performance

## Goal
Make Nuru measurably faster than its peers wherever framework work (not process startup) is what is measured. Aim for the full builder (with Mediator) to start in single-digit ms. AOT launch time will stay about 3 ms, because that is the process-startup floor.

## Goal (implementer restatement)

Make the work Nuru itself does at startup (not process and runtime startup) cheaper and
measurable, and prove each gain with the 578 `dev benchmark run <name> --base <ref>` verdict.

Acceptance:

- The benchmark suite measures Nuru fairly: the same argv for every framework (in-process and
  AOT), honest labels, a warm steady-state job beside the cold-start job, and named `dev
  benchmark` entries that isolate Nuru's in-process startup (first call in a fresh JIT process)
  and its warm per-call cost.
- Baseline numbers are recorded in Results before any emitter change.
- Generated code skips work no route needs (configuration build, DI, tracked-disposal
  teardown, the `--json-args` machinery) on the plain CLI path, without behavior change.
- Matching avoids per-call allocations that can be compile-time constants.
- The December 2025 "full builder with Mediator" regression is explained (and fixed or filed).
- After numbers come from the `dev benchmark run ... --base master` verdict JSON, not hand-copied
  timings. Items too large for this task are filed as follow-ups with the measured reason.
- Build, touched test files, and `ganda repo audit` pass.

## Findings
- Headline AOT launch numbers (benchmarks/aot-benchmarks/results/2026-01-21-aot-benchmark.md, .NET 10.0.2, hyperfine) are dominated by about 2.5-3 ms of process and runtime startup: ConsoleAppFramework 3.2 ± 0.6 ms, Nuru 3.2 ± 0.6 ms, System.CommandLine 3.8 ms. The ±0.6 ms noise hides Nuru's extra work.
- In-process (benchmarks/timewarp-nuru-benchmarks/results/2025-12-20-analysis.md, BenchmarkDotNet cold start): Nuru takes 3.98 ms and 14.8 KB, against ConsoleAppFramework v5 at 0.79 ms and 288 B. That is about 5x the time and about 50x the memory. ConsoleAppFramework emits a bare synchronous argument loop. Nuru builds an app object, config, DI, an async path and a route table at startup.
- Full builder with Mediator: 131.96 ms and 221 KB, last of 11. July 2025 was 34.42 ms, so it is about 4x worse.
- Generated startup work (source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs): ConfigurationBuilder().Build() (~1106), ServiceCollection + AddGeneratedMediator + BuildServiceProvider (~457-481, ~825-835), async tracked-disposal teardown (~639-740), NuruAsyncLease (~1133), check-updates (~1400). Route matching is in route-matcher-emitter.cs.

## Improvements
- Skip config and DI initialization when no route needs them (lazy or omitted at generation time).
- Generate a synchronous path when no handler awaits.
- Emit the route table at compile time instead of building it at startup, and avoid allocations while matching.
- Find the cause of the full-builder regression since July 2025.

## Benchmark suite improvements (do these first so the gains above can be measured)
- Refresh the results; the latest AOT run is 2026-01-21 and the latest in-process run is 2025-12-20.
- Track benchmarks in CI (.github/workflows/workflow.yml runs none today).
- Add a warm, steady-state in-process benchmark next to the cold-start job (benchmarks/timewarp-nuru-benchmarks/program.cs).
- Use the same arguments for every framework. run-benchmark.sh passes ConsoleAppFramework `--i 13 --b` and the others `-i 13 -b`. The in-process Nuru case uses its own cached args with an extra `test` literal.
- Check the builder label. commands/nuru-direct-command.cs calls CreateBuilder, but the results report it as "Direct (CreateEmptyBuilder)".
- Add a native-compiled lightweight-builder app; bench-nuru/Program.cs uses only the full CreateBuilder.

## Depends on

- 578

## Checklist

- [x] Benchmark suite fixes listed above (same argv per framework, builder label, native lightweight-builder app, warm steady-state job), measured through the 578 `dev benchmark` contract. No lightweight builder exists any more (CreateSlimBuilder and CreateEmptyBuilder were removed in January 2026), so there is no second native app to add; CI tracking is a follow-up.
- [x] Baseline numbers recorded in Results before any change to the emitters
- [x] Skip config and DI setup when no route needs them (DI was already built on first read)
- [ ] Synchronous path when no handler awaits (not done; follow-up proposed in Results)
- [ ] Compile-time route table (matching is already generated at compile time; allocation-free matching is a proposed follow-up)
- [x] Full-builder regression since July 2025 explained and fixed or filed (the regressed path was removed in task 330; see Results)
- [x] After numbers from `dev benchmark run <name> --base <ref>` (the 578 contract); the verdict it prints decides, not hand-copied numbers
- [ ] `ganda runfile cache --clear`, then CI tests green (cache cleared, covering files green and the CI assembly compiles; the full CI run is on the pull request)

## Related
- TimeWarpEngineering/timewarp-mediator: "Improve Mediator send performance" task 020: https://github.com/TimeWarpEngineering/timewarp-mediator/blob/master/kanban/to-do/020-improve-mediator-send-performance/task.md

## Session

- Created: 1508201 (2026-10-13)
- 2026-11-14 groom: added Checklist and Depends on 578 so the speedups are measured through the shared benchmark contract.
- 2026-11-17 groom: 567 (measurement bar) archived, covered by the tw-perf-sweep skill; the "after numbers" box now names the 578 verdict.
- 2026-11-21 implement (ganda task work, implement oracle, Claude Opus 5.5): benchmark suite fixes in commit 5bebefd8, then signal, configuration and dispatch-split changes measured against it.

## Results

### Baseline (before any emitter change)

`benchmarks/startup-overhead/startup-overhead.cs` on the unchanged emitter (5 runs, 2026-11-21):
first-call-us 18,600 to 20,900, warm-call-ns 5,720 to 5,890, warm-call-bytes 3,432. JIT took 11.2 ms
of the first call (85 methods). The base side of each verdict below (commit 5bebefd8, benchmarks
only, product code equal to master) is the same baseline measured interleaved.

### Where the first call went

Measured in a scratch copy of the generated code, timed in a fresh JIT process:

- `Console.CancelKeyPress` subscription in `NuruApp.RunCliAsync`: about 5 ms cold. TimeWarpTerminal
  forwards it to `Console.CancelKeyPress`, whose first subscription initializes the console. A warm
  run also paid about 2 µs to create and dispose three Posix registrations.
- `--json-args` reader and selection inline in `ExecuteRouteAsync`: about 2.7 ms of JIT, and it
  loaded System.Text.Json on every run.
- Blank `ConfigurationBuilder().Build()`: about 1 ms cold, 0.57 µs warm.

### Changes

- `source/timewarp-nuru/invocation/nuru-app.cli-run.cs`: Posix SIGINT, SIGQUIT and SIGTERM are
  registered once per process and signal every CLI run in progress. `CancelKeyPress` is subscribed
  only for a terminal that is not `TimeWarpTerminal` (TestTerminal raises it) or when SIGINT/SIGQUIT
  cannot be registered. Ctrl+Break (SIGQUIT) still maps to 130, as `Console.CancelKeyPress` did.
  Checked with real signals on a built app: SIGINT 130, SIGQUIT 130, SIGTERM 143, each immediate.
- `interceptor-emitter.method-body.cs`: a plain app (no full configuration, no generated mediator
  container, no runtime DI) builds the blank configuration root only when the emitted body or a
  dispatch method reads `configuration`; otherwise it passes null. `EnsureServicesInitialized` takes
  `IConfigurationRoot?`. The `InvokeAsync` switch and the `--json-args` selection are emitted as
  `__DispatchInvocationAsync{suffix}` and `__DispatchJsonArgsAsync{suffix}`; the reader is one shared
  `__ReadJsonArgsAsync`. Order of side effects is unchanged.
- `service-resolver-emitter.cs` (pre-existing bug, boy-scout): a handler parameter named
  `configuration` of type IConfiguration emitted `IConfiguration configuration = configuration;`
  (CS0136, CS0165). Found by the new generator-80 test.
- Benchmarks (commit 5bebefd8 and this commit): ConsoleAppFramework AOT app takes `-i`/`-b` aliases
  and `run-benchmark.sh` passes one argv to all; the in-process Nuru case uses the shared route and
  argv and is labelled `TimeWarp.Nuru (CreateBuilder)`; the BenchmarkDotNet suite gains a Warm job;
  CliFx benchmarks ported to CliFx 3; the removed `CsProjCoreToolchain.From` and the
  `AssemblyName` that stopped BenchmarkDotNet finding its project are gone (all three broke the
  suite before this task). New `benchmarks/startup-overhead` runfile, registered as
  `dev benchmark` `startup-first-call` and `startup-warm-call`.
- Refreshed cross-framework results: `benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md`.
  Nuru is 2nd of 10 cold (9.3 ms, ConsoleAppFramework 1.0 ms, System.CommandLine 18.5 ms) and 2nd
  warm (0.6 µs, System.CommandLine 4.9 µs).

### After numbers (`dev benchmark run <name> --base HEAD`, HEAD = 5bebefd8)

| Benchmark | Samples | Base median | Head median | Ratio | Noise band | Verdict |
| --------- | ------: | ----------: | ----------: | ----: | ---------: | ------- |
| startup-first-call | 30 | 20,134 us | 14,888 us | 0.7394 | 6.0% | win |
| startup-warm-call | 30 | 5,980 ns | 3,146 ns | 0.5260 | 9.4% | win |
| operation-dispatch | 20 | 5,073.5 ns | 2,753 ns | 0.5426 | 2.2% | win |
| aot-startup | 40 | 2.826 ms | 2.834 ms | 1.0027 | 7.6% | noise |

`--base master` cannot build `startup-*` (the runfile is new), so the base is the benchmark-only
commit. Other sessions were compiling on the machine; the noise bands reflect that.

### Full-builder regression (July to December 2025)

The 131.96 ms / 221 KB "TimeWarp.Nuru (Mediator)" case was `CreateBuilder(args)` plus
`services.AddMediator()` (martinothamar Mediator) with a runtime DI container and configuration
(`commands/nuru-mediator-command.cs` at 8cebb25a). Task 330 (b8473a1c, ea8f1122, January 2026)
removed it and the slim and empty builders; the generator now emits only what an app uses. The
current `CreateBuilder` cold call is 9.3 ms and 2.3 KB. Nothing is left to fix.

### Follow-ups (proposed, not filed from this walk)

`ganda kanban create` claims and opens a worktree, so these are listed here:

1. Allocation-free generated matching: per-call `HashSet`/`List` for option forms, consumed
   indexes and positionals; about 2.3 KB per warm call remains (from 3.4 KB).
2. Synchronous path when no handler awaits: the remaining warm cost is mostly async state
   machines (RunWithSignalsAsync, ObserveResultAsync, ExecuteRouteAsync, the lease).
3. Repair `runfiles/aot-benchmark.cs` (written against an older TimeWarp.Amuru API; CS0103
   `DotNet`) and refresh the AOT cross-framework table.
4. Track `dev benchmark` verdicts in CI.

### Gates

- `./bin/dev build` -> Build completed successfully.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` (tools/dev-cli changed) -> installed.
- `ganda runfile cache --clear`, then `dotnet build tests/ci-tests/run-ci-tests.cs` (compile only) -> succeeded.
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-80-startup-work-omission.cs` -> 4/4 passed (new).
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-52-configuration-detection.cs` -> 16/16 passed (edited).
- `dotnet run --file tests/timewarp-nuru-tests/generator/generator-69-endpoint-constructor-logger-warning.cs` -> 7/7 passed (edited).
- Covering files (production code under test: RunCliAsync, JSON emitters, configuration):
  - `tests/timewarp-nuru-tests/routing/routing-43-cli-failures.cs` -> 7/7 passed.
  - `tests/timewarp-nuru-tests/repl/repl-42-ctrl-c-cancellation.cs` -> 6/6 passed.
  - `tests/timewarp-nuru-tests/repl/repl-49-redirected-input.cs` -> 6/6 passed.
  - `tests/timewarp-nuru-tests/routing/routing-33-json-args.cs` -> 15/15 passed.
  - `tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs` -> 4/4 passed.
  - `tests/timewarp-nuru-tests/routing/routing-34-json-bind-invoke.cs` -> 10/10 passed.
  - `tests/timewarp-nuru-tests/routing/routing-36-problem-errors.cs` -> 8/8 passed.
  - `tests/timewarp-nuru-daemon-tests/daemon-02-forwarding.cs` -> 14/14 passed.
  - `tests/timewarp-nuru-daemon-tests/daemon-03-served-app.cs` -> 1/1 passed.
  - `tests/timewarp-nuru-tests/configuration/configuration-01-validate-on-start.cs` -> 5/5 passed.
  - `tests/timewarp-nuru-tests/configuration/configuration-02-cli-overrides.cs` -> 7/7 passed.
  - `tests/timewarp-nuru-tests/generator/generator-13-ioptions-parameter-injection.cs` -> 5/5 passed.
- `tests/ci-standalone/run-standalone-tests.cs` and `tests/ci-tests/Directory.Build.props` were
  edited only to list generator-80 as standalone; the runner is the CI standalone phase, not run here.
- `ganda repo audit` -> failed: `runfile-executable` (benchmarks/startup-overhead/startup-overhead.cs had no
  executable bit) and `vscode-window-icon` (warning, `.vscode/settings.json` missing). `ganda repo audit --fix`
  set the bit and created the git-ignored settings file; generator-80 got the same bit by hand. Rerun after
  the fix -> "Repository passes all audit checks."

### How to validate

Smoke:

```bash
dotnet run --file tools/dev-cli/dev.cs -- self-install
./bin/dev benchmark list
./bin/dev benchmark run startup-warm-call --base 5bebefd8 --samples 10
./bin/dev benchmark run startup-first-call --base 5bebefd8 --samples 10
dotnet run --file tests/timewarp-nuru-tests/generator/generator-80-startup-work-omission.cs
dotnet run --file tests/timewarp-nuru-tests/routing/routing-43-cli-failures.cs
```

Expect:

- `list` prints four benchmarks, including `startup-first-call [us]` and `startup-warm-call [ns]`.
- Both runs print a `dev-benchmark/v1` JSON object with `"verdict": "win"`: warm ratio near 0.5,
  first-call ratio near 0.75.
- generator-80 passes 4/4; routing-43 passes 7/7.
- In `artifacts/generated/bench-nuru/.../NuruGenerated.g.cs` (after building bench-nuru),
  `ExecuteRouteAsync` calls `EnsureServicesInitialized(app, null)`, has no `ConfigurationBuilder`,
  and calls `__DispatchInvocationAsync` and `__DispatchJsonArgsAsync`.

