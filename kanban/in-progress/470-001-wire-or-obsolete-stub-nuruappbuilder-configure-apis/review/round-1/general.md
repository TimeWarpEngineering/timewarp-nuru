# Round 1 — general
**Date:** 2026-09-22
**Scope reviewed:** branch task/470-001-wire-or-obsolete-stub-nuruappbuilder-configure-api vs origin/master

## Summary

The stub builder configure APIs are wired through generator IR as claimed: `UseTelemetry(Action<NuruTelemetryOptions>)` extracts all six option properties (including `EnableLogging` and `ServiceVersion`) and the telemetry emitter applies them with the documented property-then-env OTLP / env-then-property service name-version precedence; `ConfigureHelp` is located by name (not `AddHelp`) and `HelpEmitter` honors the HelpOptions defaults (per-command `--help` rows, REPL commands, and completion routes hidden unless opted in) plus `ExcludePatterns` wildcards. `NuruAppBuilder.Services` is obsolete with exception text naming `ConfigureServices` / `UseMicrosoftDependencyInjection`, M34 XML examples no longer cref `AddHelp` / two-arg `Map` / `AddDependencyInjection`, and the M35 leftover types are gone. Risk is concentrated in new help-filter matching: `IsPerCommandHelpRoute` treats any pattern that merely contains the substring `--help` as a per-command help route, so default listings hide unrelated flags such as `--helper`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:231
- Description: `IsPerCommandHelpRoute` uses `OriginalPattern.Contains("--help")` / `FullPattern.Contains("--help")` (Ordinal). Because `--help` is a prefix of other long-form names, a user route such as `run --helper` or `run --help-all` is classified as a per-command help route. `ShowPerCommandHelpRoutes` defaults to false, so those commands are omitted from CLI `--help` even though they are not help routes. The later `OptionDefinition.LongForm is "help"` walk is the correct check and already covers documented patterns like `blog --help?`.
- Suggestion: Drop the substring `Contains` (or require a token boundary: `--help` followed by end-of-pattern, whitespace, or optional `?`). Keep the `LongForm == "help"` segment test. Add a regression that maps `--helper` and asserts it still appears in default `--help`.
- Status: open
