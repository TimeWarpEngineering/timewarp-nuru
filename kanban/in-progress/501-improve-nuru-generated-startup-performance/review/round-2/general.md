# Round 2 — general
**Date:** 2026-11-21
**Scope reviewed:** commit `89a59f10` against its parent (the round-1 fix). Product files only: `changelog.md`, `benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md`, `interceptor-emitter.method-body.cs` remarks, `json-args-emitter.cs`, `json-args-emitter.invoke-parse.cs`. The whole branch was round 1's scope.

## Summary

The fix restates three documentation claims and removes one stale summary. It does not change generated behavior. M1 holds: the changelog, both emitter remarks, and the analysis note now say a plain argv run does not JIT-compile the reader or the switch, and that `ExecuteRouteAsync` still names `JsonDocument` (`json-args-emitter.cs` line 181 still emits `JsonDocument? __jsonDoc`, and the method still calls `__ReadJsonArgsAsync` and `__DispatchJsonArgsAsync`). M2 holds: `EmitDispatch` has one summary, the extracted-method summary. M3 holds: the analysis header names PowerArgs and the argv in `cli-framework-benchmark.cs` line 82 (`/str:hello world`, `/int:13`, `/bool`). The fix-pass record under `## Results` (build, generator-80, routing-33, routing-34, audit) covers a comment-only change. No new findings on this delta.

`tests/timewarp-nuru-tests/generator/generator-80-startup-work-omission.cs` still says a plain run does not load System.Text.Json in its Purpose region. That file is unchanged since round 1 and was outside the four places M1 named, so this round leaves it.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: changelog.md:60; json-args-emitter.invoke-parse.cs:84; interceptor-emitter.method-body.cs:155; benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md:69
- Description: Each of those four texts now says the split avoids JIT of the reader and the switch, and that `ExecuteRouteAsync` still names `JsonDocument`, so System.Text.Json can still load. The Results sentence under "Where the first call went" still describes the old inline body, which round 1 allowed.
- Status: fixed

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:231
- Description: The summary that said selection returns from the generated method on this path is gone. The remaining summary says the body runs in its own method and always returns.
- Status: fixed

### M3 — Severity: nit — Status: fixed
- File: benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md:3
- Description: The header says every framework except PowerArgs parses `--str "hello world" -i 13 -b`, and that PowerArgs still parses `/str:hello world /int:13 /bool`. That matches `ExecuteWithPowerArgs`.
- Status: fixed
