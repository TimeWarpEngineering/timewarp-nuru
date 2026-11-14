# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** commit `67931f0e` — NURU_T001 / NURU_T002 for `UseTelemetry` options the generator cannot read

## Summary

`TelemetryOptionsExtractor` now reports NURU_T001 at a non-constant value of a known `UseTelemetry` option (`OtlpEndpoint`, `ServiceName`, `ServiceVersion`, `EnableTracing`, `EnableMetrics`, `EnableLogging`) and at a non-constant `ActivitySources.Add` or `Meters.Add` argument, naming the fallback that the emitter already uses. A configure argument that is not an inline lambda reports NURU_T002, and the interpreter keeps the default `UseTelemetry()` model. Literals and consts still go through `GetConstantValue` and are emitted as before.

The change matches the task and the implementer restatement. The descriptor, `AnalyzerReleases.Unshipped.md` entry, telemetry guide, changelog, skill line, and generator-43 cases (literal, const local, const member, variable, property, method call, delegate, env fallback) cover the acceptance list. `## Results` and `### How to validate` record the build, the CI run (2184 passed, 6 skipped, 0 failed; standalone run with no failures), and the audit. Those gates were not re-run.

## Issues

None.
