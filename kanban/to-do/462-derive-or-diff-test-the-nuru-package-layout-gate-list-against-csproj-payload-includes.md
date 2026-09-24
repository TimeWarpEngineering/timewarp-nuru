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

- [ ] Diff test csproj pack items vs gate list
- [ ] Analyzer payload added to the gate
- [ ] Test runs on CI
- [ ] 461 fail-closed semantics intact

## Notes

- Origin: kanban 461 review round 1 (2026-08-10), non-blocking suggestion.
- Implementer: **commit and push your product changes before reporting done.** Recent Cursor runs
  left edits uncommitted and the PR carried only the kanban move.
- Run the test gate in the foreground. You are one-shot and never receive background notifications.
