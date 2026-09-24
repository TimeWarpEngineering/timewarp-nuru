# Derive or diff-test the Nuru package layout gate list against csproj payload includes

## Description

Follow-up from 461 review (verdict clean, one suggestion). The required-payload list for the
TimeWarp.Nuru nupkg is hand-duplicated in two places, synced only by a comment:

- `source/timewarp-nuru/timewarp-nuru.csproj` — explicit `None Include … Pack="true"` entries
- `tools/dev-cli/endpoints/workflow-command.cs` — `NuruRequiredPackageEntries` (layout gate)

If a future timewarp-nuru-build dependency is added to one list but not the other, the gate
silently stops covering the new file (`NupkgLayoutCheck.FindMissing` only checks its own list).

Re-checked 2026-09-24: both lists name the same twelve `build/` entries today, and no test
compares them.

Also in scope: the `analyzers/dotnet/cs` payload (`TimeWarp.Nuru.Analyzers.dll`,
`Microsoft.Extensions.Logging.Abstractions.dll`, `ICSharpCode.Decompiler.dll`) is packed by the
csproj but not gate-checked at all. A missing analyzer dll would ship unnoticed.

## Requirements

- Add a test that parses `timewarp-nuru.csproj`, collects every `None` item with `Pack="true"` and a
  `PackagePath` under `build/` or `analyzers/`, resolves the published entry name (PackagePath plus
  file name, or PackagePath itself when it names a file), and diffs that set against
  `NuruRequiredPackageEntries`. Fail on any mismatch in either direction, naming the entries.
- Add the three `analyzers/dotnet/cs/*.dll` entries to `NuruRequiredPackageEntries`.
- Keep the gate list a static array in the dev CLI. Do not add csproj/msbuild parsing to the release
  path. Keep the 461 fail-closed semantics.
- The test runs on the CI path (multi-mode or listed in `standaloneTests` in
  `tests/ci-tests/run-ci-tests.cs`). A test that CI never runs does not count.
- Update the "update BOTH" comment to point at the new test.

## Checklist

- [x] Diff test csproj pack items vs gate list
- [x] Analyzer payload added to the gate
- [x] Test runs on CI
- [x] 461 fail-closed semantics intact

## Notes

- Origin: kanban 461 review round 1 (2026-08-10), non-blocking suggestion.
- Implementer: **commit and push your product changes before reporting done.** Recent Cursor runs
  left edits uncommitted and the PR carried only the kanban move.
- Run the test gate in the foreground. You are one-shot and never receive background notifications.

## Results

Moved `NuruRequiredPackageEntries` to `NupkgLayoutCheck` as a public static array (still
hand-maintained; no csproj parsing on the release path). Added the three
`analyzers/dotnet/cs/*.dll` entries. `dev workflow` still calls
`NupkgLayoutCheck.FindMissing` with that array (461 fail-closed unchanged).

New multi-mode test `nupkg-layout-02-csproj-gate-parity` parses `timewarp-nuru.csproj`
Pack=`true` None items under `build/` and `analyzers/`, resolves published entry names, and
fails both ways on drift vs the gate list (lib/ stays gate-only — assembly packing, not a
None Pack item). Csproj comments now point at this test.

### Files changed

- `source/timewarp-nuru-devcli/content/any/services/nupkg-layout-check.cs` — gate list +
  `ResolvePublishedEntry`
- `tools/dev-cli/endpoints/workflow-command.cs` — use shared list
- `source/timewarp-nuru/timewarp-nuru.csproj` — update-BOTH comments
- `tests/timewarp-nuru-tests/devcli/nupkg-layout-02-csproj-gate-parity.cs` — new
- `source/timewarp-nuru/internals-visible-to.g.cs` (and siblings) — regenerated

### Test outcomes

- `nupkg-layout-01-check.cs`: 4 passed
- `nupkg-layout-02-csproj-gate-parity.cs`: 3 passed
- `dotnet build tools/dev-cli/dev.cs`: succeeded
- `dotnet run tests/ci-tests/run-ci-tests.cs` after `ganda runfile cache --clear`: exit 0;
  multi-mode includes `NupkgLayoutCsprojGateParity` (3 passed); grand total Passed 1717

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-462-derive-or-diff-test-the-nuru-package-layout-gate-l
dotnet run tests/timewarp-nuru-tests/devcli/nupkg-layout-02-csproj-gate-parity.cs
dotnet run tests/timewarp-nuru-tests/devcli/nupkg-layout-01-check.cs
```

**Expect**

- Parity file: 3 passed (`Csproj_pack_items_match_gate_list_under_build_and_analyzers` plus two
  `ResolvePublishedEntry` cases)
- Layout check file: 4 passed (461 fail-closed matrix unchanged)
- Removing one analyzer line from `NuruRequiredPackageEntries` (or adding a Pack item without
  updating the gate) makes the parity test fail naming the drifted entry

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Final counts:** bug/suggestion/nit all 0 open, 0 fixed, 0 wontfix
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified smoke: nupkg-layout-01 4 passed, nupkg-layout-02 3 passed

## Session

- 2026-09-24: implementer (ganda task work). Gate list + analyzer entries + parity test; CI multi-mode; pushed product commits.
- 2026-09-24: review oracle (ganda task work, tw-implementation-review effort 1). Round 1 general — disposition clean. Next host nodes: open-pr (no apply-review sibling).
