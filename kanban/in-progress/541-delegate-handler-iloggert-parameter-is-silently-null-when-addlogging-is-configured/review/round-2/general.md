# Round 2 — general
**Date:** 2026-10-26
**Scope reviewed:** delta `2e0fd8e8..3b5a6504` (M1 fix). `LoggerExpressions.IsLoggerFactoryType`, the NURU_H007 skip, and the two generator-67 `ILogger<ILoggerFactory>` cases.

## Summary

`LoggerExpressions.IsLoggerFactoryType` is true only when the name, after one leading `global::`, is exactly `Microsoft.Extensions.Logging.ILoggerFactory`. `Resolve` and `ReportLoggerWithoutConfigurationWarnings` both use that check. A fully qualified `ILogger<ILoggerFactory>` is `global::Microsoft.Extensions.Logging.ILogger<global::Microsoft.Extensions.Logging.ILoggerFactory>`, which ends in `>`, so it stays on `CreateLogger<T>()` and still warns once when logging is off. The non-generic factory still resolves to the factory field or `NullLoggerFactory.Instance`. Generator-67 locks both the no-logging and `AddLogging` shapes. No new defects on this delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/logger-expressions.cs:48
- Description: Round 1 asked for an exact `ILoggerFactory` test in `Resolve` and in the NURU_H007 skip, plus a generator-67 case for `ILogger<ILoggerFactory>`. `IsLoggerFactoryType` is that test at both call sites (`logger-expressions.cs:77`, `nuru-generator.cs:308`). The two new tests expect `CreateLogger<global::Microsoft.Extensions.Logging.ILoggerFactory>()` and, with logging off, one `NURU_H007`.
- Suggestion: Landed in `3b5a6504`.
- Status: fixed

## Issues

No new issues.
