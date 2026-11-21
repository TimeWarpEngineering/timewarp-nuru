# Round 1 — general
**Date:** 2026-11-21
**Scope reviewed:** branch `task/501-improve-nuru-generated-startup-performance` vs merge-base `740a3661` (source, tests, benchmarks, changelog, task restatement)

## Summary

The branch makes a plain CLI run cheaper: Posix signals are registered once per process and the console terminal no longer subscribes to `Console.CancelKeyPress`, a blank configuration root is emitted only when generated code reads `configuration` or a container can hand `IConfiguration` out, and the `--json-args` reader, JSON route selection, and `InvokeAsync` switch moved into their own generated methods. The recorded `dev benchmark` verdicts are wins on `startup-first-call` (0.74), `startup-warm-call` (0.53), and `operation-dispatch` (0.54), and noise on `aot-startup`. Signal registration, the configuration scan, and the dispatch split are consistent with the tests that were recorded. The peer-speed goal is a misread, recorded under `## Learnings`, not a finding. What is wrong in the diff is the claim that a plain argv run does not load System.Text.Json, plus two documentation nits.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:181
- Description: Changelog line 60, `json-args-emitter.invoke-parse.cs` lines 84–86, `interceptor-emitter.method-body.cs` lines 155–157, and `benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md` line 69 say a plain argv run does not load System.Text.Json. `EmitPrepare` still declares `JsonDocument? __jsonDoc` in `ExecuteRouteAsync`, and that method calls `__ReadJsonArgsAsync` (returns `JsonDocument`) and `__DispatchJsonArgsAsync` (takes `JsonDocument`). The JIT resolves those tokens when it compiles `ExecuteRouteAsync`, which loads the assembly. Moving the parse and the switch out does avoid compiling those method bodies until `--json-args` or `InvokeAsync` runs. The recorded first-call win can stand on the console subscription, the blank configuration root, and that JIT split. The load sentence cannot.
- Suggestion: Correct the changelog, the two emitter remarks, and the analysis note. Say the plain run does not JIT-compile the reader or the switch. State that `ExecuteRouteAsync` still names `JsonDocument`, so the assembly can still load. The Results sentence under "Where the first call went" describes the old inline body and can stay. A type that is not `JsonDocument` on the hot method would be a separate change; this finding is the false claim.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:231
- Description: `EmitDispatch` has two `<summary>` elements. The first still says the selection returns from the generated method on this path, which was true when the body was inline in `ExecuteRouteAsync`. The second summary describes the extracted method.
- Suggestion: Keep one summary, the one that says the body runs in its own method and always returns.
- Status: open

### Issue 3 — Severity: nit
- File: benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md:3
- Description: The new analysis says every framework parses `--str "hello world" -i 13 -b`. `ExecuteWithPowerArgs` in `cli-framework-benchmark.cs` still passes `/str:hello world`, `/int:13`, and `/bool`. That difference was already in the benchmark. The new sentence states it away. ConsoleAppFramework's in-process command uses the shared argv (`-i, --int` and `-b, --bool`). The AOT ConsoleAppFramework doc comments are `-i` and `-b` with no comma; ConsoleAppFramework's `ParseParameterDescription` still treats a description that starts with `-` as the alias, so that script change matches the generator.
- Suggestion: Say PowerArgs is the exception and name the argv it still uses.
- Status: open
