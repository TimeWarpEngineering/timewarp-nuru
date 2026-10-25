# Generator rejects public UseLogging() and UseConsoleLogging() with NURU_S999

## Description

Public logging APIs exist but the source generator cannot interpret them, so any consumer build that calls them fails with `NURU_S999 Unrecognized DSL method`. Found by the task 523 consumer check (TimeWarp.Nuru 4.0.0-beta.5 installed from nuget.org into a clean cache; evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

Source of truth is the source, not the docs. The public API says the behavior should exist: `NuruAppBuilder.UseLogging(ILoggerFactory)` in `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs` (~line 106), plus the `UseConsoleLogging` overloads and `ConfigureLogging` in `source/timewarp-nuru/logging/nuru-logging-extensions.cs`. The DSL interpreter dispatch (`source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`, ~line 677) has no case for them. Broken since the semantic interpreter landed (#278, 2025-12-27). No test or sample calls any of them, which is how it stayed hidden.

Docs that mention logging follow whatever this task decides; they are not evidence of intended behavior.

## Checklist

- [x] Read the `#region Purpose` / `#region Design` blocks (and code) of the logging files and the interpreter to establish intent
- [x] Decide: support these APIs in the interpreter/IR/emitter, or remove them from the public API. Record the decision and reasoning in Notes; ask the human if intent is unclear
- [ ] Implement the decision: remove `UseLogging(ILoggerFactory)`, the `UseConsoleLogging` overloads, `UseDebugLogging` and `ConfigureLogging` (all of `source/timewarp-nuru/logging/nuru-logging-extensions.cs`, plus `NuruAppBuilder.UseLogging` in `nuru-app-builder.configuration.cs`), and anything that exists only to serve them (for example a builder `LoggerFactory` field nothing generated reads, or a package reference only they use). Keep `ConfigureServices(s => s.AddLogging(...))`, `ILogger<T>` injection, NURU_H007, `NuruApp.LoggerFactory` disposal and `UseTelemetry` logging untouched. Changelog entry marked breaking.
- [ ] Add or update `#region Purpose` / `#region Design` in every touched file
- [ ] Test that the supported path works end to end: `ConfigureServices(s => s.AddLogging(b => b.AddConsole().SetMinimumLevel(...)))` with an injected `ILogger<T>` logs; use `.Map<T>()`, not `DiscoverEndpoints()` (reuse an existing test if one already covers it)
- [ ] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [ ] Update `skills/tw-nuru-verify`: replace the `use-logging` and `use-console-logging` probes and cases with one `add-logging` probe for `ConfigureServices(s => s.AddLogging(...))` plus `ILogger<T>`; drop the removed APIs from SKILL.md's coverage and uncovered tables; run the consumer check against a locally packed candidate (non-release version, private cache) and attach evidence
- [ ] Update logging docs (builder-api.md, logging.md, output-handling.md, README if present) to teach only `AddLogging` in `ConfigureServices`; task 532 covers the broader docs pass

## Notes

### Decision (Steven, 2026-10-25): remove them

Nuru supports logging through one path: `ConfigureServices(s => s.AddLogging(...))`. The DSL interpreter reads it (`ServiceExtractor.ExtractLoggingConfiguration`, `dsl-interpreter.cs` ~1305), `ILogger<T>` is injectable, NURU_H007 warns when `ILogger<T>` is injected with no logging configured, the runtime disposes `NuruApp.LoggerFactory`, and samples teach it (`samples/fluent/09-logging/fluent-logging-console.cs`, `samples/endpoints/09-logging/console/console.cs`, the Serilog sample).

`UseLogging`, `UseConsoleLogging`, `UseDebugLogging` and `ConfigureLogging` came from the 2025-08 `TimeWarp.Nuru.Logging` package, before the source generator. The semantic interpreter (#278, 2025-12-27) never supported them, so every consumer build that calls them has failed with NURU_S999 since then; no test or sample uses them; `UseLogging(ILoggerFactory)` only sets a builder property the generated code never reads. Removing them leaves one DI-based way to configure logging. It is breaking on paper but no working app can depend on them. Ship it in the same 4.0 beta as 518's attribute removal.

why: Steven 2026-10-25 "ok let's remove them"


- Evidence: task 523 consumer check against 4.0.0-beta.5.
- Related: 523 (verification skill), the docs alignment task.
- No trailing whitespace (RCS1037). New analyzer diagnostics go in `AnalyzerReleases.Unshipped.md`, never Shipped.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-10-25 cockpit: recorded the remove decision (Steven) and rewrote the checklist to match
