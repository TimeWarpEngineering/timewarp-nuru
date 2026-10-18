# Round 2 — general
**Date:** 2026-10-19
**Scope reviewed:** fix delta `45fe5339` (parent `b70770fd`). `DescribeReturnType`, `Should_describe_problem_pair_endpoints_by_their_success_type`, and the problem-pair paragraph in `agent-invocation.md`.

## Summary

`DescribeReturnType` now classifies `SuccessTypeName` when `ReturnsProblemPair` is set, then strips `global::` and maps that name through `GetOutputStrategy`. That is the same string `EmitCommandResult` passes to the output strategy after it unwraps `OneOf<TSuccess, SharedProblemDetails>` (`result.AsT0`). A non-problem `OneOf` still uses the awaited type. `dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-08-runtime-catalog.cs` passed 12/12, including `rt-problem-text` as `string`/`primitive` and `rt-problem-report` as the `RtReport` record/`json`. No new findings.

`ReturnTypeCapability.Name` still says an async handler's name is the awaited type. `agent-invocation.md` states the problem-pair exception. That comment was not raised.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs:403
- Description: Round 1: `DescribeReturnType` classified the awaited `OneOf`, so a problem-pair endpoint was published as `json` under the `OneOf` name while the invoker writes `TSuccess`.
- Suggestion: Describe `SuccessTypeName` with `GetOutputStrategy`, strip `global::`, and assert string and complex success kinds.
- Status: fixed

## Issues

No new findings.
