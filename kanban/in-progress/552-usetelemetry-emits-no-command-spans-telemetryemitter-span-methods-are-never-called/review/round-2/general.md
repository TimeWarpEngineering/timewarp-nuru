# Round 2 — general
**Date:** 2026-10-28
**Scope reviewed:** fix delta `2f834e7d` vs round-1 base `1c4555d1` (behavior command initializer naming, command-construction flag supplied-name, telemetry-05 short-flag and kebab-option tests).

## Summary

The fix makes `UseTelemetry()` compile the route shapes round 1 rejected. `BehaviorEmitter.EmitCommandCreation` now takes property names from `CommandClassEmitter.GetParameterPropertyName` and `GetOptionPropertyName` (`LongForm ?? ParameterName ?? ShortForm`, with that class's kebab and snake `ToPascalCase`) and local names from the route matcher: flags use `ToCamelCase(LongForm ?? ShortForm)`, value options use `ParameterName` as written. `CommandConstructionEmitter.Supplied` uses the same short-form fallback for a flag bool. The goal restatement still matches the task: one mechanism, `TelemetryBehavior` registered by `UseTelemetry()`, dead emitter twins removed, one span, no double span, parent `Activity.Current`, redaction kept.

Checked against `route-matcher-emitter.cs` (`EmitFlagParsingWithIndexTracking` and `EmitValueOptionParsingWithIndexTracking`) and `command-class-emitter.cs`. A short-only flag `-v` is property `V` and local `v`. Two short flags `-i -t` are `I = i` and `T = t`. `--target-name {targetName?}` is property `TargetName` and local `targetName`, which is the `Target-name = targetname` pair the fix pass found. A long form still wins over the short form, so existing `--verbose` locals stay `verbose`. telemetry-05 adds three tests that build those routes under `UseTelemetry()` alone, run them, check the bound handler values, and expect one span named by the route pattern.

This pass did not execute telemetry-05. A `dotnet run` writes build output outside the kitchen, and that command was rejected. The naming check is the emitter expressions themselves, which now call the same helpers as the command class and copy the matcher formulas.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs:213
- Description: Round 1: a short-only flag under `UseTelemetry()` initialized `Option = flag` while the command class and the matcher used the short form. The fix pass also covered `--target-name {targetName?}`, which the old first-letter `ToPascalCase` and `ToLowerInvariant` turned into `Target-name = targetname`. The initializer now uses `GetOptionPropertyName` and the matcher local rule. `Supplied()` for an endpoint flag uses `LongForm ?? ShortForm`, matching the matcher bool (`parameter-binding.cs` documents that flag `SourceName` is `LongForm ?? ShortForm`).
- Suggestion: none remaining on this finding.
- Status: fixed

## Issues

<!-- No new findings. -->
