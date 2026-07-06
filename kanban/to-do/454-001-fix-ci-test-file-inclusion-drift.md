# Fix CI Test File Inclusion Drift

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`tests/ci-tests/Directory.Build.props` enumerates test files by hand, and later-added
tests were never wired in. Committed, substantial test files that NEVER compile or run
in CI:

- `tests/timewarp-nuru-tests/auto/` — entire dir (e.g. `endpoint-nullable-option-01.cs`)
- `tests/timewarp-nuru-tests/group-options/` — entire dir (492-line `group-options-01-basic.cs`)
- `tests/timewarp-nuru-tests/generator/generator-{16,17,18,19,20-issue184,20-parameterized,21,22,24,25}.cs` + `temp-iconfig-test.cs`
- `tests/timewarp-nuru-tests/completion/completion-27-endpoint-protocol.cs`
- `tests/timewarp-nuru-tests/repl/repl-38-auto-start-when-empty.cs`, `repl-39-no-duplicate-options.cs`
- ALL 6 `tests/timewarp-nuru-mcp-tests/mcp-0*.cs` (glob commented out at line ~116) — MCP server has ZERO CI coverage

Impact: group filtering, nullable options, duplicate-option suppression, endpoint-protocol
completion, and the entire MCP server can regress with green CI. Prior report
`.agent/workspace/2026-01-17T00-00-00_ci-excluded-tests-report.md` flagged the same
pattern; drift has continued since.

## Requirements

- Replace the hand-maintained include list with directory globs plus an explicit,
  commented exclusion list — OR add a CI check that fails when a tracked test file is
  not referenced by any glob.
- Wire in the excluded tests above; fix or explicitly exclude (with reason) any that fail.
- Remember CI multi-mode constraints: tests must use `.Map<TEndpoint>()` instead of
  `.DiscoverEndpoints()` to avoid endpoint cross-contamination.

## Checklist

- [ ] Inventory all tracked test files vs files matched by CI globs
- [ ] Rework Directory.Build.props inclusion strategy (globs + exclusions)
- [ ] Wire in / fix excluded tests (auto, group-options, generator, completion-27, repl-38/39)
- [ ] Re-enable MCP test glob or document why excluded
- [ ] Add drift guard so new tests can't be silently skipped
- [ ] Run `ganda runfile cache --clear` then full `dotnet run tests/ci-tests/run-ci-tests.cs`

## Notes

Tests that pass individually but fail in CI multi-mode are usually stale runfile cache or
DiscoverEndpoints cross-contamination (see .agent/local/nuru-specific.md).
