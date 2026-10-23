# Round 2 — general
**Date:** 2026-10-23
**Scope reviewed:** delta `1e767fd6..9b4a097a` (round-1 fix). `skills/tw-nuru-verify/` (SKILL.md, `scripts/nuru-consumer-check.sh`, `probes/discover-endpoints.cs`, `probes/cases.tsv`) and the refreshed `evidence/` for 4.0.0-beta.5 and 3.0.0-beta.76. Prior findings M1–M4 re-checked on that delta. Compared with `## Goal (implementer restatement)` and the original task. No misread.

## Summary

The fix closes the four round-1 findings. Every two-argument `builder.Map("…", …)` fence in `readme.md` and `documentation/user/**` at `v4.0.0-beta.5` is compiled after `NuruApp.CreateBuilder()`, and `CS1501` fails the docs step. `DiscoverEndpoints()` is driven by a probe whose routes run only because the generator honors that call. A missing `script` or `nc` is a doctor refusal, and any result other than PASS fails the run. SKILL.md carries the `why:` clause. No new defect on the delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:485
- Description: Re-checked. The wrap selects a C# fence with no `CreateBuilder` whose body matches `builder.Map("…",`. On `v4.0.0-beta.5` that is exactly the ten fences in `output-handling.md` and `use-cases.md` (lines 193 and 241). `use-cases.md` line 149 already contains `CreateBuilder` and is compiled as a program. `nuru_errors` treats every `CS1501` as broken. `evidence/4.0.0-beta.5/snippets.tsv` has ten `builder` / `broken` rows and the line-149 program row, all `CS1501: No overload for method 'Map' takes 2 arguments`. The docs rollup is 26 broken, 38 compile, 74 fragments, 138 total, and the summary is 38 passed / 29 failed. The host-extension carve-out for `CS1061` / `CS0117` does not hide these rows: the only `CS1061` broken row on this release is `NuruAppBuilder.WithGroup`.
- Suggestion: none remaining.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/probes/discover-endpoints.cs:7
- Description: Re-checked. The probe calls `DiscoverEndpoints()`, registers two `[NuruRoute]` classes, and does not call `Map<T>()`. `FilterEndpointsForApp` includes those routes only when the interpreter saw `DiscoverEndpoints()` (`ir-app-builder.cs` sets `DiscoverEndpointsEnabled`). A method the generator accepts and ignores would leave both cases without `discovered add: 5` and `discovered subtract: 2`. The 4.0.0-beta.5 summary records both cases PASS. The coverage table names the probe.
- Suggestion: none remaining.
- Status: fixed

### M3 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:123
- Description: Re-checked. Doctor exits 2 when probes are selected and `cases.tsv` has a `pty` row without `script`, or an `otlp` row without `nc`, and when `cli` is selected without `probes`. The in-loop SKIP branches are gone. `record` sets `FAILED` on any status other than PASS. The refreshed summaries show 0 skipped. SKILL.md states that exit 0 means every selected check ran and passed.
- Suggestion: none remaining.
- Status: fixed

### M4 — Severity: nit — Status: fixed
- File: skills/tw-nuru-verify/SKILL.md:11
- Description: Re-checked. The body has `(why: task 523, Steven approved the consumer-use check 2026-10-23)`, which is the `why:` clause `tw-skill-authoring` accepts on a `tw-*` skill edit.
- Suggestion: none remaining.
- Status: fixed

## Issues

None.
