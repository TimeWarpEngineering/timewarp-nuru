# Investigate and cut nuru CI time (20+ min: 11.5 min multi-mode test compile, 5.4 min sequential sample builds)

## Description

A push to master or a PR takes 20–26 minutes of wall clock on `ubuntu-latest`. Every PR merge waits on it, and the
release pipeline promotes that same run's artifact, so this is the floor for shipping anything. Steven (2026-10-14):
"it takes 30 min for CI on nuru that is a bit crazy... all the samples rebuild".

The workflow is one job, one step: `dotnet run --file tools/dev-cli/dev.cs -- workflow`, which runs
`attestation -> clean -> build -> verify-samples -> test` (`tools/dev-cli/endpoints/workflow-command.cs`).

### Where the time goes (master run 38428046057, 2026-10-13, 20.4 min in "Run CI Pipeline")

| Phase | Wall clock | What it does |
|-------|-----------:|--------------|
| Step 1–3 attestation, clean, build | 0.5 min | 6 projects built sequentially via `bin/dev build`; each 1–10 s |
| Step 4 verify-samples | **5.4 min** | 64 runfile samples, `dotnet build` one at a time, ~4–5 s each (`verify-samples-command.cs:110-140`) |
| Step 5 test: multi-mode compile | **11.5 min** | `dotnet run tests/ci-tests/run-ci-tests.cs` is silent from "Working from:" (16:15:29) until its first `WriteLine` (16:26:57). That is restore + build of one file-based program that `<Compile Include>`s ~199 test files (`tests/ci-tests/Directory.Build.props`), with the Nuru source generator and analyzers running over all of them |
| Step 5 test: multi-mode execute | 0.5 min | 1877 tests, 16:26:57 → 16:27:26 |
| Step 5 test: standalone runs | 1.6 min | 28 excluded tests each run as a separate `dotnet run` (`run-ci-tests.cs:60`), ~3.5 s each, sequential |

Local full CI (`bin/dev build` + samples + `dotnet run tests/ci-tests/run-ci-tests.cs`) on the dev box is much faster,
so the hosted runner's core count and cold NuGet/compiler caches are part of it: the workflow has no `actions/cache`
for `~/.nuget/packages` or the runfile build cache, and no `setup-dotnet` cache input.

## Goal

Get a PR / master run under ~8 minutes without losing coverage, and record what each change bought.

## Investigation checklist

- [x] Reproduce the 11.5 min multi-mode gap with `dotnet run tests/ci-tests/run-ci-tests.cs -v` (or `--verbosity detailed` /
      `-bl` binlog on the generated project) on the hosted runner: how much is restore, how much is Roslyn + the Nuru
      generator over 199 files, how much is analyzers. A `dotnet build` of the runfile's generated project with
      `/clp:PerformanceSummary` or a binlog will say.
- [x] Measure the same thing locally for a baseline (and note the core count of the runner: `nproc`).
- [x] Why is the runner silent for 11.5 min? Confirm it is compile, not a stuck restore or a hung generator.

## Improvement candidates (measure before adopting; do not drop coverage)

- [ ] **Cache**: `actions/cache` for `~/.nuget/packages` keyed on `Directory.Packages.props` + `*.csproj`, and for the
      file-based-program build cache so unchanged samples/tests are no-ops on the runner.
- [ ] **Samples in parallel** (`verify-samples-command.cs`): build with `Parallel.ForEachAsync` / `Task.WhenAll` bounded
      to `Environment.ProcessorCount`, or one `dotnet build` of a synthetic `samples.sln`/`dirs.proj` so MSBuild
      parallelises and shares the restore. Or skip sample builds on PRs unless `samples/**` or `source/**` changed.
- [ ] **Standalone tests in parallel** (`run-ci-tests.cs:60`): 28 sequential `dotnet run` invocations; each restores and
      compiles its own program. Bound them with `Parallel.ForEachAsync`.
- [ ] **Split the job**: `build` once, upload the packages artifact, then `samples` and `tests` as parallel jobs that
      download it. Wall clock becomes max(phase) not sum(phase).
- [ ] **Multi-mode compile itself**: check whether analyzers run in that compile (they should be excluded for the
      test assembly: `RunAnalyzers=false` or `<Analyzer Remove>` for the test program), whether `TreatWarningsAsErrors`
      + `dotnet format` runs there, and whether `Deterministic`/`EmitCompilerGeneratedFiles` is adding IO.
- [ ] **Bigger runner** (`ubuntu-latest-4-core`/`8-core`) as a last resort; record cost vs minutes saved.

## Requirements

- Keep the single `workflow.yml` + `dev workflow` convention (kanban 458): the release pipeline promotes the CI
  artifact, so the artifact-producing job must stay deterministic and attested.
- Keep `ganda runfile cache --clear` semantics for local runs (stale generator cache gives false passes).
- Results section: before/after table per phase with run ids.

## Checklist

- [x] Investigation done; per-phase numbers in Results
- [x] At least the two cheap wins shipped (NuGet cache, parallel samples/standalone tests) with measured deltas
- [x] Decision recorded on job split / larger runner
- [ ] PR merged; master run under the target, run id in Results

## Notes

- Filed from the 502 wait (Amuru 2.0.0 pin bump PR), 2026-10-14. Run 38428046057 is the master run after PR #315.
- `TestCommand` streams the runner (`RunAsync`), so the 11.5 min silence is genuinely before the runner's first line,
  i.e. inside `dotnet run`'s build of the file-based program.

## Results

