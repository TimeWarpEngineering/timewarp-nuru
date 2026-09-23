# Round 1 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:152
- Description: `index?.Versions ?? throw` only catches a literal `null` body. `NuGetVersionIndex.Versions` defaults to `[]`, so a 200 with body `{}` or `{"versions":[]}` returns an empty list, which callers read as "never published". A proxy/CDN returning an empty JSON object with 200 can still clear the already-released gate, contradicting the M9 fail-closed contract.
- Suggestion: Treat a 200 whose index has a null or empty `versions` array as malformed and throw. The flat container answers 404, not `{"versions":[]}`, for an unknown id. Add `{}` and `{"versions":[]}` cases to check-version-05.
- Source: general
- Disposition notes: Fixed by the review oracle on this task id. `GetPackageVersionsAsync` now throws `HttpRequestException` when the deserialized index is null or has zero versions. Tests `Empty_object_body_on_success_throws` and `Empty_versions_array_on_success_throws` added to check-version-05.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:124
- Description: Only `HttpRequestException` reaches the graceful "refusing to report it as safe" path in both endpoints. A non-JSON 200 (captive portal / proxy HTML) throws `JsonException`, and an `HttpClient` timeout throws `TaskCanceledException` with the caller's token not cancelled. Both escape as unhandled exceptions: still fail closed and non-zero, but with a stack trace instead of the documented error message.
- Suggestion: In `GetPackageVersionsAsync`, wrap `JsonException` and non-caller-cancelled `OperationCanceledException` in `HttpRequestException` so the two existing catch blocks cover every lookup failure.
- Source: general
- Disposition notes: Fixed by the review oracle. Deserialization is wrapped so `JsonException` becomes `HttpRequestException` (status 200 preserved); `OperationCanceledException` when `!cancellationToken.IsCancellationRequested` becomes `HttpRequestException` with `RequestTimeout`. Caller-initiated cancellation still propagates unchanged. Tests `Non_json_body_on_success_throws_http_request_exception`, `Client_timeout_throws_http_request_exception`, and `Caller_cancellation_propagates` added to check-version-05.

### M3 — Severity: suggestion — Status: wontfix
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:303
- Description: The release-side id validation (303-309) and fail-closed catch (321-329) have no test; check-version-07 only drives `CheckVersionCommand.Handler`. The two blocks are near copies and could drift. Reverting only the release-command.cs half of cb9c566d would leave every 05/06/07 test green.
- Suggestion: Extract the "validate ids, then look up each package or fail" loop into a static helper under services/ and unit-test it with the stub handler; or note in Results that release coverage relies on structural identity.
- Source: general
- Disposition notes: wontfix (decided by review oracle, Claude Fable 5.1, 2026-09-23). `ReleaseCommand.Handler` step 7 sits behind six git-state preconditions (clean tree, on master, synced with origin, tag absent, etc.) that cannot be satisfied from a unit test without a git fixture, and refactoring the release handler's loop is out of scope for a targeted gate fix. The release block now shares the same `NuGetVersionService` failure contract as check-version (all lookup failures surface as `HttpRequestException`), so the untested delta is the two-line catch plus one validation call. Recorded in task Results so a later release-command refactor task can pick up the extraction.

### M4 — Severity: nit — Status: fixed
- File: tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs:34
- Description: `Nuget_outage_fails_closed_instead_of_reporting_safe` and `Invalid_package_id_is_rejected_before_lookup` assert `Environment.ExitCode.ShouldBe(1)` without first resetting it to 0, so the assertion passes vacuously if a prior test left it at 1. The third test resets it.
- Suggestion: Add `Environment.ExitCode = 0;` at the start of each `try`.
- Source: general
- Disposition notes: Fixed by the review oracle; both tests now reset the exit code before running the handler.

## Duplicates / conflicts

- None; single reviewer.
