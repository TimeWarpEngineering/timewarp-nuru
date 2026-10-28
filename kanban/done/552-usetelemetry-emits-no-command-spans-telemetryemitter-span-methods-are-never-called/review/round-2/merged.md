# Round 2 — merged findings
**Date:** 2026-10-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs:213
- Description: Round 1: `UseTelemetry()` failed to compile a fluent route with a short-only flag because the behavior initializer emitted `Option = flag`. The same line also missed kebab long forms (`Target-name = targetname` for `--target-name {targetName?}`). The initializer now uses `CommandClassEmitter` property names and route-matcher local names. Endpoint flag `Supplied()` uses `LongForm ?? ShortForm`. telemetry-05 covers `tel05-build -v`, `tel05-docker run -i -t`, and `tel05-commit -m {message} --target-name {targetName?}`.
- Suggestion: none remaining.
- Source: general
- Disposition notes: fixed in `2f834e7d`. Re-checked on the fix delta; no new findings.

## Duplicates / conflicts

- One prior finding, carried forward. No new findings to merge.
