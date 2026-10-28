# Disposition — task 552

**Date:** 2026-10-28
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 found M1: `UseTelemetry()` did not compile a fluent route with a short-only flag, because the behavior command initializer did not use the command-class property name or the route-matcher local. The fix pass aligned those names, including kebab long forms, and added telemetry-05 coverage. Round 2 re-checked that delta and found no new defects. Every raised finding is fixed.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|

## Escalations

- None.
