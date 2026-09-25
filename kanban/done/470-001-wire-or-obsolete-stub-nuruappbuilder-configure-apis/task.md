# Wire or obsolete stub NuruAppBuilder configure APIs

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M1, M2, M3). Nits folded: M34, M35.

## Description

Several public `NuruAppBuilder` configure APIs document runtime behavior that is never applied because they are compile-time stubs, the generator looks for a removed method name, or a field is never assigned.

### M1 — `UseTelemetry(Action<NuruTelemetryOptions>)` discards the lambda
`source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs:179-185` returns immediately. DSL dispatch (`dsl-interpreter.cs:1424-1434`) only calls parameterless `UseTelemetry()`. Emitted setup reads `OTEL_*` env vars (`telemetry-emitter.cs:42-48`). Documented `NuruTelemetryOptions` properties are never applied.

### M2 — `ConfigureHelp` / `HelpOptions` filtering is dead
`ConfigureHelp` mutates a builder field (`nuru-app-builder.cs:90-95`) but no emitter reads `ShowPerCommandHelpRoutes` / `ShowReplCommandsInCli` / `ShowCompletionRoutes` / `ExcludePatterns`. Help is always enabled (`nuru-generator.cs:373-377` `HasHelp = true`). Generator locator/interpreter still look for removed `AddHelp` (`add-help-locator.cs:12`). User docs advertise `.ConfigureHelp(options => …)` (`documentation/user/reference/builder-api.md:203-206`).

### M3 — `Services` always throws
`nuru-app-builder.cs:26-37` tells the caller to `Call AddDependencyInjection() first`. That method does not exist (opt-in is `UseMicrosoftDependencyInjection`). `ServiceCollection` is never assigned.

## Requirements

- Either wire each API end-to-end (runtime and/or generator IR) or obsolete/remove it and stop advertising it as live.
- Locate `ConfigureHelp` (not `AddHelp`) if help filtering stays.
- Fix `Services` exception text and assignment, or obsolete the property in favor of `ConfigureServices`.
- Update XML docs/examples that still cref `AddHelp`, two-arg `Map`, and `AddDependencyInjection()` (M34).
- Delete or wire unused `EmptyServiceProvider` / `NuruLoggingBuilder` / `NuruMetricsBuilder` (M35).
- Tests for whichever behavior is kept.

## Checklist

- [x] M1 telemetry options: wire or obsolete
- [x] M2 help filtering: wire into HelpEmitter / HelpModel, or stop advertising
- [x] M3 Services / AddDependencyInjection naming
- [x] M34 XML/docs examples
- [x] M35 dead DI leftover types
- [x] Tests
- [x] `ganda runfile cache --clear` + CI tests if generator emit changes
- [x] Implementation review under `review/` (effort 1, disposition clean)

## Notes

Evidence and merge IDs: parent 470 `review/round-1/merged.md` (M1–M3, M34, M35).

Review kitchen: `review/review-framework.md`, `review/round-1/`, `review/round-2/`, `review/disposition.md`. Effort 1, general only. Round 1 raised M1 (`IsPerCommandHelpRoute` substring). Fixed on this id. Round 2 re-verified; 0 open. Disposition `clean`.

## Session

- Created: ganda claim 3385166 (2026-09-04)
- Implementer: grok session 01a0c70a-96e6-7c81-a2af-2f0b22a63099 (2026-09-22)
- Review oracle: grok session 01a0c727-75be-7e52-979a-c9b318b5b028 (2026-09-22)
- Reviewer (general, round 1): 01a0c729-5178-7a83-a336-2374b4a10aca
- Reviewer (general, round 2): 01a0c735-66c2-7672-b3f5-c861261f0354

## Results

Wired the stub builder configure APIs that were advertised as live, and obsoleted the one that cannot work under source-gen DI.

**M1 — UseTelemetry options.** DSL dispatch extracts `UseTelemetry(Action<NuruTelemetryOptions>)` into `TelemetryModel`. The telemetry emitter applies `ServiceName`, `ServiceVersion`, `OtlpEndpoint`, `EnableTracing`, `EnableMetrics`, and `EnableLogging`. Property-then-env for the OTLP endpoint; env-then-property for service name/version, matching `NuruTelemetryOptions`.

