# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** branch `task/541-delegate-handler-iloggert-parameter-is-silently-nu` vs merge-base `3f595ea9` (product diff: logger path, service resolver, validator, H007, docs, probe, generator-66/67)

## Summary

Delegate handler parameters now resolve `ILogger<T>`, `ILogger`, `ILoggerFactory`, and typed `AddHttpClient` services through `ServiceResolverEmitter.ResolveServiceExpression`, and the three previous logger copies go through `LoggerExpressions`. With `AddLogging`, generator-66 writes an Information log from a fluent delegate and filters Debug; without logging, generator-67 expects a NullLogger and one `NURU_H007`. The factory check is a suffix test on the whole type name, so a legal `ILogger<T>` whose type argument ends in `ILoggerFactory` is emitted as the factory and no longer compiles.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/logger-expressions.cs:63
- Description: `LoggerExpressions.Resolve` decides `ILoggerFactory` with `typeName.EndsWith("ILoggerFactory")` before it reads the `ILogger<T>` category. `IsLoggingServiceType` already accepts `Microsoft.Extensions.Logging.ILogger<T>`, and parameter types are `FullyQualifiedFormat`, so `ILogger<ILoggerFactory>` and `ILogger<MyApp.CustomILoggerFactory>` both end with `ILoggerFactory`. Those parameters used to emit `CreateLogger<T>()` (the old extractor took the first `<` ... last `>`). They now emit the factory field (`ILoggerFactory __loggerFactory` or `app.LoggerFactory`) assigned to `ILogger<T>`, which does not compile. The same suffix test in `NuruGenerator.ReportLoggerWithoutConfigurationWarnings` (nuru-generator.cs:308) skips `NURU_H007` for that category when logging is off. A non-generic `Microsoft.Extensions.Logging.ILoggerFactory` is unaffected and correctly returns the factory.
- Suggestion: Treat the factory only when the name after `Microsoft.Extensions.Logging.` is exactly `ILoggerFactory` (no generic argument), and use that check in both `LoggerExpressions.Resolve` and the H007 skip. Cover `ILogger<ILoggerFactory>` in generator-67: the generated statement is `CreateLogger<...ILoggerFactory>()`, and with no logging the category warns once.
- Status: open
