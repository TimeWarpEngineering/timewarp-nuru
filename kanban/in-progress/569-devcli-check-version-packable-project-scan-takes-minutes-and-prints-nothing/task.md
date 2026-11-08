# DevCli check-version: packable-project scan takes minutes and prints nothing

## Goal (implementer restatement)

`dev check-version` (and the shared pack/release scan) looks hung because
`GetPackableProjectsAsync` evaluates every `source/**/*.csproj` with its own
`dotnet msbuild -getProperty` call, one after another, and prints nothing until
that finishes. On Roslynk that is about two minutes for 24 projects.

Make that scan finish much faster without dropping or mis-reading a packable
project. Keep real MSBuild evaluation for every project (no path skips such as
TestFixtures, no XML parsing), keep the fail-loud guards (nonzero exit,
unparseable JSON, blank PackageId, duplicate PackageId), and keep
`ValidateDerivedSet` ordering deterministic. Say how many projects are being
evaluated before the scan starts, without changing the stdout that callers
parse. Update the Design note that claims ~0.4 s per project. Cover parallel
ordering and a named failure in tests. The change ships in a DevCli prerelease
after this branch merges; Roslynk bumping its DevCli pin is the follow-up.

## Description

Filed from the Roslynk cockpit, 2026-11-09. Steven reported "`dev check-version` just hangs, never
returns" in TimeWarpEngineering/Roslynk.

It does not hang. On Roslynk master (Version 1.0.0-beta.1, DevCli 4.0.0-beta.11) it completes in
**114 s** with exit 0 and the correct answer, but prints **nothing** until the end, so it looks hung.

Cause: `source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs`
`GetPackableProjectsAsync` enumerates every `*.csproj` under `source/` and runs
`dotnet msbuild <proj> -nologo -getProperty:IsPackable,PackageId` **sequentially**, one process per
project. The Design comment assumes ~0.4 s/project; observed ~4.5 s/project on WSL2. Roslynk has 24
csproj under `source/` (5 app projects + 19 test-fixture projects under `source/TestFixtures/`, which
carry their own `global.json` and are never packable). Only one project (`TimeWarp.Roslynk`) is packable.
`dev release` and `dev pack`/push paths share this service, so they pay the same cost.

## Requirements

- Make the scan fast. Candidates, pick with evidence (time each on Roslynk):
  - Evaluate projects in parallel with a bounded degree (e.g. `Environment.ProcessorCount`), keeping the
    deterministic `ValidateDerivedSet` ordering and every fail-loud guard (458-004 / 470 M23).
  - One MSBuild invocation for many projects (e.g. a generated traversal or `-getProperty` over a
    solution), if it gives the same per-project answers.
  - Do **not** skip projects by path heuristic (e.g. "TestFixtures") or parse XML — task 458-004 D1
    requires real MSBuild evaluation so no packable project is silently dropped.
- Show progress: say what it is doing before the scan (e.g. `Evaluating 24 projects for IsPackable…`)
  so the command never sits silent. Keep stdout stable for anything that parses it.
- Update the Purpose/Design region (the 0.4 s/project claim) to match reality.
- Tests: existing parse/validate unit tests still pass; add coverage for the parallel path's ordering and
  error propagation (a failing project still throws naming it).
- Ship in a TimeWarp.Nuru.DevCli prerelease; follow-up: Roslynk bumps DevCli.

## Checklist

- [x] Reproduce timing on Roslynk (`bin/dev check-version`, ~114 s baseline)
- [x] Faster evaluation, guards preserved
- [x] Progress line before the scan
- [x] Design comment updated
- [x] Tests
- [ ] Released in a DevCli beta; Roslynk follow-up noted

## Session

- Created: Roslynk cockpit 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-09)
- Implementation: 01a1bc87-a01f-7281-8e30-9a9e9b2f7c60 (2026-11-09). Chose bounded parallel `dotnet msbuild -getProperty` (degree = processor count, `-nodeReuse:false`). A generated traversal failed on Roslynk with MSB4057 and would miss bare projects that the blank-PackageId guard still evaluates. `<Version>` stays `4.0.0-beta.11`; this branch cannot `dev release` (release is from clean synced master after merge). Roslynk bumping its DevCli pin waits on that prerelease.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bc87-a01f-7281-8e30-9a9e9b2f7c60, max-turns 500, Done — 2026-11-08T19:38:10Z

