# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch `task/523-nuru-verification-skill-launch-and-drive-the-real` vs merge-base `8c9a0562`. `skills/tw-nuru-verify/` (SKILL.md, `scripts/nuru-consumer-check.sh`, probes) and the attached `evidence/` for 4.0.0-beta.5, 3.0.0-beta.76, and the state#647 scratch cache. Compared with `## Goal (implementer restatement)` and the original task. No misread.

## Summary

The skill is hand-authored, lives next to `tw-nuru` and `tw-cli-search`, and installs the shipped package and `nuru` tool into a private cache with a `<clear/>` nuget.config. Doctor, Cleanup, and the invocation decision (tw-release, not the task walk) are justified in Notes. The attached evidence does show the three named failures: ConfigureHelp on 3.0.0-beta.76, stale `CreateBuilder(args)` docs on that same tag, and a poisoned global-cache hash for `timewarp.nuru` 4.0.0-beta.5. The current-release run is a real consumer pass/fail, not a stubbed green.

The stale-docs gate does not catch the pre-3.0 two-argument `.Map(pattern, handler)` that living docs still teach. `DiscoverEndpoints()` is advertised and never driven. A missing `script` or `nc` is recorded as SKIP and can still exit 0.

## Issues

### Issue 1 — Severity: bug
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:466
- Description: The docs step is the nuru#296 catcher, and it misses the pre-3.0 builder shape those docs still teach. Two gaps compound. Extraction keeps only fences whose text contains `CreateBuilder` (line 466), so the two-argument samples in `documentation/user/features/output-handling.md` (line 32), later `documentation/user/use-cases.md` sections (line 193), and `documentation/user/features/auto-help.md` (line 363) are never compiled. The one fence that does contain `CreateBuilder`, `documentation/user/use-cases.md` line 149 (`.Map("deploy prod {*args}", async (string[] args) => …)`), was compiled on the 4.0.0-beta.5 run and recorded as `fragment` with `CS1501: No overload for method 'Map' takes 2 arguments` (`evidence/4.0.0-beta.5/snippets.tsv`). `nuru_errors` (lines 256–270) only blames a `CS*` error when the message contains `nuru` or `CreateBuilder`. `CS1501` on `Map` does neither, so the else branch at lines 522–524 labels it a fragment and the run does not fail. That is the opposite of the comment above the classifier, which says a fragment only lacks names the snippet never defined. A bare `builder.Map(pattern, handler)` fence would also stay a fragment if it were extracted today, because an undefined `builder` is `CS0103` and the overload error is then dropped.
- Suggestion: Treat overload and missing-member errors (`CS1501`, `CS1061`, `CS0117`) as broken even when the message does not name a Nuru type. Compile the two-argument `.Map` samples in a context where the receiver exists (wrap a `builder.` fragment in `NuruApp.CreateBuilder()`), and fail the docs step on `use-cases.md` and `output-handling.md` for 4.0.0-beta.5. Re-quote `snippets.tsv` in the task evidence.
- Status: open

### Issue 2 — Severity: bug
- File: skills/tw-nuru-verify/SKILL.md:121
- Description: The task requires every public builder API the docs and README advertise to be called and shown to do something real. `DiscoverEndpoints()` is on that list (builder-api, README Endpoint DSL, getting-started). The coverage table says it has no probe because "README snippet compiles". On the 4.0.0-beta.5 run the README sample (`readme.md` line 54, fence at line 33) is a fragment, `CS8803` top-level statements after the endpoint class, not `compiles`. No probe calls `DiscoverEndpoints()`. `probes/endpoint-dsl.cs` registers the route with `Map<T>()` instead. Other doc fences that mention `DiscoverEndpoints()` and did compile only prove the generator accepts the name. A method that returns the builder and discovers nothing would still pass.
- Suggestion: Add a probe that puts a `[NuruRoute]` handler in the same compilation, calls `DiscoverEndpoints()` rather than `Map<T>()`, and asserts the route's output in `cases.tsv`. Correct the coverage-table reason. Do not cite the README fence as proof until that probe passes.
- Status: open

### Issue 3 — Severity: bug
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:372
- Description: `pty` without `script(1)` and `otlp` without `nc` record SKIP (lines 372 and 388). `record` sets the run to failed only for `FAIL` (line 64), and the process exits with that flag (line 576). SKILL.md line 40 says exit 0 means every check passed. Notes say a beta whose check fails is not promoted. A host missing `script` or `nc` can exit 0 without driving `AddRepl` or `UseTelemetry`. The 4.0.0-beta.5 evidence happened to skip nothing; the gate still has the hole.
- Suggestion: When a selected probe mode cannot run, record FAIL or refuse in doctor (exit 2). Do not exit 0.
- Status: open

### Issue 4 — Severity: nit
- File: skills/tw-nuru-verify/SKILL.md:1
- Description: The task requires the skill to follow skill conventions. `tw-skill-authoring` requires one `why:` clause on an agent-authored `tw-*` skill body or frontmatter. `skills/tw-nuru-verify/SKILL.md` has none. The prompt for the clause is task 523 and Steven's approval of the consumer-use check on 2026-10-23.
- Suggestion: Add one `why:` clause naming that prompt.
- Status: open
