# Generator rejects public UseLogging() and UseConsoleLogging() with NURU_S999

## Description

Public logging APIs exist but the source generator cannot interpret them, so any consumer build that calls them fails with `NURU_S999 Unrecognized DSL method`. Found by the task 523 consumer check (TimeWarp.Nuru 4.0.0-beta.5 installed from nuget.org into a clean cache; evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

Source of truth is the source, not the docs. The public API says the behavior should exist: `NuruAppBuilder.UseLogging(ILoggerFactory)` in `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs` (~line 106), plus the `UseConsoleLogging` overloads and `ConfigureLogging` in `source/timewarp-nuru/logging/nuru-logging-extensions.cs`. The DSL interpreter dispatch (`source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`, ~line 677) has no case for them. Broken since the semantic interpreter landed (#278, 2025-12-27). No test or sample calls any of them, which is how it stayed hidden.

Docs that mention logging follow whatever this task decides; they are not evidence of intended behavior.

## Checklist

- [ ] Read the `#region Purpose` / `#region Design` blocks (and code) of the logging files and the interpreter to establish intent
- [ ] Decide: support these APIs in the interpreter/IR/emitter, or remove them from the public API. Record the decision and reasoning in Notes; ask the human if intent is unclear
- [ ] Implement the decision (interpreter case + IR + emitter, or API removal with changelog note)
- [ ] Add or update `#region Purpose` / `#region Design` in every touched file
- [ ] Regression test that calls each of `UseLogging`, each `UseConsoleLogging` overload and `ConfigureLogging` (or confirms removal) and runs; use `.Map<T>()`, not `DiscoverEndpoints()`
- [ ] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [ ] Consumer-style verification with the `tw-nuru-verify` `use-logging` probe (installed package, clean cache)
- [ ] Update logging docs to match the decision (task 5 covers the broader docs pass)

## Notes

- Evidence: task 523 consumer check against 4.0.0-beta.5.
- Related: 523 (verification skill), the docs alignment task.
- No trailing whitespace (RCS1037). New analyzer diagnostics go in `AnalyzerReleases.Unshipped.md`, never Shipped.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