**M2 — ConfigureHelp filtering.** Interpreter locates `ConfigureHelp` (replaced `AddHelp`). `HelpModel` now mirrors `HelpOptions`. `HelpEmitter` hides per-command `--help` rows and completion routes by default, optionally lists REPL commands and completion routes, and honors `ExcludePatterns` wildcards.

**M3 — Services.** `[Obsolete]` on `NuruAppBuilder.Services` with an exception that names `ConfigureServices` and `UseMicrosoftDependencyInjection`. The unused `ServiceCollection` field is gone.

**M34.** `CreateBuilder` XML crefs `ConfigureHelp` and shows `.Map(...).WithHandler(...).Done()`. `ConfigureServices` examples use `UseMicrosoftDependencyInjection` and `Map<T>()`.

**M35.** Deleted unused `EmptyServiceProvider`, `NuruLoggingBuilder`, and `NuruMetricsBuilder`.

### Files changed

- Generator: `help-model.cs`, `telemetry-model.cs`, extractors, `dsl-interpreter.cs`, `ir-app-builder.cs`, `iir-app-builder.cs`, `help-emitter.cs`, `telemetry-emitter.cs`, `interceptor-emitter.cs`, `configure-help-locator.cs` (replaces `add-help-locator.cs`)
- Runtime: `nuru-app-builder.cs`, `nuru-app-builder.configuration.cs`, `nuru-app.cs`, `help-options.cs`, `global-usings.cs`
- Deleted: `empty-service-provider.cs`, `nuru-logging-builder.cs`, `nuru-metrics-builder.cs`
- Tests: `help-10-configure-help-filtering.cs`, `telemetry-01-use-telemetry-options.cs`, `builder-01-services-property.cs`, `generator-43-telemetry-options.cs` (standalone)

### Key decisions

- Wire telemetry options and help filtering through generator IR rather than runtime, matching every other builder configure API.
- Obsolete `Services` instead of populating a collection the generator never reads.
- `ExcludePatterns` keeps a setter so documented collection-expression assignment compiles.

### Test outcomes

- `builder-01-services-property.cs`: 1 passed
- `help-10-configure-help-filtering.cs`: 7 passed (includes `--helper` regression after review M1)
- `telemetry-01-use-telemetry-options.cs`: 1 passed
- `generator-43-telemetry-options.cs`: 2 passed
- `help-02-table-formatting.cs` (regression): 9 passed
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`: 1634 passed, 7 skipped, 0 failed (includes generator-43 standalone)

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-470-001-wire-or-obsolete-stub-nuruappbuilder-configure-api
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/help/help-10-configure-help-filtering.cs
dotnet run tests/timewarp-nuru-tests/telemetry/telemetry-01-use-telemetry-options.cs
dotnet run tests/timewarp-nuru-tests/builder/builder-01-services-property.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-43-telemetry-options.cs
```

**Expect**

- help-10: 7 passed (`ExcludePatterns` hides `h10-secret-debug`; default `--help` omits `h10-status --help` and `__complete`; opt-in lists those rows plus REPL `clear-history`; `h10-run --helper` still lists)
- telemetry-01: 1 passed (`TracerProvider` set, `MeterProvider` null when metrics disabled and `OtlpEndpoint` is set)
- builder-01: 1 passed (`InvalidOperationException` message contains `ConfigureServices` and `UseMicrosoftDependencyInjection`)
- generator-43: 2 passed (generated source contains `tel43-cli` and `http://127.0.0.1:4318`; tracing-disabled source has no `app.TracerProvider`)

**Automated gate**

```bash
ganda runfile cache --clear
dotnet run tests/ci-tests/run-ci-tests.cs
# expect: multi-mode + standalone generator tests pass, including generator-43
```

### Review disposition

- **Outcome:** `clean`
- **Effort / roster:** 1, general only
- **Rounds:** 2 (`review/round-1/`, `review/round-2/`)
- **Final counts:** 0 open / 1 fixed / 0 wontfix (bug M1: `IsPerCommandHelpRoute` substring); 0 suggestion; 0 nit
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`
- No sibling apply-review task; M1 fixed on this id (LongForm-only classification + help-10 regression).
