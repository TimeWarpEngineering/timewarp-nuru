# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:466
- Description: The docs step misses the pre-3.0 two-argument `.Map(pattern, handler)` that living docs still teach. Fences without the text `CreateBuilder` are never compiled (`output-handling.md`, later `use-cases.md` sections, `auto-help.md`). The fence that does contain it, `documentation/user/use-cases.md` line 149, was classified `fragment` on the 4.0.0-beta.5 run (`CS1501: No overload for method 'Map' takes 2 arguments`) because `nuru_errors` only blames a `CS*` error when the message contains `nuru` or `CreateBuilder`. The run does not fail. That is the nuru#296 class.
- Suggestion: Treat `CS1501`, `CS1061`, and `CS0117` as broken even when the message does not name a Nuru type. Compile two-argument `.Map` samples in a context where the receiver exists, and fail the docs step on those samples for 4.0.0-beta.5. Re-quote `snippets.tsv` in the task evidence.
- Source: general
- Disposition notes: Fixed. `nuru_errors` treats `CS1501` as broken always, and `CS1061`/`CS0117` as broken unless the receiver is a host extension point (`IServiceCollection`, `ILoggingBuilder`, …). Without that exception, 16 fences whose only error is a missing `using Microsoft.Extensions.DependencyInjection` or a third-party extension would also fail. Fences with `builder.Map("…", …)` and no `CreateBuilder` compile after `var builder = NuruApp.CreateBuilder();`, with usings hoisted and type declarations moved last (`context` = `builder` in `snippets.tsv`). On 4.0.0-beta.5 the docs step fails on use-cases.md#L149/#L193/#L241 and output-handling.md ×8 (`CS1501 Map takes 2 arguments`). `evidence/4.0.0-beta.5/snippets.tsv` is re-quoted.

### M2 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/SKILL.md:121
- Description: `DiscoverEndpoints()` is an advertised builder API with no runtime probe. The coverage table says the README snippet compiles. On the 4.0.0-beta.5 run that fence (`readme.md` line 33) is a `CS8803` fragment. `probes/endpoint-dsl.cs` uses `Map<T>()` instead. A no-op `DiscoverEndpoints()` that the generator accepts would pass.
- Suggestion: Add a probe that calls `DiscoverEndpoints()` on a `[NuruRoute]` in the same compilation and asserts the route output. Correct the coverage-table reason.
- Source: general
- Disposition notes: Fixed. New `probes/discover-endpoints.cs` has two `[NuruRoute]` classes, `DiscoverEndpoints()` and no `Map<T>()`. Two cases assert both routes run. Both PASS on 4.0.0-beta.5. The coverage table row now names the probe.

### M3 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:372
- Description: A missing `script(1)` or `nc` records SKIP and the process can still exit 0, so `AddRepl` and `UseTelemetry` are not driven. SKILL.md says exit 0 means every check passed, and Notes treat that exit as the promotion gate.
- Suggestion: Record FAIL, or refuse in doctor, when a selected probe mode cannot run. Do not exit 0.
- Source: general
- Disposition notes: Fixed. Doctor exits 2 when the pty cases are selected and `script` is missing, when the otlp case is selected and `nc` is missing, or when `cli` is selected without `probes`. The SKIP paths are removed, and `record` fails the run on any non-PASS. SKILL.md documents exit 0 as "every selected check ran and passed". Verified with `script`/`nc` hidden from PATH (exit 2).

### M4 — Severity: nit — Status: fixed
- File: skills/tw-nuru-verify/SKILL.md:1
- Description: Agent-authored `skills/tw-nuru-verify/SKILL.md` has no `why:` clause. `tw-skill-authoring` requires one on a `tw-*` skill edit. The prompt is task 523 and Steven's approval of the consumer-use check on 2026-10-23.
- Suggestion: Add one `why:` clause naming that prompt.
- Source: general
- Disposition notes: Fixed. SKILL.md carries `(why: task 523, Steven approved the consumer-use check 2026-10-23)`.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
