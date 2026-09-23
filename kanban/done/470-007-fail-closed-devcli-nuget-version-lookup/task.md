# Fail-closed DevCli NuGet version lookup

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M9, M10). Suggestion folded: M31.

## Description

`GetPackageVersionsAsync` (`nuget-version-service.cs:43-46`) returns `[]` for every non-success HTTP status. NuGet 404 for a never-published id is correctly empty, but 429/5xx/auth are indistinguishable. Callers treat empty as “not published”: check-version `continue`s (`check-version-command.cs:117-120`) so `alreadyPublished` stays empty → `PublishState.None` → “safe to release”. A transient NuGet outage can clear the already-released gate.

M10: `GetVersionFromSource` (`check-version-command.cs:202-203`) does not trim `<Version>`. `release-command.cs:464-465` and workflow `ReadPropsVersion` do. Whitespace in the element makes check-version miss a published version.

M31: package id is interpolated unescaped (`nuget-version-service.cs:40`); `../evil` normalizes off `v3-flatcontainer`. Host stays `api.nuget.org`. Reachable from `--package`.

## Requirements

- Treat only 404 (maybe 400) as empty; other statuses fail-closed. Dispose HttpResponseMessage.
- Trim Version in check-version (or share one props-version reader).
- Validate NuGet id grammar and/or Uri.EscapeDataString the path segment (M31).
- Tests for non-404 HTTP and whitespace Version.

## Checklist

- [x] Fail-closed HTTP (M9)
- [x] Trim Version (M10)
- [x] Package id validation (M31)
- [x] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M9, M10, M31. 458 policy is out of scope; this is code correctness of the gate.

## Results

`NuGetVersionService.GetPackageVersionsAsync` now maps only HTTP 404 to an empty list. Every other non-success status throws `HttpRequestException` carrying the status code. A 200 whose body is not JSON, deserializes to no index object, or carries a missing/empty `versions` array also throws `HttpRequestException` (review round 1, M1/M2), and an `HttpClient` timeout is wrapped the same way while caller cancellation still propagates. The `HttpResponseMessage` is disposed. `check-version` and `release` catch `HttpRequestException`, print `Error: NuGet lookup for '<pkg>' failed: ...` plus a refusing-to-report-safe line, set exit code 1, and return before the publish-state classification, so a NuGet outage can no longer clear the already-released gate (M9).

