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

- [ ] M1 telemetry options: wire or obsolete
- [ ] M2 help filtering: wire into HelpEmitter / HelpModel, or stop advertising
- [ ] M3 Services / AddDependencyInjection naming
- [ ] M34 XML/docs examples
- [ ] M35 dead DI leftover types
- [ ] Tests
- [ ] `ganda runfile cache --clear` + CI tests if generator emit changes

## Notes

Evidence and merge IDs: parent 470 `review/round-1/merged.md` (M1–M3, M34, M35).

## Session

- Created: ganda claim 3385166 (2026-09-04)
