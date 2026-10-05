# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commits b64d055a and 46b1f6d0 (`bea516e3..HEAD`), living docs refresh for task 482-013

## Summary

The refresh removes the called-out 2.x surface (`CreateBuilder(args)`, `MapDefault` except the migration table, `AddAutoHelp`, `new NuruAppBuilder()`, `TimeWarp.Nuru.Logging`, the old parser type names) from living user and developer docs. `documentation/posts/` is not in the diff. `[NuruRoute]` remarks match NURU_A001, the DevCli readme calls `CreateBuilder()`, and `error-handling.md` matches `route-matcher-emitter.cs` (`Terminal.WriteLine` then `return 1`), `interceptor-emitter.cs` (`WriteErrorLineAsync("Unknown command. Use --help for usage.")`), and `telemetry-emitter.cs` (record `error.type`, then `throw;`) plus `NuruApp.RunAsync` (`Environment.ExitCode`, handler values are output). What remains is leftover analyzer, version, completion, and help text that still describes APIs or output the source does not produce.

## Issues

### Issue 1 — Severity: bug
- File: documentation/developer/guides/using-analyzers.md:225
- Description: The analyzer guides still teach a removed diagnostic and a `Map<T>` overload that does not exist. The sample is `.Map<PingCommand>("ping")  // NURU_D001` with `services.AddMediator()` and `dotnet add package Mediator.Abstractions` / `Mediator.SourceGenerator` (lines 218–235). `NuruAppBuilder.Map<TEndpoint>()` in `nuru-app-builder.routes.cs` takes no pattern argument. There is no `NURU_D001` descriptor under `source/timewarp-nuru-analyzers/diagnostics/` (ids stop at NURU_P*, NURU_S*, NURU_H*, NURU_R*, NURU_A*, and NURU050–NURU056). Mediator in source is `TimeWarp.Mediator` (`TimeWarp.Mediator.Contracts` / `TimeWarp.Mediator.Generators` in `timewarp-nuru.csproj`; `AddGeneratedMediator`, not `AddMediator`). The same page’s install block (lines 7–10) and `documentation/user/features/analyzer.md:21` and `:24` pin `TimeWarp.Nuru` `2.1.0-beta.9`. `analyzer.md:183` repeats `builder.Map<PingCommand>("ping");` and the Mediator.SourceGenerator packages (lines 177–187). Both pages also document `NURU_DEBUG` / `NURU_DEBUG001` (`using-analyzers.md:325`, `analyzer.md:280–293`); `rg NURU_DEBUG` over `source/` is empty.
- Suggestion: Delete the NURU_D001 and NURU_DEBUG sections. Show `.Map<PingCommand>()` with the route on `[NuruRoute]`. If mediator setup stays, name `TimeWarp.Mediator` and the TimeWarp.Mediator packages, not `Mediator.Abstractions` / `AddMediator()`. Drop the `2.1.0-beta.9` package pin from current install snippets.
- Status: open

### Issue 2 — Severity: bug
- File: documentation/developer/guides/using-analyzers.md:57
- Description: NURU_P003 is illustrated as `builder.Map("build -verbose")` with the comment “Multi-character single-dash” marked an error, and the fix is “Use double-dash for long options”. `ParseOption` / the parser segment header accept multi-character single-dash shorts (`-bl`, `-verbosity`); `OptionMatcher` matches them by exact string. NURU_P003’s message (`diagnostic-descriptors.syntax.cs`) is only “options must start with '--' or '-'”. `documentation/user/features/analyzer.md:84–93` already says `-bl` and `-verbosity` are valid. This page contradicts that and the parser.
- Suggestion: Remove the `-verbose` error example. Point NURU_P003 at an option that does not start with `-` or `--`, and point readers at the multi-character short-option note already in `analyzer.md`.
- Status: open

### Issue 3 — Severity: bug
- File: documentation/user/features/built-in-routes.md:9
- Description: The version route is documented as `--version`, `-v` (heading at line 16) and the sample output (lines 23–33) includes `Commit:` and `Date:` lines “injected by TimeWarp.Build.Tasks”. `BuiltInFlags.VersionForms` is only `["--version"]` (`built-in-flags.cs`). `VersionEmitter.Emit` writes the optional app name and `AssemblyInformationalVersion` (or `AssemblyName.Version`, or `"1.0.0"`). It does not write commit or date lines. The same `-v` and commit claims remain in `documentation/user/features/overview.md:40` (“`--version, -v` - Display version with commit info”) and `documentation/user/reference/nuru-app-options.md:169` and `:195–199`. `overview.md:41` also lists `--check-updates` as a route `CreateBuilder()` auto-registers; the emitter adds it only when `app.HasCheckUpdatesRoute` (`AddCheckUpdatesRoute()`).
- Suggestion: Document `--version` only. Show version text as the informational version line. Mark `--check-updates` as opt-in on the overview bullet, matching `built-in-routes.md`.
- Status: open

