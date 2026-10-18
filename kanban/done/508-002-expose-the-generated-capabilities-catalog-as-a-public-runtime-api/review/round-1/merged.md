# Round 1 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs:395
- Description: `DescribeReturnType` classifies the awaited type. For `OneOf<TSuccess, SharedProblemDetails>`, that is the `OneOf`, so `returnType` is kind `json` and the name is the `OneOf` CLR type. `EmitCommandResult` (`handler-invoker-emitter.cs` around the `ReturnsProblemPair` branch) classifies `SuccessTypeName` instead and writes that value; problem details go to stderr. The published descriptor does not match the CLI output strategy the task requires. `capabilities-08-runtime-catalog.cs` does not cover a problem-pair route. `T508ProblemQuery` (`IQuery<OneOf<string, SharedProblemDetails>>`) is an existing fixture that would show the mismatch.
- Suggestion: When `ReturnsProblemPair` is set, describe `SuccessTypeName` with `GetOutputStrategy` and strip `global::`. Keep non-problem `OneOf` on the awaited type. Assert string and complex success kinds in the runtime-catalog tests.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. No overlaps.
