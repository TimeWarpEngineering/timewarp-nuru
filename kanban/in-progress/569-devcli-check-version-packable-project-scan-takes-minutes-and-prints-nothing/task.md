# DevCli check-version: packable-project scan takes minutes and prints nothing

## Goal (implementer restatement)

The packable-project scan shared by `dev check-version`, pack, and release
evaluates every `source/**/*.csproj` with real MSBuild. It has to finish in
seconds, and it has to keep every project: no path skips, no XML parsing. A
nonzero exit, unparseable JSON, a blank PackageId, or a duplicate PackageId
still fails the command and names the project. Finish order does not change
the PackageId sort or which path a duplicate names first.

Before the scan starts, the command says how many projects it is evaluating.
That line is not part of the stdout callers parse. It is written on the
`ITerminal` the app already injects, so a test terminal can read it.

Round 1 is part of done for this pass. The parse fixtures name their constants
`Stdout` and do not silence IDE1006. `PackableProjectService` takes that
`ITerminal` and reports progress through it. When more than one project fails
evaluation, the `InvalidOperationException` carries those exceptions in an
inner `AggregateException`. The DevCli prerelease still waits until this
branch merges; Roslynk bumping its pin is the follow-up.

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
- [x] Review M1: parse constants named `Stdout`; IDE1006 pragma removed
- [x] Review M2: progress line written on the injected `ITerminal`
- [x] Review M3: multi-failure throw keeps an inner `AggregateException`
- [ ] Released in a DevCli beta; Roslynk follow-up noted

## Session

- Created: Roslynk cockpit 93b68595-4648-49ee-a58b-6a1ec1f5dab9 (2026-11-09)
- Implementation: 01a1bc87-a01f-7281-8e30-9a9e9b2f7c60 (2026-11-09). Chose bounded parallel `dotnet msbuild -getProperty` (degree = processor count, `-nodeReuse:false`). A generated traversal failed on Roslynk with MSB4057 and would miss bare projects that the blank-PackageId guard still evaluates. `<Version>` stays `4.0.0-beta.11`; this branch cannot `dev release` (release is from clean synced master after merge). Roslynk bumping its DevCli pin waits on that prerelease.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1bc87-a01f-7281-8e30-9a9e9b2f7c60, max-turns 500, Done — 2026-11-08T19:38:10Z
- Review oracle round 1: claude (claude-opus-5-5), effort 2, roster general — 2026-11-09. 3 open findings (M1 bug, M2 suggestion, M3 nit); fix pass requested. Artifacts under `review/round-1/`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 5854f69d-3a90-4f21-bd14-8706afdf17d7, max-turns 120, artifacts general.md, merged.md — 2026-11-08T19:41:30Z
- Fix pass (implement-fix-2): 2026-11-09. M1 names the parse constants `Stdout` and drops the file-wide IDE1006 pragma. M2 gives `PackableProjectService` an `ITerminal`; generated DI passes `__fw_ITerminal`, and the derivation fixture reads the progress line on `TestTerminal`. M3 keeps the original exceptions on the multi-failure `InvalidOperationException` as an inner `AggregateException`.

## Results

`GetPackableProjectsAsync` still evaluates every `source/**/*.csproj` with real MSBuild (no path skip, no XML parse). The evaluations run together, bounded by `Environment.ProcessorCount`, and each result is stored at the project's enumerated index before `ValidateDerivedSet` sorts by PackageId. Finish order does not change the set or which path a duplicate PackageId names first. Non-cancellation failures are kept per index: one failure is rethrown unchanged; more than one become a single `InvalidOperationException` that names every failure and whose inner exception is an `AggregateException` of the originals. Progress is one line on the injected `ITerminal`, `Evaluating N projects for IsPackable…` (singular when N is 1), and only when N is at least 1. In the app that terminal writes stderr. Stdout is unchanged.

Measured on Roslynk master (24 csproj under `source/`, one packable: `Morris.Roslynk.Cli.csproj` / `TimeWarp.Roslynk`):

| Run | Time | Result |
|-----|------|--------|
| Published `bin/dev check-version` (DevCli 4.0.0-beta.11) | 258 s | exit 0, correct version report, stderr empty, first stdout at exit |
| Sequential `-getProperty`, one process per project | 255 s | median 10.6 s, 0 failures |
| Parallel, `-nodeReuse:false` | 11 s | stdout and exit code match the sequential run for every project |
| Parallel, node reuse on | 11 s | same answers; reuse left off so the processes do not share one MSBuild node |
| One generated traversal | 10 s | exit 1, MSB4057 (`ReportPackableProperties` does not exist). Rejected |