### Issue 4 — Severity: bug
- File: documentation/user/features/built-in-routes.md:86
- Description: “Pre-release versions (e.g., `1.0.0-beta.1`) only compare against other pre-releases” is the opposite of the generated checker. `check-updates-emitter.cs` sets `isPrerelease` from a `-` in the current version, then uses every GitHub release when current is a pre-release and only non-pre-release rows when current is stable (`candidateReleases = isPrerelease ? releases : releases.Where(r => !r.Prerelease)`). The stable bullet on the next line matches the emitter; the pre-release bullet does not.
- Suggestion: Say a stable current version is compared only with stable releases, and a pre-release current version is compared with all releases (stable and pre-release).
- Status: open

### Issue 5 — Severity: bug
- File: documentation/user/reference/nuru-app-options.md:113
- Description: Completion samples construct `new StaticCompletionSource("dev", "staging", "prod")` (also lines 117 and 224). The same type is in `documentation/user/features/shell-completion.md:65` and `:647`. No `StaticCompletionSource` type exists under `source/`. The registry methods `RegisterForParameter` / `RegisterForType` exist and take `ICompletionSource`. The only built-in implementation found is `EnumCompletionSource<TEnum>`.
- Suggestion: Replace `StaticCompletionSource` with a small type that implements `ICompletionSource.GetCompletions`, or with `EnumCompletionSource<TEnum>` where the values are an enum. Do not name a type the package does not ship.
- Status: open

### Issue 6 — Severity: bug
- File: documentation/user/reference/builder-api.md:238
- Description: Exit codes are “**0**: Success” and “**Non-zero**: Failure (handler threw exception)”, and the sample says “To signal failure, throw an exception” (lines 248–251). `NuruApp.RunAsync` documents that handler return values are terminal output and that a non-zero code is `Environment.ExitCode`. Generated matched routes `return Environment.ExitCode`. Binding failures `return 1` without throwing. Handler exceptions are not turned into exit code 1; with telemetry, `EmitTelemetryCatch` records the exception and `throw;`s. This contradicts `documentation/developer/reference/error-handling.md`, which this refresh got right.
- Suggestion: Replace the exit-code bullets and the throw sample with `Environment.ExitCode` for handler failure, `return 1` for generated binding and no-match failures, and propagation for handler exceptions.
- Status: open

### Issue 7 — Severity: bug
- File: documentation/user/features/auto-help.md:112
- Description: The complete example uses `.Map("version|Show application version")` and the transcript (lines 139–149) shows that pipe text as the command description, plus “Available commands:” and “Use '<command> --help'…”. A pipe after a literal is not a command description: `ParseLiteral` consumes only the identifier, and a following pipe hits `HandleUnexpectedToken` (`InvalidCharacterError` / NURU_P005). Command text comes from `.WithDescription` (this page says that correctly at line 196). Root help in `help-emitter.cs` prints `Version:`, `Usage:`, `Options:`, and `Commands:` (first literal, not the full pattern). It does not print “Available commands:” or that footer. The `deploy --help` transcript (lines 156–172) starts with `Usage: myapp deploy {env} {tag?}` and an Examples block, but the sample never calls `.WithExample`. `EmitRouteHelpContent` prints the pattern, then an indented description, then Parameters/Options tables, and Examples only when examples were declared.
- Suggestion: Use `.WithDescription("Show application version")` on `Map("version")`. Replace the transcripts with the `Version:` / `Usage:` / `Options:` / `Commands:` layout and a per-route pattern line. Show an Examples section only on a route that calls `.WithExample` or `[NuruRouteExample]`.
- Status: open

### Issue 8 — Severity: suggestion
- File: documentation/user/features/logging.md:73
- Description: The current `UseConsoleLogging` example filters `TimeWarp.Nuru.CommandResolver` and `TimeWarp.Nuru.Parsing` (same filters in `documentation/developer/guides/logging.md:73–74`). There is no `CommandResolver` type in `source/`. Parser, lexer, and compiler loggers are `ILogger<Parser>`, `ILogger<Lexer>`, and `ILogger<Compiler>` in namespace `TimeWarp.Nuru`, so the categories are `TimeWarp.Nuru.Parser`, `TimeWarp.Nuru.Lexer`, and `TimeWarp.Nuru.Compiler`. The migration table’s old `NURU_LOG_*` names are fine as history; these filters are presented as the API to call now, and they match nothing.
- Suggestion: Filter `TimeWarp.Nuru.Parser` (and Lexer/Compiler if those are the intended categories). Drop `TimeWarp.Nuru.CommandResolver` and `TimeWarp.Nuru.Parsing`.
- Status: open

### Issue 9 — Severity: suggestion
- File: documentation/developer/guides/debugging.md:351
- Description: This refresh added `[Nuru Logging Extensions](../../source/timewarp-nuru/logging/nuru-logging-extensions.cs)`. From `documentation/developer/guides/` that resolves to `documentation/source/...`, which does not exist. The file is `source/timewarp-nuru/logging/nuru-logging-extensions.cs` at the repo root (`../../../source/...`). Other broken links in this file use the old PascalCase `Source/` and `Tests/` paths and were already broken before this diff.
- Suggestion: Point the new logging link at `../../../source/timewarp-nuru/logging/nuru-logging-extensions.cs`.
- Status: open
