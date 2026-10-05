# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general, plus one orchestrator finding (M10) verified against `source/` after the reviewer pass

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 10 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

Statuses below were set to `fixed` after the doc edits on this task and before round 2 opened. Round 2 re-checked them.

## Issues

### M1 — Severity: bug — Status: fixed
- File: documentation/developer/guides/using-analyzers.md:225
- Description: Analyzer guides still teach `NURU_D001`, `Map<PingCommand>("ping")`, `AddMediator()`, and `Mediator.Abstractions` / `Mediator.SourceGenerator`. `Map<TEndpoint>()` takes no pattern. There is no `NURU_D001` descriptor. Mediator types are `TimeWarp.Mediator` (`AddGeneratedMediator`). Both pages pin `TimeWarp.Nuru` `2.1.0-beta.9` (`using-analyzers.md:7–10`, `analyzer.md:21` and `:24`). Both document `NURU_DEBUG` / `NURU_DEBUG001` (`using-analyzers.md:325`, `analyzer.md:280–293`); `source/` has no `NURU_DEBUG` id. `analyzer.md:183` and the dependency table at `analyzer.md:348` repeat the same removed diagnostic.
- Suggestion: Delete the NURU_D001 and NURU_DEBUG sections. Show `.Map<PingCommand>()` with the route on `[NuruRoute]`, and name `TimeWarp.Mediator` if mediator setup stays. Drop the `2.1.0-beta.9` pin from current install snippets.
- Source: general Issue 1
- Disposition notes:

### M2 — Severity: bug — Status: fixed
- File: documentation/developer/guides/using-analyzers.md:57
- Description: NURU_P003 is illustrated as `builder.Map("build -verbose")` with “Multi-character single-dash” marked an error. Multi-character single-dash shorts (`-verbose`, `-bl`, `-verbosity`) are valid and match exactly. POSIX grouping is not supported. `InvalidOptionFormatError` is never constructed. `analyzer.md:84–93` already says the multi-character forms are valid. NURU_P003’s message is only “options must start with '--' or '-'”.
- Suggestion: Remove the false `-verbose` error sample. Align this page with `analyzer.md`. Do not invent a pattern that emits NURU_P003.
- Source: general Issue 2
- Disposition notes:

### M3 — Severity: bug — Status: fixed
- File: documentation/user/features/built-in-routes.md:9
- Description: Version is documented as `--version`, `-v` and the sample output includes `Commit:` and `Date:` lines. `BuiltInFlags.VersionForms` is only `["--version"]`. `VersionEmitter.Emit` writes an optional app name and `AssemblyInformationalVersion` (or `AssemblyName.Version`, or `"1.0.0"`). The same `-v` and commit claims are in `overview.md:40` and `nuru-app-options.md:169` and `:195–199`. `overview.md:41` lists `--check-updates` as auto-registered by `CreateBuilder()`; it is added only by `AddCheckUpdatesRoute()`. `--interactive`, `-i` requires `AddRepl()`.
- Suggestion: Document `--version` only. Show the informational version line. Mark `--check-updates` as opt-in and interactive as `AddRepl()`.
- Source: general Issue 3
- Disposition notes:

### M4 — Severity: bug — Status: fixed
- File: documentation/user/features/built-in-routes.md:86
- Description: “Pre-release versions only compare against other pre-releases” is the opposite of `check-updates-emitter.cs`. A stable current version is compared only with stable releases. A pre-release current version (`-` in the version) is compared with all releases.
- Suggestion: State that comparison the way the emitter implements it.
- Source: general Issue 4
- Disposition notes:

### M5 — Severity: bug — Status: fixed
- File: documentation/user/reference/nuru-app-options.md:113
- Description: Completion samples construct `new StaticCompletionSource(...)` (`nuru-app-options.md:113`, `:117`, `:224`; `shell-completion.md:65` and `:647`). No such type exists. `RegisterForParameter` / `RegisterForType` take `ICompletionSource`. The built-in implementation is `EnumCompletionSource<TEnum>`. `CompletionCandidate` is `(string Value, string? Description, CompletionType Type, string? ParameterType = null, CompletionDirective Directive = None)`.
- Suggestion: Show a user type that implements `ICompletionSource`, and `EnumCompletionSource<LogLevel>()` where the values are an enum. Do not name a type the package does not ship. LogLevel candidates are the enum names, not `"info"`.
- Source: general Issue 5
- Disposition notes:

### M6 — Severity: bug — Status: fixed
- File: documentation/user/reference/builder-api.md:238
- Description: Exit codes are “0: Success” and “Non-zero: Failure (handler threw exception)”, and the sample says to throw to signal failure. Handler return values are terminal output. Matched routes return `Environment.ExitCode`. Binding failures and no-match return 1. Handler exceptions propagate (telemetry records `error.type` and rethrows).
- Suggestion: Document `Environment.ExitCode` for handler failure, `return 1` for binding and no-match, and propagation for handler exceptions.
- Source: general Issue 6
- Disposition notes:

