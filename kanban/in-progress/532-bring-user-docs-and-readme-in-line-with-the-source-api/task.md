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

- [ ] For each defect, confirm against the source's public API that the doc is wrong (not the code); if the API says the behavior should exist, file a code task instead of editing the doc
- [ ] Fix README snippets
- [ ] Fix `output-handling.md` and `use-cases.md` two-argument `Map` calls
- [ ] Fix `best-practices.md` `WithGroup`
- [ ] Fix `routing.md` unreachable `Map("")`
- [ ] Fix `supported-types.md` parameter names
- [ ] Fix `telemetry.md` ActivitySource sample
- [ ] Re-run `tw-nuru-verify --steps docs --docs-ref HEAD` and record the broken-snippet count (target 0 outside the two dependent areas)
- [ ] Record any code tasks spawned in Notes

## Notes

- Related: the UseLogging/UseConsoleLogging NURU_S999 task, the WithNuruTools ASP.NET chain task, 523.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