Package ids are validated with `NuGetVersionService.IsValidPackageId` (ASCII letters, digits, underscore, single `.`/`-` separators, no leading/trailing/consecutive separators, max 100 chars, mirroring NuGet's `PackageIdValidator`). `GetIndexUrl` throws `ArgumentException` for an invalid id and escapes the lowercased path segment. Both endpoints reject invalid `--package`/config ids up front with `Error: invalid NuGet package id(s): ...` and exit code 1, before any HTTP call (M31).

A shared `PropsVersionReader.Read(repoRoot)` replaces the two private `GetVersionFromSource` copies in `check-version` and `release`. It trims the `<Version>` element and returns null for a blank element, so the existing "could not read Version" error fires instead of comparing whitespace against NuGet (M10). The repo tool's `workflow-command.cs` `ReadPropsVersion` already trims and was left as is (it is not part of the packaged DevCli content).

The test seam is an `internal NuGetVersionService(HttpMessageHandler)` constructor. It is internal on purpose: the Nuru DI generator resolves the public constructor with the most parameters, and a public overload triggered NURU051 in `tools/dev-cli`. Test runfiles compile the service file into their own assembly, so internal is visible there.

### Files changed

- `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs`
- `source/timewarp-nuru-devcli/content/any/services/props-version-reader.cs` (new)
- `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs`
- `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`
- `tests/timewarp-nuru-tests/devcli/Directory.Build.props` and `tests/ci-tests/Directory.Build.props` (compile the new reader)
- `tests/timewarp-nuru-tests/devcli/check-version-05-fail-closed-lookup.cs` (new)
- `tests/timewarp-nuru-tests/devcli/check-version-06-props-version-reader.cs` (new)
- `tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs` (new, standalone only)

### Test outcomes

- `check-version-05-fail-closed-lookup.cs`: 15 passed (10 original + 5 from review round 1: `{}` body, empty versions array, HTML body, client timeout, caller cancellation)
- `check-version-06-props-version-reader.cs`: 7 passed
- `check-version-07-endpoint-fail-closed.cs`: 3 passed
- Existing `check-version-01` (14), `check-version-03` (15), `check-version-04` (1): all passed
- `dotnet build tools/dev-cli/dev.cs`: succeeded
- Live smoke: `dev check-version --package TimeWarp.Nuru` reports 3.0.0-beta.77 new, latest 3.0.0-beta.76, exit 0; `--package ../evil` prints the invalid-id error, exit 1
- `dotnet run tests/ci-tests/run-ci-tests.cs` after `ganda runfile cache --clear` (2026-09-23, post-review-fix `8cd05197`): exit 0, multi-mode grand total 1672 (1665 passed, 7 skipped, 0 failed); `FailClosedLookup` (15) and `PropsVersionReader` (7) present in the grand total

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-470-007-fail-closed-devcli-nuget-version-lookup
dotnet run tests/timewarp-nuru-tests/devcli/check-version-05-fail-closed-lookup.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-06-props-version-reader.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs
dotnet run tools/dev-cli/dev.cs -- check-version --package "../evil"; echo "exit=$?"
```

**Expect**

- 05: 15 passed. `Empty_object_body_on_success_throws` and `Non_json_body_on_success_throws_http_request_exception` get `HttpRequestException` with `StatusCode == OK`; `Client_timeout_throws_http_request_exception` gets `RequestTimeout`; `Service_unavailable_throws_with_status` gets `HttpRequestException` with `StatusCode == ServiceUnavailable`; `Not_found_yields_empty_list` returns an empty list; `Invalid_id_throws_before_request` throws `ArgumentException` with zero requests sent; request URL is `https://api.nuget.org/v3-flatcontainer/timewarp.nuru/index.json`.
- 06: 7 passed. `<Version>\n  1.2.3-beta.4\n</Version>` reads as `1.2.3-beta.4`; blank element reads as null.
- 07: 3 passed. With a stub returning 503, the handler writes `NuGet lookup for 'TimeWarp.Nuru' failed` and `refusing to report it as safe to release` to stderr, never prints `safe to release`, exit code 1. `../evil` is rejected with zero requests. 404 for every package still prints `safe to release` with exit code 0.
- `check-version --package "../evil"` prints `Error: invalid NuGet package id(s): ../evil` and exits 1.

**Automated gate**

```bash
ganda runfile cache --clear
dotnet run tests/ci-tests/run-ci-tests.cs
# expect: exit 0, 0 failed; multi-mode includes check-version-05 (15) and check-version-06 (7); check-version-07 is standalone only
```

### Review disposition

- Rounds: 2. Roster: general (effort 1). Reviewer: Claude sub-agent (opus) spawned by the review oracle (Claude Fable 5.1, ganda task work).
- Final counts: bug 1 fixed / suggestion 1 fixed + 1 wontfix / nit 1 fixed; 0 open.
- Disposition: **accepted-exceptions**. M3 (suggestion) wontfix: no release-side endpoint test for the id validation and fail-closed catch in `release-command.cs`, because release step 7 sits behind git-state preconditions that need a git fixture and extracting the shared lookup loop is out of scope. The service-level failure contract both endpoints depend on is tested directly. Candidate follow-up when release-command is next refactored.
- Fix commit: `8cd05197` (M1 bug: `{}` / empty `versions` 200 now throws; M2: JsonException and client timeout wrapped as `HttpRequestException`; M4: exit-code reset in check-version-07 tests).
- Artifacts: `review/review-framework.md`, `review/round-1/{general,merged}.md`, `review/round-2/{general,merged}.md`, `review/disposition.md`.

## Session

- 2026-09-23: implementer (ganda task work, oracle implement). Product fix `cb9c566d`, tests `ea8c2aa6`. Full CI green. Kanban folderize committed on the task branch. Next host nodes: review, open-pr.
- 2026-09-23: `7c6be22e` sets the executable bit on the three new test runfiles (flagged by `ganda repo audit` runfile-executable). Remaining audit failures are pre-existing on master and out of scope here: `bin/dev` missing, global-usings-analyzer pins, five older test runfiles without exec bit, `documentation/developer/design/dsl/fluent-api-example.cs` shebang, and the gitignored `oracle-*.log` kebab-path hit beside this kitchen.
- 2026-09-23: review oracle (ganda task work, Claude Fable 5.1). Round 1 general review raised M1 bug (empty `{}` 200 cleared the gate), M2/M3 suggestions, M4 nit. Fixed M1/M2/M4 in `8cd05197`; M3 wontfix with rationale. Round 2 re-review clean, disposition accepted-exceptions. Post-fix CI rerun green (1665 passed, 0 failed). Review artifacts committed. Next host nodes: open-pr.
