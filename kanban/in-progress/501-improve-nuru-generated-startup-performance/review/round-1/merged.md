# Round 1 — merged findings
**Date:** 2026-11-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:181
- Description: Changelog line 60, `json-args-emitter.invoke-parse.cs` lines 84–86, `interceptor-emitter.method-body.cs` lines 155–157, and `benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md` line 69 say a plain argv run does not load System.Text.Json. `EmitPrepare` still declares `JsonDocument? __jsonDoc` in `ExecuteRouteAsync`, and that method calls `__ReadJsonArgsAsync` (returns `JsonDocument`) and `__DispatchJsonArgsAsync` (takes `JsonDocument`). The JIT resolves those tokens when it compiles `ExecuteRouteAsync`, which loads the assembly. Moving the parse and the switch out does avoid compiling those method bodies until `--json-args` or `InvokeAsync` runs. The recorded first-call win can stand on the console subscription, the blank configuration root, and that JIT split. The load sentence cannot.
- Suggestion: Correct the changelog, the two emitter remarks, and the analysis note. Say the plain run does not JIT-compile the reader or the switch. State that `ExecuteRouteAsync` still names `JsonDocument`, so the assembly can still load. The Results sentence under "Where the first call went" describes the old inline body and can stay. A type that is not `JsonDocument` on the hot method would be a separate change; this finding is the false claim.
- Source: general
- Disposition notes: Fixed: changelog, `EmitReadMethod` remarks, `EmitDeferredDispatchMethod` remarks and the analysis note now say a plain run does not JIT-compile the reader or the switch, and that `ExecuteRouteAsync` still names `JsonDocument`, so System.Text.Json can still load.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:231
- Description: `EmitDispatch` has two `<summary>` elements. The first still says the selection returns from the generated method on this path, which was true when the body was inline in `ExecuteRouteAsync`. The second summary describes the extracted method.
- Suggestion: Keep one summary, the one that says the body runs in its own method and always returns.
- Source: general
- Disposition notes: Fixed: the stale first `<summary>` on `EmitDispatch` is removed.

### M3 — Severity: nit — Status: fixed
- File: benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md:3
- Description: The new analysis says every framework parses `--str "hello world" -i 13 -b`. `ExecuteWithPowerArgs` in `cli-framework-benchmark.cs` still passes `/str:hello world`, `/int:13`, and `/bool`. That difference was already in the benchmark. The new sentence states it away. ConsoleAppFramework's in-process command uses the shared argv. The AOT doc comments `-i` and `-b` (no comma) are aliases: ConsoleAppFramework's `ParseParameterDescription` treats a description that starts with `-` as the alias list.
- Suggestion: Say PowerArgs is the exception and name the argv it still uses.
- Source: general
- Disposition notes: Fixed: the analysis note says PowerArgs is the exception and still parses `/str:hello world /int:13 /bool`.

## Duplicates / conflicts

- Single reviewer. No overlaps.
