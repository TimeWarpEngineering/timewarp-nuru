# Generator rejects public UseLogging() and UseConsoleLogging() with NURU_S999

## Description

Public logging APIs exist but the source generator cannot interpret them, so any consumer build that calls them fails with `NURU_S999 Unrecognized DSL method`. Found by the task 523 consumer check (TimeWarp.Nuru 4.0.0-beta.5 installed from nuget.org into a clean cache; evidence in `kanban/done/523-*/evidence/4.0.0-beta.5/` on branch `task/523-nuru-verification-skill-launch-and-drive-the-real`).

Source of truth is the source, not the docs. The public API says the behavior should exist: `NuruAppBuilder.UseLogging(ILoggerFactory)` in `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs` (~line 106), plus the `UseConsoleLogging` overloads and `ConfigureLogging` in `source/timewarp-nuru/logging/nuru-logging-extensions.cs`. The DSL interpreter dispatch (`source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`, ~line 677) has no case for them. Broken since the semantic interpreter landed (#278, 2025-12-27). No test or sample calls any of them, which is how it stayed hidden.

Docs that mention logging follow whatever this task decides; they are not evidence of intended behavior.

## Goal (implementer restatement)

Nuru has public logging builder APIs (`UseLogging(ILoggerFactory)`, `UseConsoleLogging` x2,
`UseDebugLogging`, `ConfigureLogging`) that the source generator never understood, so any consumer
calling them fails with NURU_S999. Steven decided to remove them. Done means:

- Those five methods are gone from the public API, along with anything that only served them
  (the builder's private `LoggerFactory` field and the `Build()` line that copied it). The supported
  path, `ConfigureServices(s => s.AddLogging(...))` with injected `ILogger<T>`, NURU_H007,
  `NuruApp.LoggerFactory` disposal and `UseTelemetry` logging, is untouched.
- The changelog has a breaking entry. Touched files carry `#region Purpose` / `#region Design`.
- A test shows `AddLogging(b => b.AddConsole().SetMinimumLevel(...))` plus an injected `ILogger<T>`
  logs at the configured level, using `.Map<T>()`.
- CI tests are green after a runfile cache clear.
- `tw-nuru-verify` has one `add-logging` probe instead of the two removed ones, its SKILL.md tables
  no longer list the removed APIs, and the consumer check passes against a locally packed,
  non-release candidate with evidence attached.
- builder-api.md, logging.md, output-handling.md and the README teach only `AddLogging`.

## Checklist

- [x] Read the `#region Purpose` / `#region Design` blocks (and code) of the logging files and the interpreter to establish intent
- [x] Decide: support these APIs in the interpreter/IR/emitter, or remove them from the public API. Record the decision and reasoning in Notes; ask the human if intent is unclear
- [x] Implement the decision: remove `UseLogging(ILoggerFactory)`, the `UseConsoleLogging` overloads, `UseDebugLogging` and `ConfigureLogging` (all of `source/timewarp-nuru/logging/nuru-logging-extensions.cs`, plus `NuruAppBuilder.UseLogging` in `nuru-app-builder.configuration.cs`), and anything that exists only to serve them (for example a builder `LoggerFactory` field nothing generated reads, or a package reference only they use). Keep `ConfigureServices(s => s.AddLogging(...))`, `ILogger<T>` injection, NURU_H007, `NuruApp.LoggerFactory` disposal and `UseTelemetry` logging untouched. Changelog entry marked breaking.
- [x] Add or update `#region Purpose` / `#region Design` in every touched file
- [x] Test that the supported path works end to end: `ConfigureServices(s => s.AddLogging(b => b.AddConsole().SetMinimumLevel(...)))` with an injected `ILogger<T>` logs; use `.Map<T>()`, not `DiscoverEndpoints()` (reuse an existing test if one already covers it)
- [x] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [x] Update `skills/tw-nuru-verify`: replace the `use-logging` and `use-console-logging` probes and cases with one `add-logging` probe for `ConfigureServices(s => s.AddLogging(...))` plus `ILogger<T>`; drop the removed APIs from SKILL.md's coverage and uncovered tables; run the consumer check against a locally packed candidate (non-release version, private cache) and attach evidence
- [x] Update logging docs (builder-api.md, logging.md, output-handling.md, README if present) to teach only `AddLogging` in `ConfigureServices`; task 532 covers the broader docs pass

## Notes

### Decision (Steven, 2026-10-25): remove them

Nuru supports logging through one path: `ConfigureServices(s => s.AddLogging(...))`. The DSL interpreter reads it (`ServiceExtractor.ExtractLoggingConfiguration`, `dsl-interpreter.cs` ~1305), `ILogger<T>` is injectable, NURU_H007 warns when `ILogger<T>` is injected with no logging configured, the runtime disposes `NuruApp.LoggerFactory`, and samples teach it (`samples/fluent/09-logging/fluent-logging-console.cs`, `samples/endpoints/09-logging/console/console.cs`, the Serilog sample).

`UseLogging`, `UseConsoleLogging`, `UseDebugLogging` and `ConfigureLogging` came from the 2025-08 `TimeWarp.Nuru.Logging` package, before the source generator. The semantic interpreter (#278, 2025-12-27) never supported them, so every consumer build that calls them has failed with NURU_S999 since then; no test or sample uses them; `UseLogging(ILoggerFactory)` only sets a builder property the generated code never reads. Removing them leaves one DI-based way to configure logging. It is breaking on paper but no working app can depend on them. Ship it in the same 4.0 beta as 518's attribute removal.

why: Steven 2026-10-25 "ok let's remove them"


### Implementation notes (2026-10-25)

- `Microsoft.Extensions.Logging.Console` stays a `TimeWarp.Nuru` package reference. Inside the
  library only `NuruLoggingExtensions` used it, but consumers' `AddLogging(b => b.AddConsole())`
  resolves `AddConsole` through it transitively: the tw-nuru-verify probe projects reference
  `TimeWarp.Nuru` only and the `add-logging` probe builds because of it. Dropping it would break the
  supported path, so it is not "a package reference only they use".
- `NuruApp.LoggerFactory` stays: generated code sets it for `UseTelemetry` logging and the REPL
  reads it. Only the builder field that `UseLogging` set was removed.

### Follow-ups found (not fixed here: generator behavior changes that need their own review)

1. **Delegate handler `ILogger<T>` is null.** `.WithHandler((ILogger<X> logger) => ...)` with
   `AddLogging` configured emits `ILogger<X> logger = default!; // Service not registered`
   (`ServiceResolverEmitter.EmitServiceResolution` has no ILogger case, unlike
   `HandlerInvokerEmitter.ResolveServiceForCommand`), and there is no diagnostic. Endpoint handler
   constructors and behaviors work. logging.md now says delegate parameters are not resolved yet.
2. **Multi-app assemblies share the first app's logger factory.**
   `InterceptorEmitter.GetFirstLoggerFactoryFieldName` picks one factory for every app, so app B's
   `ILogger<T>` comes from app A's `AddLogging`. One-app consumers are unaffected; the CI
   multi-mode assembly is affected, which is why generator-66 runs standalone.

Both need a task filed from the cockpit (`ganda kanban create` in timewarp-nuru).

- Evidence: task 523 consumer check against 4.0.0-beta.5.
- Related: 523 (verification skill), the docs alignment task.
- No trailing whitespace (RCS1037). New analyzer diagnostics go in `AnalyzerReleases.Unshipped.md`, never Shipped.

## Results

- Removed `source/timewarp-nuru/logging/nuru-logging-extensions.cs` (`NuruLoggingExtensions`:
  `ConfigureLogging`, `UseConsoleLogging` x2, `UseDebugLogging`), `NuruAppBuilder.UseLogging`, the
  builder's `LoggerFactory` field and its copy in `Build()`. Added Purpose/Design regions to
  `nuru-app-builder.cs` and `nuru-app-builder.configuration.cs`.
- Changelog: `### Removed` entry marked **BREAKING** with the replacement for each method.
- Test: `tests/timewarp-nuru-tests/generator/generator-66-add-logging-injected-logger.cs`,
  `AddLogging(b => b.AddConsole().AddProvider(capture).SetMinimumLevel(Information))` + `Map<T>()`
  endpoint with constructor `ILogger<T>`; Information captured, Debug filtered. Standalone-only in CI
  (follow-up 2), listed in `tests/ci-tests/Directory.Build.props` excludes and `run-ci-tests.cs`.
- CI: `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`, exit 0
  (multi-mode 2117 total, 0 failed; standalone 0 failed, generator-66 passed).
- tw-nuru-verify: `probes/add-logging.cs` replaces `use-logging.cs` and `use-console-logging.cs`;
  `cases.tsv` and SKILL.md coverage/uncovered tables updated. Consumer check against
  `4.0.0-t527.1` (Release build to `/tmp/t527-feed`, private cache): release verdict **PASS**,
  36 passed, 0 failed; `add-logging` prints `info: WorkCommand[0] probe-log-entry` and no Debug
  entry. Docs at commit 56229740: logging.md Quick Start compiles; logging.md, builder-api.md and the
  output-handling DI example have no defects; the 22 remaining doc defects are pre-existing in
  other docs (task 532). Evidence: `evidence/4.0.0-t527.1/`.
- Docs: `documentation/user/features/logging.md` rewritten for `AddLogging` only (developer copy
  `documentation/developer/guides/logging.md` kept identical), builder-api.md Logging section,
  output-handling.md DI example (endpoint + stderr console logger), developer debugging.md parser
  snippets now call `PatternParser.Parse(pattern, loggerFactory)` (verified in a scratch runfile),
  README feature row.
- Audit: the commit-time audit failed `runfile-executable` on the new generator-66 runfile;
  `ganda repo audit --fix` set the executable bit and the audit passes.

### How to validate

Smoke:
```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-66-add-logging-injected-logger.cs
dotnet run tests/ci-tests/run-ci-tests.cs
grep -rn "UseLogging\|UseConsoleLogging\|UseDebugLogging" source/ samples/ tests/ --include=*.cs
dotnet build source/timewarp-nuru/timewarp-nuru.csproj -c Release -p:Version=4.0.0-t527.v -p:PackageOutputPath=/tmp/t527v-feed/
skills/tw-nuru-verify/scripts/nuru-consumer-check.sh --source /tmp/t527v-feed --version 4.0.0-t527.v --steps cache,probes
```

Expect:
- generator-66: `Passed: 1`, with `info: Gen66WorkCommand[0] gen66-info-entry` on the console.
- CI runner exits 0, `Standalone finished ... (0 failed)`.
- The grep finds only the comment in the `nuru-app-builder.configuration.cs` Design region and the
  generator-66 Purpose region; no call sites.
- Consumer check exits 0, `0 failed`, and `PASS probes add-logging: ConfigureServices AddLogging + ILogger<T> (Information logged)`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- 2026-10-25 cockpit: recorded the remove decision (Steven) and rewrote the checklist to match
- 2026-10-25 implementer (claude): removed the APIs, added generator-66 and the add-logging probe, rewrote the logging docs, CI green, consumer check PASS on 4.0.0-t527.1; two generator follow-ups recorded in Notes
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-25T16:22:30Z
