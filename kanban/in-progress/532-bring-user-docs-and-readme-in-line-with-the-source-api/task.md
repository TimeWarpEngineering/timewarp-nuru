# Bring user docs and README in line with the source API

## Description

Bring user docs and README in line with the source API. The source (code plus its `#region Purpose` / `#region Design` blocks) is the source of truth; docs follow it. A doc snippet that fails to build is a docs defect unless the source's public API says the behavior should exist (in which case it is a code task).

Evidence: the task 523 consumer check built every documentation snippet against TimeWarp.Nuru 4.0.0-beta.5 installed from nuget.org into a clean cache. 26 of 138 snippets are broken (evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

Known defects:
- README still teaches `NuruApp.CreateBuilder(args)` and `EnableStaticCompletion()` (`readme.md` ~L176, ~L246)
- Two-argument `builder.Map(pattern, handler)` (CS1501) in `output-handling.md` (~L32, L69, L155, L181, L209, L243, L271, L347) and `use-cases.md` (~L149, L193, L241)
- `best-practices.md` ~L28 uses nonexistent `WithGroup`
- `routing.md` ~L130 shows `Map("")` beside `{*args}` (NURU_R003 unreachable)
- `supported-types.md` ~L28/L49/L109 handler parameter names differ from route segments (NURU_H005)
- `telemetry.md` TelemetryBehavior sample uses `new ActivitySource("MyApp.Commands")`, but the generated tracer subscribes only to `TimeWarp.Nuru` and `TimeWarp.Nuru.Behavior`, so the sample exports no spans. Verify against the telemetry source intent, then fix the doc (or file a code task if the source says custom sources should be subscribed)

Logging docs depend on the outcome of the UseLogging/UseConsoleLogging task; MCP docs depend on the WithNuruTools task.

## Goal (implementer restatement)

Make every C# snippet in `readme.md` and `documentation/user/**` that builds an app compile against the latest shipped TimeWarp.Nuru package, by correcting the docs to match the source public API (the source plus its Purpose/Design regions is the authority). Where the source says a documented behavior should exist and the package does not deliver it, file a code task instead of editing the doc.

Acceptance:
- Each listed defect (README `CreateBuilder(args)`/`EnableStaticCompletion`, two-argument `Map` in `output-handling.md`/`use-cases.md`, `WithGroup` in `best-practices.md`, unreachable `Map("")` in `routing.md`, parameter-name mismatches in `supported-types.md`, the `telemetry.md` ActivitySource sample) is checked against source and fixed in the doc, or turned into a code task.
- `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --steps docs --docs-ref HEAD` reports 0 broken snippets outside the logging (task 527 area) and MCP/`WithNuruTools` (task 531) areas; the count is recorded in Results.
- Any spawned code tasks are listed in Notes.
- Build, test and audit gates pass.

## Checklist

- [x] For each defect, confirm against the source's public API that the doc is wrong (not the code); if the API says the behavior should exist, file a code task instead of editing the doc
- [x] Fix README snippets
- [x] Fix `output-handling.md` and `use-cases.md` two-argument `Map` calls
- [x] Fix `best-practices.md` `WithGroup`
- [x] Fix `routing.md` unreachable `Map("")`
- [x] Fix `supported-types.md` parameter names
- [x] Fix `telemetry.md` ActivitySource sample
- [x] Re-run `tw-nuru-verify --steps docs --docs-ref HEAD` and record the broken-snippet count (target 0 outside the two dependent areas)
- [x] Record any code tasks spawned in Notes

## Notes

- Related: the UseLogging/UseConsoleLogging NURU_S999 task, the WithNuruTools ASP.NET chain task, 523.
- Code tasks filed from this task (2026-11-14):
  - **585** Anonymous-type handler return breaks the build in the generated `NuruUserTypesJsonContext`. The docs taught `() => new { ... }` returns; the generated JSON context writes the anonymous type name into `typeof(...)` and the build fails. Docs now return named records and say anonymous types do not build.
  - **586** `{name:string}` (a built-in constraint in `built-in-type-names.cs`) generates code that does not compile (`CS0103` on the handler variable). The source says the constraint is valid, so `supported-types.md` keeps `{message:string}`; the snippet compiles once 586 lands. The verify script classifies it as a fragment because the error is in generated code.
- Source checks behind each doc edit:
  - `NuruApp.CreateBuilder` has no `args` overload; completion is `EnableCompletion` (`completion/nuru-app-builder-extensions.cs`).
  - Groups are `WithGroupPrefix(...)` ... `.Done()` (`nuru-app-builder.routes.cs`, `group-builder.cs`); there is no `WithGroup`.
  - `overlap-validator.cs` treats catch-all as optional, so a top-level `{*args}` has the same required signature as `""` and wins on specificity; NURU_R003 is the intended answer. `routing.md` now handles empty input inside the catch-all.
  - A handler's returned `int` is printed, not the exit code (`skills/tw-nuru/SKILL.md` "Return Values vs Exit Codes", confirmed by running a scratch app: `return 1` printed `1`, exit 0). Doc snippets that returned exit codes now set `Environment.ExitCode` (output-handling, use-cases, terminal-abstractions, README).
  - Telemetry: since task 561, `UseTelemetry(o => o.ActivitySources.Add("..."))` subscribes extra sources (`nuru-telemetry-options.cs`). The custom-behavior sample now registers `MyApp.Commands` that way.
- Two snippets that overlapped on optional flags (`delete database {name} --confirm` / `--dry-run`, `build {project}` / `build {project} --debug --quiet`) were merged into one route each.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-11-14 implementer (claude): merged origin/master (535 land), fixed docs, filed 585 and 586, merged origin/master again (585/586 lands), ran gates.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-14T01:26:41Z

## Results

Docs changed: `readme.md`, `documentation/user/features/output-handling.md`, `routing.md`, `telemetry.md`, `terminal-abstractions.md`, `documentation/user/guides/best-practices.md`, `documentation/user/reference/supported-types.md`, `documentation/user/use-cases.md`. No product code changed.

Consumer docs check against TimeWarp.Nuru 4.0.0-beta.13 (nuget.org), `--docs-ref HEAD`:

| Run | Broken | Compile | Fragments | Total |
|-----|--------|---------|-----------|-------|
| Before (master + claim) | 19 | 42 | 77 | 138 |
| After | 1 | 45 | 82 | 128 |

The one remaining broken snippet is `documentation/user/features/mcp-hosting.md:50` (`NURU_S999: Unrecognized DSL method: AddMcpServer`), the MCP/WithNuruTools area that task 531 owns. Logging docs have no broken snippets. The total drops by 10 because the converted `builder.Map(...)` blocks no longer use the two-argument form, so the script no longer wraps them in a `CreateBuilder()` program.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | pass (0 warnings, 0 errors) |
| Test | `./bin/dev test` | pass (multi-mode 2213 total, 2207 passed, 6 skipped; standalone 47 files, 0 failed) |
| Audit | `ganda repo audit` | first run: pass with 1 advisory (`vscode-window-icon`, `.vscode/settings.json` missing); `ganda repo audit --fix` wrote the git-ignored `.vscode/settings.json`; rerun: "Repository passes all audit checks." |

### How to validate

Smoke:

```bash
skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --steps docs --docs-ref HEAD
```

Expect: `doc snippets (HEAD) use only APIs the package has (1 broken, ...)`, and the only `DOC-DEFECT ... snippet` line is `documentation/user/features/mcp-hosting.md:50` (task 531). No `CS1501`, `WithGroup`, `NURU_R003` or `NURU_H005` rows.