## Results

`GetPackableProjectsAsync` still evaluates every `source/**/*.csproj` with real MSBuild (no path skip, no XML parse). The evaluations run together, bounded by `Environment.ProcessorCount`, and each result is stored at the project's enumerated index before `ValidateDerivedSet` sorts by PackageId. Finish order does not change the set or which path a duplicate PackageId names first. Non-cancellation failures are kept per index: one failure is rethrown with its original message (the project path); more than one become a single `InvalidOperationException` that names every failure. Progress is one stderr line, `Evaluating N projects for IsPackable…` (singular when N is 1), and only when N is at least 1. Stdout is unchanged.

Measured on Roslynk master (24 csproj under `source/`, one packable: `Morris.Roslynk.Cli.csproj` / `TimeWarp.Roslynk`):

| Run | Time | Result |
|-----|------|--------|
| Published `bin/dev check-version` (DevCli 4.0.0-beta.11) | 258 s | exit 0, correct version report, stderr empty, first stdout at exit |
| Sequential `-getProperty`, one process per project | 255 s | median 10.6 s, 0 failures |
| Parallel, `-nodeReuse:false` | 11 s | stdout and exit code match the sequential run for every project |
| Parallel, node reuse on | 11 s | same answers; reuse left off so the processes do not share one MSBuild node |
| One generated traversal | 10 s | exit 1, MSB4057 (`ReportPackableProperties` does not exist). Rejected |

New content, from the Roslynk checkout: `dotnet run <this-worktree>/tools/dev-cli/dev.cs -- check-version` exited 0 in 19 s including the `dotnet run` compile. Stderr at 6 s was `Evaluating 24 projects for IsPackable…`. Stdout at 18 s was the same version report (`1.0.0-beta.1`, `Packages checked: TimeWarp.Roslynk`, safe to release). The scan itself is about 11 s.

Tests: `packable-projects-01-parse` 21/21, `packable-projects-02-derivation-fixture` 4/4 (real `dotnet msbuild`, including a project that fails evaluation), `packable-projects-03-parallel` 8/8 (sort order, duplicate path order, one failure, two failures, unparseable output, blank PackageId, exclusion, empty input writes no progress). `dotnet build tests/devcli-consumer-check/devcli-consumer-check.csproj` succeeded with 0 warnings.

Not done here: publishing `TimeWarp.Nuru.DevCli`. The changelog entry is under Unreleased. After this merges and CI is green, `dev release` from master cuts the prerelease. Follow-up: Roslynk bumps its DevCli package pin and rebuilds `bin/dev`. Until that pin moves, Roslynk's installed `bin/dev` stays on the slow scan.

`ganda repo audit` also failed two checks that were already true on master. `vscode-settings-gitignore --fix` dropped the `!.vscode/settings.json` re-include and untracked that file (Peacock colors and cSpell words; the working copy stays). `source-file-line-cap` has no auto-fix and origin has no baseline, so `.timewarp/line-cap-baseline.json` records the 44 C# files already over 500 lines. That ratchet can only shrink after it lands. Splitting those files is later work. The new packable-project service and its tests are under the cap. Regenerating `internals-visible-to.g.cs` added `packable-projects-03-parallel` and the already-present `telemetry-06-extra-activity-sources` stem. The generator skips `devcli-consumer-check` so that project, which ProjectReferences TimeWarp.Nuru, does not receive InternalsVisibleTo (528, 562).

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-03-parallel.cs
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-02-derivation-fixture.cs
```

From a Roslynk checkout (the published `bin/dev` does not contain this change):

```bash
dotnet run /path/to/this-worktree/tools/dev-cli/dev.cs -- check-version
```

Expect: 21/21, 8/8, and 4/4 passed. The parallel run asserts PackageId order `A, M, Z` when Z finishes last, and a duplicate PackageId message that names `source/a/a.csproj` before `source/b/b.csproj` when `b` finishes first. Two failing projects produce one exception whose message contains both paths. On Roslynk, stderr shows `Evaluating 24 projects for IsPackable…` before any stdout, stdout still reports `TimeWarp.Roslynk` and `1.0.0-beta.1`, exit code is 0, and the scan is on the order of 11 s rather than about 250 s.

## Notes

- Related: 525 (feed lookup for check-version), 458-004 (MSBuild-derived packable set).