The 11.5 minute silence is `csc` on the multi-mode test compilation, not a stuck restore.
On a warm NuGet cache, `RestoreTask` was 123 ms. A 4-core pin (`taskset -c 0-3`, this machine
has `nproc` 32) of `dotnet build tests/ci-tests/run-ci-tests.cs -c Release --no-incremental
-clp:PerformanceSummary` put 437 s of 442 s on `tests/ci-tests/run-ci-tests.cs` itself.
`RunAnalyzersDuringBuild=false` and `EnforceCodeStyleInBuild=false` dropped that project to
83 s (wall 89 s). Source generators still ran: the build succeeded and the suite passed.

Sample builds are process startup, about 5–7 s each. Eight samples took 54.1 s one at a time
and 12.3 s at 4-wide. `dotnet build file.cs` rejects `-m`, so the fan-out is one process per
sample. Parallel builds raced on `artifacts/packages/*.nupkg` (`GeneratePackageOnBuild` on the
shared projects). The command builds those three projects once, then builds samples with
`--no-dependencies`.

### Before / after

| Phase | Before, master run [38428046057](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/38428046057) | After, this worktree (local) |
|-------|-----------:|-----------:|
| Attestation, clean, build | 0.5 min | unchanged logic; the step now prints its own duration |
| verify-samples | 5.4 min (64 sequential) | 30.8 s, 64/64, parallelism 32, warm outputs |
| Multi-mode compile | 11.5 min | 442 s analyzers on vs 89 s analyzers off (4-core pin, `--no-incremental`). Full `dotnet run` wall after the props change: 125 s on 32 cores |
| Multi-mode execute | 0.5 min (1877 tests) | 25.9 s. Total 1883, passed 1877, skipped 6, failed 0 |
| Standalone runs | 1.6 min (28 sequential) | 10.1 s, parallelism 32, 0 failed |

Hosted after numbers are the PR run. `workflow` prints `ProcessorCount` and a duration for
each step, and the workflow prints `nproc` before the pipeline. ubuntu-latest is unchanged.

Steady-state projection on a 4-core runner with a NuGet cache hit: samples about 1.5–2.5 min,
test compile about 2–3 min if the analyzer ratio holds, execute 0.5 min, standalone under 1 min,
build 0.5 min. That is under 8 min. A cache miss still pays a cold restore.

### Decisions

- **NuGet cache**: adopted. `actions/setup-dotnet` `cache: true`, keyed on
  `Directory.Packages.props`, `global.json`, `nuget.config`, and `**/*.csproj`.
- **Runfile directory cache**: not adopted. The SDK skips a rebuild by comparing source
  mtimes with `build-success.cache`. Checkout stamps sources at checkout time, so a restored
  directory either misses or can skip a real change. Local invalidation stays
  `ganda runfile cache --clear`.
- **Job split**: not adopted. Kanban 458 keeps one `workflow.yml` job whose `dev workflow`
  run is the attested artifact the release pipeline promotes.
- **Larger runner**: not adopted. The compile and sample wins are aimed at `ubuntu-latest`.
  Revisit only if that PR run is still over 8 minutes.
- **Test analyzers**: `RunAnalyzersDuringBuild`, `EnforceCodeStyleInBuild`, and
  `ReportAnalyzer` are off under `tests/`. `dev build` still analyzes product projects.
  Diagnostic coverage of the Nuru analyzer stays in the Roslyn-hosted standalone tests.

### How to validate

Smoke:

1. `dotnet run --file tools/dev-cli/dev.cs -- verify-samples`
2. `dotnet run tests/ci-tests/run-ci-tests.cs`
3. Read `.github/workflows/workflow.yml` for `cache: true` on `actions/setup-dotnet`.

Expect:

1. Exit 0. Log contains `Parallelism:` and `64/64 samples built successfully`.
2. Exit 0. Grand total failed 0 (last local run: total 1883, passed 1877, skipped 6).
   Log contains `Standalone tests: 28 files` and `Standalone finished in` with `0 failed`.
3. The setup-dotnet step sets `cache: true` and does not cache `~/.local/share/dotnet/runfile`.

### Review (round 1, 2026-10-14)

Effort 2 (by diff, 380 lines), roster `general` (review oracle, Claude Opus 5.5).
**Disposition: pending. Three findings are open and need the fix loop on this id, then a round-2 re-review.**

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

- **M1 (bug, open):** The parallel standalone `dotnet run`s each rebuild and repack the shared
  ProjectReferences. Locally, one standalone build printed "Successfully created package" for
  `TimeWarp.Nuru` and `TimeWarp.Nuru.Analyzers`. At `ProcessorCount`-wide, those processes race on
  `obj/Debug` and `artifacts/packages/*.nupkg`, the race `verify-samples` now avoids. The result can be a
  flaky merge gate or a torn nupkg in the master `Packages-*` artifact. Fix: build the standalone files
  without packing or rebuilding the shared projects.
- **M2 (suggestion, open):** The analyzer and code-style switches are at `tests/` scope. That drops test-code
  lint, RCS1037 included, for every single-file run, not only the multi-mode compile. Move them to
  `tests/ci-tests/Directory.Build.props`, or document that this is intended.
- **M3 (nit, open):** Print the failed-sample summary in sorted order.
- Out of scope: test-phase Debug builds overwrite the Release nupkg in `artifacts/packages/`. This was
  already true before this change. Check separately whether promoted artifacts contain Debug assemblies.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Session

- Created: Claude Fable 5.1, session 31dfb8d1 (2026-10-14), from the nuru cockpit; unclaimed
- Implementation: implementer-grok (2026-10-14)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-14T05:23:12Z
- Review oracle: round 1, Claude Opus 5.5 (headless `ganda task work`, session id not exposed), effort 2,
  roster general — 1 bug, 1 suggestion, 1 nit open; disposition pending fix loop (2026-10-14)
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-14T05:27:59Z
