# Remove Stale Scratch Files From Repo

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M25).

## Description

Stale scratch/one-off files are committed and orphaned (nothing references them):

- `optimization-results.md` (repo root) — one-off results dump
- `tests/test-status-report.md` — hand-maintained pass/fail table, guaranteed to rot
- `tests/temp-test-chained.cs` — bug-#295 repro scratch at tests/ root, outside any project
- `tests/timewarp-nuru-tests/generator/temp-iconfig-test.cs` — temp test (also referenced
  by committed `internals-visible-to.g.cs` files; regenerate those after removal via
  `scripts/generate-internals-visible-to.cs` — see 454-032)

## Checklist

- [x] Confirm each file is truly unreferenced (grep + git log)
- [x] Delete or relocate content worth keeping (e.g. into kanban/documentation)
- [x] Regenerate internals-visible-to.g.cs if temp tests removed
- [x] Build + CI tests still green

## Results

### What was implemented

Deleted 4 stale scratch/one-off files and cleaned up all references.

### Files deleted

- `optimization-results.md` (repo root) — one-off results dump, zero references in any .cs/.md/.json/.xml file
- `tests/test-status-report.md` — hand-maintained pass/fail table, zero references
- `tests/temp-test-chained.cs` — bug #295 repro scratch, NOT compiled by CI (lived at tests/ root, outside both CI glob directories)
- `tests/timewarp-nuru-tests/generator/temp-iconfig-test.cs` — scratch repro, explicitly excluded from CI via CiTestExcludes
- `source/timewarp-nuru/completion/internals-visible-to.g.cs` — stale nested generated file outside the generator's managed set, contained only obsolete test-assembly names; the root `internals-visible-to.g.cs` provides full coverage

### Files edited

- `tests/ci-tests/Directory.Build.props` — removed the stale `CiTestExcludes` entry + comment for `temp-iconfig-test.cs` (the file no longer exists, so the exclude was itself drift)
- `source/timewarp-nuru/internals-visible-to.g.cs` — regenerated, stale `temp-test-chained` and `temp-iconfig-test` entries removed
- `source/timewarp-nuru-parsing/internals-visible-to.g.cs` — regenerated
- `source/timewarp-nuru-mcp/internals-visible-to.g.cs` — regenerated

### Verification (per reviewer lesson: confirm tests are actually in CI, not just green)

- **Stale reference grep**: `rg "temp-test-chained|temp-iconfig-test|optimization-results|test-status-report"` across all .cs/.md/.json/.xml/.props/.targets/.csproj files (excluding kanban/) → **zero results**. All references eliminated.
- **CI count check**: Previous baseline was 1360 passed. After changes: **1360 passed, 7 skipped, 0 failed**. Count unchanged — confirms the deleted files were NOT contributing compiled tests to CI (temp-test-chained was outside the glob, temp-iconfig-test was in CiTestExcludes). No tests were lost.
- **No content worth keeping**: All 4 files were scratch/one-off with no reusable content (optimization results dump, stale status table, bug repro, IOptions scratch test).

### Key decisions made

- **Removed stale CiTestExcludes entry**: The exclude for `temp-iconfig-test.cs` was itself drift — the file no longer exists, so excluding it is unnecessary and leaving it would be a stale reference. Removed both the comment and the `<CiTestExcludes>` entry.
- **Removed redundant nested `.g.cs`**: `source/timewarp-nuru/completion/internals-visible-to.g.cs` was not managed by the generator (generator only writes to project roots), still contained the stale `temp-test-chained` entry, and held many other obsolete test-assembly names. The root `internals-visible-to.g.cs` covers `ci-tests` and all current standalone test files. Removed to eliminate a drift source.
- **Used the now-fixed generator**: The `generate-internals-visible-to.cs` generator was fixed in 454-024 (Directory.Exists guard for non-existent project directories). Confirmed it runs cleanly and regenerates all 3 project-root `.g.cs` files.

### Test outcomes

- **Full CI** (`dotnet run tests/ci-tests/run-ci-tests.cs`): 1360 passed, 7 skipped, 0 failed. Matches previous baseline — no regressions, no test loss.
