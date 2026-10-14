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

- [ ] Reproduce the 11.5 min multi-mode gap with `dotnet run tests/ci-tests/run-ci-tests.cs -v` (or `--verbosity detailed` /
      `-bl` binlog on the generated project) on the hosted runner: how much is restore, how much is Roslyn + the Nuru
      generator over 199 files, how much is analyzers. A `dotnet build` of the runfile's generated project with
      `/clp:PerformanceSummary` or a binlog will say.
- [ ] Measure the same thing locally for a baseline (and note the core count of the runner: `nproc`).
- [ ] Why is the runner silent for 11.5 min? Confirm it is compile, not a stuck restore or a hung generator.

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

- [ ] Investigation done; per-phase numbers in Results
- [ ] At least the two cheap wins shipped (NuGet cache, parallel samples/standalone tests) with measured deltas
- [ ] Decision recorded on job split / larger runner
- [ ] PR merged; master run under the target, run id in Results

## Notes

- Filed from the 502 wait (Amuru 2.0.0 pin bump PR), 2026-10-14. Run 38428046057 is the master run after PR #315.
- `TestCommand` streams the runner (`RunAsync`), so the 11.5 min silence is genuinely before the runner's first line,
  i.e. inside `dotnet run`'s build of the file-based program.

## Session

- Created: Claude Fable 5.1, session 31dfb8d1 (2026-10-14), from the nuru cockpit; unclaimed
