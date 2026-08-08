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

- [x] Inventory all tracked test files vs files matched by CI globs
- [x] Rework Directory.Build.props inclusion strategy (globs + exclusions)
- [x] Wire in / fix excluded tests (auto, group-options, generator, completion-27, repl-38/39)
- [x] Re-enable MCP test glob or document why excluded
- [x] Add drift guard so new tests can't be silently skipped (recursive glob = auto-include;
      exclusions are explicit `CiTestExcludes` items with reason comments)
- [x] Run `ganda runfile cache --clear` then full `dotnet run tests/ci-tests/run-ci-tests.cs`

## Results

CI suite grew from ~1000 to **1271 tests, all green** (1264 passed, 0 failed, 7 skipped —
skips pre-existing).

Directory.Build.props now uses `../timewarp-nuru-tests/**/*.cs` + `../timewarp-nuru-mcp-tests/*.cs`
recursive globs; new test files are auto-included so coverage can't silently drift.
Documented `CiTestExcludes`:
- `generator-17-local-function-config.cs` — tests local functions in top-level statements;
  cannot exist in the multi-mode assembly. Verified passing standalone.
- `temp-iconfig-test.cs` — scratch repro, deletion tracked in 454-025.
- `completion/engine/engine-01-input-tokenizer.cs` — references ParsedInput/InputTokenizer,
  deleted from source in refactor #360; does not compile standalone either. Delete or rewrite.
- `mcp-02-syntax-documentation.cs` — asserts MCP:endpoint-* syntax regions that exist
  nowhere (MCP product bug). Tracked in 454-033.

Fixes made to wire tests in:
- generator-20-parameterized/21/24: moved global-namespace service types into unique
  per-file namespaces (they collided with each other and generator-04 in multi-mode).
- generator-20's Gen20KanbanQuery endpoint + its test gated `#if !JARIBU_MULTI`: a
  [NuruRoute] endpoint requiring a service poisons every other test app using unfiltered
  DiscoverEndpoints() (50× NURU050). Runs standalone (verified, 5/5 pass).
- generator-19 NoFilter_IncludesAll gated `#if !JARIBU_MULTI`: unfiltered discovery is
  inherently global in multi-mode. Runs standalone (verified, 7/7 pass).
- mcp-01: updated stale assertions to current samples/examples.json IDs; repointed two
  tests away from dead manifest entries (drift bug → 454-033).

Discovered product bugs filed as **454-033**: examples.json manifest has 28 dead paths;
GetSyntaxTool's endpoint regions are unresolvable (only the fluent file is embedded).

## Notes

Tests that pass individually but fail in CI multi-mode are usually stale runfile cache or
DiscoverEndpoints cross-contamination (see .agent/local/nuru-specific.md).

## Session

- Created: 2026-07-06 (full-repo review session)
- Implementation: 2026-07-06 (same session)
