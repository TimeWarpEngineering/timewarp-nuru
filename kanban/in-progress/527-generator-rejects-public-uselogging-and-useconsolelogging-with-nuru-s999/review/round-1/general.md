# Round 1 — general
**Date:** 2026-10-25
**Scope reviewed:** branch `task/527-generator-rejects-public-uselogging-and-useconsole` vs merge-base `cc2176cb` — logging API removal, generator-66, tw-nuru-verify `add-logging`, changelog, logging docs.

## Summary

The implementer restatement matches the decided task. The description still says the public methods should work, and Notes plus the rewritten checklist record Steven's 2026-10-25 decision to remove them. The restatement follows that checklist: drop the five methods and the builder `LoggerFactory` field, leave the `AddLogging` path alone, mark the changelog breaking, prove the path with `Map<T>()` and an injected `ILogger<T>`, replace the two probes with `add-logging`, and teach `AddLogging` in the named docs. No misread.

`UseLogging`, both `UseConsoleLogging` overloads, `UseDebugLogging`, and `ConfigureLogging` are gone with `nuru-logging-extensions.cs`. The builder field and the `Build()` copy are gone. `NuruApp.LoggerFactory` disposal, the telemetry emitter that assigns it, `ServiceExtractor.ExtractLoggingConfiguration`, and NURU_H007 are outside the diff. `Microsoft.Extensions.Logging.Console` stays on `timewarp-nuru.csproj`, which is what lets a `TimeWarp.Nuru`-only consumer call `AddConsole()`. The changelog entry is under `## [Unreleased]` / `### Removed` and is marked breaking. generator-66 calls `AddLogging(b => b.AddConsole().AddProvider(...).SetMinimumLevel(Information))`, maps `Gen66WorkCommand`, and asserts the Information entry is present and the Debug entry is absent. It is excluded from the multi-mode assembly and listed in the standalone phase. That matches `InterceptorEmitter.GetFirstLoggerFactoryFieldName`, which returns the first app's factory for every app; the exclusion comment says so, and the test still runs. The `add-logging` probe's evidence log shows `info: WorkCommand[0]` / `probe-log-entry` and no debug line, and the release verdict is 36 passed, 0 failed. Call sites of the removed methods in `source/`, `samples/`, and `tests/` are comments only.

## Issues

### Issue 1 — Severity: suggestion
- File: documentation/user/features/logging.md:178
- Description: "Nuru's Own Log Messages" says the parser, lexer, and compiler messages are written in the source generator at build time. The generator parses with `PatternParser.TryParse` and `new Parser()`, and `TryParse` constructs `Parser` and `Compiler` with no logger (`pattern-parser.cs` comment "without logger"). `ParsingPattern`, `DumpingTokens`, `DumpingAst`, and `SettingBooleanOptionParameter` are inside `#if !ANALYZER_BUILD`, and the analyzer project defines `ANALYZER_BUILD`, so a build does not write them. They are written only when a non-analyzer build calls `PatternParser.Parse(pattern, loggerFactory)`. The same sentence is in `documentation/developer/guides/logging.md:178`. The code sample under the sentence is the call that actually logs.
- Suggestion: Say the generator parses with no logger, so neither the build nor a running app writes these messages, and keep the `PatternParser.Parse(pattern, loggerFactory)` sample as the way to see them. Update both logging.md copies.
- Status: open
