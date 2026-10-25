# Round 1 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/logger-expressions.cs:63
- Description: `LoggerExpressions.Resolve` decides `ILoggerFactory` with `typeName.EndsWith("ILoggerFactory")` before it reads the `ILogger<T>` category. `IsLoggingServiceType` already accepts `Microsoft.Extensions.Logging.ILogger<T>`, and parameter types are `FullyQualifiedFormat`, so `ILogger<ILoggerFactory>` and `ILogger<MyApp.CustomILoggerFactory>` both end with `ILoggerFactory`. Those parameters used to emit `CreateLogger<T>()` (the old extractor took the first `<` ... last `>`). They now emit the factory field (`ILoggerFactory __loggerFactory` or `app.LoggerFactory`) assigned to `ILogger<T>`, which does not compile. The same suffix test in `NuruGenerator.ReportLoggerWithoutConfigurationWarnings` (nuru-generator.cs:308) skips `NURU_H007` for that category when logging is off. A non-generic `Microsoft.Extensions.Logging.ILoggerFactory` is unaffected and correctly returns the factory.
- Suggestion: Treat the factory only when the name after `Microsoft.Extensions.Logging.` is exactly `ILoggerFactory` (no generic argument), and use that check in both `LoggerExpressions.Resolve` and the H007 skip. Cover `ILogger<ILoggerFactory>` in generator-67: the generated statement is `CreateLogger<...ILoggerFactory>()`, and with no logging the category warns once.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. M1 is general issue 1, including both call sites of the suffix check.