New content, from the Roslynk checkout: `dotnet run <this-worktree>/tools/dev-cli/dev.cs -- check-version` exited 0 in 19 s including the `dotnet run` compile. Stderr at 6 s was `Evaluating 24 projects for IsPackable…`. Stdout at 18 s was the same version report (`1.0.0-beta.1`, `Packages checked: TimeWarp.Roslynk`, safe to release). The scan itself is about 11 s.

Tests: `packable-projects-01-parse` 21/21 (constants are `Stdout`; the IDE1006 pragma is gone and the standalone runfile still compiles with warnings as errors), `packable-projects-02-derivation-fixture` 4/4 (real `dotnet msbuild`; the progress line is on `TestTerminal`: 3 projects, 1 project, and no line when `source/` is absent), `packable-projects-03-parallel` 8/8 (sort order, duplicate path order, one failure, two failures whose inner exception is an `AggregateException` naming both paths, unparseable output, blank PackageId, exclusion, empty input writes no progress). `check-version-04` 1/1 and `check-version-07` 11/11 construct `PackableProjectService` with the handler's `TestTerminal`. `dotnet build tests/devcli-consumer-check/devcli-consumer-check.csproj` succeeded with 0 warnings. The generated resolver is `new PackableProjectService(__fw_ITerminal)`. `ganda repo audit` passed (38 passed, 0 failed, 1 skipped). This pass did not re-time Roslynk.

Not done here: publishing `TimeWarp.Nuru.DevCli`. The changelog entry is under Unreleased. After this merges and CI is green, `dev release` from master cuts the prerelease. Follow-up: Roslynk bumps its DevCli package pin and rebuilds `bin/dev`. Until that pin moves, Roslynk's installed `bin/dev` stays on the slow scan.

`ganda repo audit` also failed two checks that were already true on master. `vscode-settings-gitignore --fix` dropped the `!.vscode/settings.json` re-include and untracked that file (Peacock colors and cSpell words; the working copy stays). `source-file-line-cap` has no auto-fix and origin has no baseline, so `.timewarp/line-cap-baseline.json` records the 44 C# files already over 500 lines. That ratchet can only shrink after it lands. Splitting those files is later work. The new packable-project service and its tests are under the cap. Regenerating `internals-visible-to.g.cs` added `packable-projects-03-parallel` and the already-present `telemetry-06-extra-activity-sources` stem. The generator skips `devcli-consumer-check` so that project, which ProjectReferences TimeWarp.Nuru, does not receive InternalsVisibleTo (528, 562).

### Review

Round 1 (effort 2, roster: general) reviewed 93650965 vs master. The implementation matches the
task and the implementer restatement (no misread). Re-ran `packable-projects-01-parse` 21/21,
`-02-derivation-fixture` 4/4, `-03-parallel` 8/8. Counts: bug 1 open, suggestion 1 open, nit 1 open.
The fix pass implemented M1 (parse constants renamed to `Stdout`, IDE1006 pragma removed),
M2 (progress goes through the injected `ITerminal`), and M3 (inner `AggregateException`).
Round 2 has not reviewed that diff yet.
Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-03-parallel.cs
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-02-derivation-fixture.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-07-endpoint-fail-closed.cs
dotnet build tests/devcli-consumer-check/devcli-consumer-check.csproj
```

From a Roslynk checkout (the published `bin/dev` does not contain this change):

```bash
dotnet run /path/to/this-worktree/tools/dev-cli/dev.cs -- check-version
```

Expect: 21/21, 8/8, 4/4, 1/1, and 11/11 passed, and the consumer build reports 0 warnings. The parse file has no IDE1006 pragma. The parallel run asserts PackageId order `A, M, Z` when Z finishes last, a duplicate PackageId message that names `source/a/a.csproj` before `source/b/b.csproj` when `b` finishes first, and two failing projects produce one exception whose message contains both paths and whose inner exception is an `AggregateException` containing both. The fixture reads `Evaluating 3 projects for IsPackable…` and `Evaluating 1 project for IsPackable…` on the test terminal, and writes no progress line when `source/` is absent. On Roslynk, stderr shows `Evaluating 24 projects for IsPackable…` before any stdout, stdout still reports `TimeWarp.Roslynk` and `1.0.0-beta.1`, exit code is 0, and the scan is on the order of 11 s rather than about 250 s.

## Review

- Round 1: open M1, M2, M3 — implemented in the fix pass; round 2 has not reviewed that diff

## Notes

- Related: 525 (feed lookup for check-version), 458-004 (MSBuild-derived packable set).
