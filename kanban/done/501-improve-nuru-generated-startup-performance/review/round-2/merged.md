# Round 2 — merged findings
**Date:** 2026-11-21
**Sources:** general

Re-review of commit `89a59f10` (round-1 doc fix). Prior IDs carried forward. No new IDs.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: changelog.md:60; source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.invoke-parse.cs:84; source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.method-body.cs:155; benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md:69
- Description: Those four texts said a plain argv run does not load System.Text.Json. `EmitPrepare` still emits `JsonDocument? __jsonDoc` in `ExecuteRouteAsync` and calls the reader and the JSON dispatch method.
- Suggestion: Say the plain run does not JIT-compile the reader or the switch, and that `ExecuteRouteAsync` still names `JsonDocument`.
- Source: general
- Disposition notes: Fixed in `89a59f10`. Round 2 re-read the four texts and `json-args-emitter.cs` line 181. The claim matches the emitter. The "Where the first call went" Results sentence still describes the old inline body, as round 1 allowed.

### M2 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:231
- Description: `EmitDispatch` had two `<summary>` elements. The first described the old inline path.
- Suggestion: Keep the summary that says the body runs in its own method and always returns.
- Source: general
- Disposition notes: Fixed in `89a59f10`. Round 2 confirmed one summary remains, at lines 231–235.

### M3 — Severity: nit — Status: fixed
- File: benchmarks/timewarp-nuru-benchmarks/results/2026-11-21-analysis.md:3
- Description: The analysis said every framework parses `--str "hello world" -i 13 -b`. PowerArgs still uses `/str:hello world`, `/int:13`, and `/bool`.
- Suggestion: Name PowerArgs and the argv it still uses.
- Source: general
- Disposition notes: Fixed in `89a59f10`. Round 2 checked `cli-framework-benchmark.cs` line 82. The header matches those three arguments.

## Duplicates / conflicts

- Single reviewer. No new findings. No overlaps.
