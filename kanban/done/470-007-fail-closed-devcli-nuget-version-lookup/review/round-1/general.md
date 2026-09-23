# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** `git diff 41f2204e HEAD -- source tests` (nuget-version-service.cs, new props-version-reader.cs, check-version-command.cs, release-command.cs, check-version-05/06/07 tests, both Directory.Build.props edits), plus call sites (tools/dev-cli/dev.cs and Directory.Build.props, publish-state.cs ParsePackageList, dev-cli-json-context.cs, NuGetVersionIndex)

## Summary

The change makes the NuGet already-released gate fail closed. Only a 404 maps to "not published". Other non-success statuses throw `HttpRequestException`, and `check-version` and `release` catch it and exit 1 before any publish-state classification. Package ids are validated before any HTTP call, and the two copies of the props-version reader are replaced by one shared reader that trims the value. The core M9/M10/M31 fixes are correct. The response is disposed on all paths, the id grammar rules out path traversal, validation runs before the lookup in both endpoints, and the reader behaves the same as before except that it now trims. Overall risk is low. The remaining gap is at the edges of "malformed 200": the body `{}` still reads as "never published" because `NuGetVersionIndex.Versions` defaults to `[]`. Non-HTTP failures such as JSON errors and timeouts still fail closed, but only by crashing with an unhandled exception instead of printing the refusal message.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:152
- Description: The null check `index?.Versions ?? throw ...` only catches a literal `null` body. `NuGetVersionIndex.Versions` has the initializer `= []` (line 347), so a 200 with body `{}`, `{"items":[]}`, or any object without a `versions` property deserializes to an index with an empty list. It returns `[]` with no exception. In `check-version` (check-version-command.cs:144) this hits `continue`, and if it happens for every package the result is `PublishState.None` and "safe to release" with exit 0. In `release`, `IsVersionPublished` is false, so the gate passes. A proxy, CDN, or mirror that returns an empty JSON object with 200 can still clear the gate. That breaks the M9 contract that a malformed 200 fails closed. The Results wording ("a 200 whose body deserializes to no index object also throws") is literally true but narrower than the fail-closed claim.
- Suggestion: Treat a 200 with a missing or empty `versions` array as malformed. The flat container returns 404, not `{"versions":[]}`, for an id with no packages. Either throw when `index?.Versions is null or { Count: 0 }`, or remove the `= []` default and mark the property `[JsonRequired]`. Add a `{}` / `{"versions":[]}` case to check-version-05 next to `Null_body_on_success_throws`.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:124
- Description: Only `HttpRequestException` becomes the graceful "refusing to report it as safe" path (check-version-command.cs:133, release-command.cs:321). Two other failures escape both endpoint handlers as unhandled exceptions. The first is a non-JSON 200, such as a captive portal or proxy HTML page, which throws `JsonException` from `DeserializeAsync` (line 148). The second is an `HttpClient` timeout (100 s by default), which throws `TaskCanceledException` even though the caller's token was not cancelled. Nothing in the app catches exceptions (`tools/dev-cli/dev.cs` just returns `app.RunAsync(args)`), so the process crashes with a stack trace and a non-zero exit. It still fails closed, and "safe to release" is never printed. The operator just gets a stack trace instead of the documented `Error: NuGet lookup for '<pkg>' failed ... Retry when NuGet is reachable.` message.
- Suggestion: In `GetPackageVersionsAsync`, wrap `JsonException` in an `HttpRequestException` (or a dedicated lookup exception). Do the same for `TaskCanceledException`/`OperationCanceledException` when `!cancellationToken.IsCancellationRequested`. Then the two existing catch blocks cover every lookup failure the same way.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:303
- Description: The new release-side behavior (id validation at 303-309 and the fail-closed catch at 321-329) has no test. check-version-07 only drives `CheckVersionCommand.Handler`. The release block is a near copy of the check-version block (check-version-command.cs:102-108 and 126-142), so the two can drift. For example, the release branch could lose its catch in a later edit and every test would still pass. Reverting only the release-command.cs part of cb9c566d would leave all 05/06/07 tests green.
- Suggestion: Extract the shared "validate ids, then look up each package, throwing or returning a typed failure" loop into a small static helper in services/, for example next to `PublishStateClassifier`, and unit-test it with the stub handler. Both endpoints then call one tested code path. If the extraction feels out of scope, at least note in Results that release-side coverage relies on the code being structurally identical.
- Status: open

### Issue 4 — Severity: nit
- File: tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs:34
- Description: `Nuget_outage_fails_closed_instead_of_reporting_safe` and `Invalid_package_id_is_rejected_before_lookup` (line 65) assert `Environment.ExitCode.ShouldBe(1)` but never set it to 0 first. `Not_found_for_every_package_reports_safe` (line 97) does reset it. If an earlier test in the process leaves `ExitCode` at 1, the exit-code assertion in these two tests passes vacuously. The stderr assertions still catch a full revert, so this is polish only.
- Suggestion: Add `Environment.ExitCode = 0;` at the start of each `try`, matching line 97.
- Status: open
