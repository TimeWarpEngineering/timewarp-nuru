# Round 2 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs:403
- Description: `DescribeReturnType` classified the awaited type. For `OneOf<TSuccess, SharedProblemDetails>`, that is the `OneOf`, so `returnType` was kind `json` under the `OneOf` name. `EmitCommandResult` writes `SuccessTypeName` instead.
- Suggestion: When `ReturnsProblemPair` is set, describe `SuccessTypeName` with `GetOutputStrategy` and strip `global::`. Keep a non-problem `OneOf` on the awaited type. Assert string and complex success kinds.
- Source: general
- Disposition notes: Fixed on this id in `45fe5339`. `DescribeReturnType` uses `SuccessTypeName` when `ReturnsProblemPair` is set, then `GetOutputStrategy`, then strips `global::`. `Should_describe_problem_pair_endpoints_by_their_success_type` expects `string`/`primitive` and the `RtReport` record/`json`. Re-ran `capabilities-08-runtime-catalog.cs`: 12 passed, 0 failed.

## Resolved prior

- M1 fixed. No new findings.

## Duplicates / conflicts

- Single reviewer. No overlaps.
