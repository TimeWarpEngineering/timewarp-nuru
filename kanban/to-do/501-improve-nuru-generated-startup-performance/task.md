# Improve Nuru generated startup performance

## Goal
Make Nuru measurably faster than its peers wherever framework work (not process startup) is what is measured. Aim for the full builder (with Mediator) to start in single-digit ms. AOT launch time will stay about 3 ms, because that is the process-startup floor.

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

## Related
- TimeWarpEngineering/timewarp-mediator: "Improve Mediator send performance" task 020: https://github.com/TimeWarpEngineering/timewarp-mediator/blob/master/kanban/to-do/020-improve-mediator-send-performance/task.md

## Session

- Created: 1508201 (2026-10-13)