### M7 — Severity: bug — Status: fixed
- File: documentation/user/features/auto-help.md:112
- Description: The complete example uses `.Map("version|Show application version")`. A pipe after a command literal is NURU_P005, not a description. Command text comes from `.WithDescription`. Root help prints `Version:`, `Usage:` (`{app} [command] [options]`), `Options:`, and `Commands:` (first literal plus description). It does not print “Available commands:” or a “Use '<command> --help'” footer. Per-route help prints the pattern (`{name}` required, `[name]` optional, `[--long,-short]` for an optional option), an indented description, Parameters (Name/Required/Type/Description) and Options tables, Examples only when `.WithExample` / `[NuruRouteExample]` is declared, then “Values may come from --json-args.”. The same false layout is in the earlier transcripts on this page (about lines 32–38, 63–72, and 96–104). Parameter and option pipes in the samples are valid.
- Suggestion: Use `.WithDescription` for command text. Replace the transcripts with the emitter layout. Show Examples only on a route that calls `.WithExample`.
- Source: general Issue 7
- Disposition notes:

### M8 — Severity: bug — Status: fixed
- File: documentation/user/features/logging.md:73
- Description: Promoted from the reviewer’s suggestion. Both logging pages (`documentation/user/features/logging.md` and `documentation/developer/guides/logging.md`) present `TimeWarp.Nuru.CommandResolver` and `TimeWarp.Nuru.Parsing` as the categories to filter now (lines 73–74, 145–146, 161–180, 210, 224–225, 257, 268, 342–343). There is no `CommandResolver` type. Live loggers are `ILogger<Parser>`, `ILogger<Lexer>`, and `ILogger<Compiler>` in namespace `TimeWarp.Nuru` (categories `TimeWarp.Nuru.Parser`, `TimeWarp.Nuru.Lexer`, `TimeWarp.Nuru.Compiler`). Callers that exist: Parser `ParsingPattern` (1100), `DumpingTokens` (1101), `DumpingAst` (1102); Lexer `StartingLexicalAnalysis` (1050), `CompletedLexicalAnalysis` (1051), `DumpingTokens` (1101); Compiler `SettingBooleanOptionParameter` (1103). `StartingRouteRegistration` / `ResolvingCommand` / `MatchedRoute` have no callers. The migration table may name `NURU_LOG_*` as removed history. The replacement column must not tell the reader to filter a category that does not exist.
- Suggestion: Point filters, the category table, sample lines, and the migration replacement column at Parser, Lexer, and Compiler. Base sample lines on messages that are called.
- Source: general Issue 8 (severity raised by the orchestrator: the refreshed page teaches a removed type as the current API)
- Disposition notes:

### M9 — Severity: bug — Status: fixed
- File: documentation/developer/guides/debugging.md:351
- Description: Promoted from the reviewer’s suggestion. This refresh added `[Nuru Logging Extensions](../../source/timewarp-nuru/logging/nuru-logging-extensions.cs)`. From `documentation/developer/guides/` that resolves under `documentation/`, not the repo `source/` tree. The file is `source/timewarp-nuru/logging/nuru-logging-extensions.cs` (`../../../source/...`). The adjacent bullet links `../../Source/TimeWarp.Nuru/CommandResolver/RouteBasedCommandResolver.cs`, which does not exist and names a deleted type. The logger-message bullet above it uses the same stale `Source/` prefix. Other PascalCase doc links on this page (`Logging.md`, `Glossary.md`) are pre-existing and stay out of this fix.
- Suggestion: Point the logging-extensions link (and the logger-message bullet in the same list) at `../../../source/...`. Remove the dead `RouteBasedCommandResolver` link. Do not rewrite the rest of the debugging guide.
- Source: general Issue 9 (severity raised by the orchestrator: the link this diff added does not resolve)
- Disposition notes:

### M10 — Severity: bug — Status: fixed
- File: documentation/user/reference/supported-types.md:370
- Description: Orchestrator finding, not in `general.md`. The invalid-type sample marks `{value:float}` as NURU_P004 and says to use `double`. `float` is in `BuiltInTypeNames`. `{value:integer}` is the invalid example. This commit reformatted the fluent chain and kept the false comment. The built-in table (lines 9–20) is a partial list and does not include `float`.
- Suggestion: Remove `float` from the invalid example and add a `float` row to the built-in table. Do not expand the table to every other omitted built-in name.
- Source: orchestrator, verified against `source/timewarp-nuru-parsing/parsing/parser/built-in-type-names.cs`
- Disposition notes:

## Duplicates / conflicts

- general Issue 8 and Issue 9 were suggestions. Both are bugs here: the filters and the new link describe APIs or paths the tree does not have.
- M10 was missed by the reviewer pass and is added as its own id.
- Pre-existing items left out of this merge: handler-less `.Done()` in `auto-help.md` (shared-prefix sample), `documentation/posts/`, design-doc `EndpointCollection`, PascalCase links other than the debugging implementation list this diff touched, and the wrong `samples/10-type-converters/` path in `supported-types.md`.
