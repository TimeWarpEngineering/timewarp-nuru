# Round 2 — general
**Date:** 2026-10-05
**Scope reviewed:** fix delta for M1–M10 on this task. Orchestrator re-check against `source/`. Round 1 `general.md` was not edited.

## Summary

The doc edits remove the false diagnostics, version text, completion type, exit-code rule, help transcripts, logger categories, broken logging link, and the `{value:float}` NURU_P004 example. Each prior id matches the emitters and parsers checked in round 1. No new open defect on the fix delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: documentation/developer/guides/using-analyzers.md, documentation/user/features/analyzer.md
- Description: Install snippets are unpinned `PackageReference` elements. `NURU_D001` and `NURU_DEBUG` sections are gone. `.Map<PingCommand>()` has no pattern argument. The route is `[NuruRoute("ping")]`. The pages name `TimeWarp.Mediator` and say not to call `AddMediator()` or reference `Mediator.Abstractions` / `Mediator.SourceGenerator`.
- Suggestion: none
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: documentation/developer/guides/using-analyzers.md
- Description: `-verbose` and `-bl` are shown as valid shorts. The page states that matching is exact and that the parser does not construct `InvalidOptionFormatError` for a dash-prefixed token. No invented NURU_P003 trigger.
- Suggestion: none
- Status: fixed

### M3 — Severity: bug — Status: fixed
- File: documentation/user/features/built-in-routes.md, documentation/user/features/overview.md, documentation/user/reference/nuru-app-options.md
- Description: Version is `--version` only. Sample output is the version line, with an optional app-name prefix. No commit or date lines. `--check-updates` is opt-in. `--interactive`, `-i` requires `AddRepl()`.
- Suggestion: none
- Status: fixed

### M4 — Severity: bug — Status: fixed
- File: documentation/user/features/built-in-routes.md
- Description: Stable current versions compare only with stable releases. A pre-release current version (a `-` in the version) compares with every release.
- Suggestion: none
- Status: fixed

### M5 — Severity: bug — Status: fixed
- File: documentation/user/reference/nuru-app-options.md, documentation/user/features/shell-completion.md
- Description: Samples use a user `ICompletionSource` (`CompletionCandidate` value, description, `CompletionType.Parameter`) and `EnumCompletionSource<LogLevel>`. No `StaticCompletionSource`.
- Suggestion: none
- Status: fixed

### M6 — Severity: bug — Status: fixed
- File: documentation/user/reference/builder-api.md
- Description: Exit codes follow `Environment.ExitCode`, with 1 for binding failure and no match. Handler exceptions propagate. The throw-to-fail sample is gone.
- Suggestion: none
- Status: fixed

### M7 — Severity: bug — Status: fixed
- File: documentation/user/features/auto-help.md
- Description: Command text uses `.WithDescription`. Root transcripts use `Version:` / `Usage:` / `Options:` / `Commands:`. Per-route transcripts use the pattern line, parameter columns, and examples only on the route that calls `.WithExample`. `{tag?}` renders as `[tag]`. `--compress,-c` without `?` is not bracketed. Parameter and option pipes stay.
- Suggestion: none
- Status: fixed

### M8 — Severity: bug — Status: fixed
- File: documentation/user/features/logging.md, documentation/developer/guides/logging.md
- Description: Both copies filter `TimeWarp.Nuru.Parser`, `TimeWarp.Nuru.Lexer`, and `TimeWarp.Nuru.Compiler`. Sample lines are `ParsingPattern`, lexer start/complete, and `SettingBooleanOptionParameter`. The migration table keeps `NURU_LOG_*` as variables that are not read, and the replacement column does not name `CommandResolver`.
- Suggestion: none
- Status: fixed

### M9 — Severity: bug — Status: fixed
- File: documentation/developer/guides/debugging.md
- Description: The logging-extensions link and the logger-message link use `../../../source/...` and those files exist. The `RouteBasedCommandResolver` link is removed. Filter samples on this page use Parser, Lexer, and Compiler. The opening states that events 1000–1001 and 1200–1355 have no callers. The message-id tables remain as that catalog.
- Suggestion: none
- Status: fixed

### M10 — Severity: bug — Status: fixed
- File: documentation/user/reference/supported-types.md
- Description: `{value:float}` is in the valid sample and the built-in table (`Single`). `{value:integer}` remains the NURU_P004 example.
- Suggestion: none
- Status: fixed

## Issues

<!-- No new issues. -->
