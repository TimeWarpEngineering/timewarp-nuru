# Collect CI code coverage through Jaribu runfile coverage mode

## Description

`ganda repo audit` warns `ci-code-coverage`: CI runs tests but collects no coverage. Nuru runs
tests in runfile mode (`dev workflow --stage test-multi|test-standalone` → `dotnet <runner>.cs` →
`RunAllTests()`), which has no coverage hook today. Jaribu task 060 adds a runfile coverage mode
(relaunch under `dotnet-coverage`, Cobertura output in `artifacts/coverage`). This task adopts it.

## Requirements

- Bump `TimeWarp.Jaribu` to the prerelease that ships 060.
- `tools/dev-cli/endpoints/test-command.cs` turns coverage on in CI mode (`--coverage` flag or
  `JARIBU_COVERAGE=1`), results dir `artifacts/coverage`, module filter to `TimeWarp.Nuru*`
  product assemblies.
- `.github/workflows/workflow.yml`: install `dotnet-coverage` in the test stages and add an
  `actions/upload-artifact` step with `path: artifacts/coverage` and a per-matrix-leg `name`
  (`coverage-test-multi`, `coverage-test-standalone`) so the legs do not collide.
- `ganda repo audit` reports `ci-code-coverage` PASS (the check reads `dotnet-coverage`/`--coverage`
  in dev-cli source plus the upload step; its auto-fix cannot do this for non-MTP runners).
- Do not set `ci-code-coverage.min-line` yet; first look at the numbers.

## Goal (implementer restatement)

CI must produce a Cobertura coverage report from Nuru's runfile test runners and upload it, so
`ganda repo audit` passes `ci-code-coverage`. Coverage collection comes from Jaribu's runfile
coverage mode (task 060, shipped in `TimeWarp.Jaribu` 1.0.0-beta.16), not a Nuru-side wrapper.

- `Directory.Packages.props` pins `TimeWarp.Jaribu` 1.0.0-beta.16.
- `dev test --coverage` sets `JARIBU_COVERAGE=1`, `JARIBU_COVERAGE_RESULTS=<repo>/artifacts/coverage`
  and a module filter that keeps only `TimeWarp.Nuru*` assemblies. `dev workflow` turns that on
  for the test stages when it runs under GitHub Actions (`GITHUB_ACTIONS=true`); local runs stay
  coverage-free unless `--coverage` is passed.
- Both runners join one collection per leg: `run-ci-tests.cs` relaunches through `RunAllTests()`;
  `run-standalone-tests.cs` (which registers no test classes) relaunches through
  `RunAllTests(allowEmpty: true)` before it starts the per-file `dotnet run` children, so the
  children are profiled by that one collector. A relaunched parent returns the child's exit code
  and does not run any phase a second time.
- `.github/workflows/workflow.yml` installs `dotnet-coverage` in the `test-multi` and
  `test-standalone` legs and uploads `artifacts/coverage` as `coverage-test-multi` /
  `coverage-test-standalone`.
- Acceptance: build and test gates pass; a local `--coverage` run writes
  `artifacts/coverage/coverage.cobertura.xml` with only `TimeWarp.Nuru*` packages; `ganda repo audit`
  reports `ci-code-coverage` PASS; the first line-rate numbers are recorded under Results; no
  `ci-code-coverage.min-line` is set.

## Checklist

- [x] Jaribu bumped
- [x] test-command.cs enables coverage in CI
- [x] Workflow installs dotnet-coverage and uploads artifacts
- [x] Audit PASS on the branch
- [x] Record the first line-rate numbers under Results

## Results

`TimeWarp.Jaribu` is now 1.0.0-beta.16 (nuget.org), which ships runfile coverage (timewarp-jaribu 060).
`dev test --coverage` sets `JARIBU_COVERAGE=1`, `JARIBU_COVERAGE_RESULTS=<repo>/artifacts/coverage`, and
`JARIBU_COVERAGE_MODULE_FILTER` to one exclude regex that drops every assembly whose file name does not
start with `TimeWarp.Nuru`. `dev workflow` passes `Coverage = true` to the test step when
`GITHUB_ACTIONS=true`. A local `./bin/dev test` or `./bin/dev workflow` still runs without coverage.

`run-ci-tests.cs` relaunches through its existing `RunAllTests()`. The relaunched parent now returns the
child's exit code right after that call, so phase `all` does not run the standalone phase twice.
`run-standalone-tests.cs` registers no test classes. It calls `RunAllTests(allowEmpty: true)` first, only
when coverage is requested and this is not the coverage child. Every per-file `dotnet run` then runs
inside that one collection and inherits `JARIBU_COVERAGE_ACTIVE=1`, so the per-file test files do not
relaunch again. Each leg writes one `artifacts/coverage/coverage.cobertura.xml`.

`workflow.yml` installs `dotnet-coverage` (`dotnet tool install --global`) in the `test-*` legs, and after
the stage it uploads `artifacts/coverage` as `coverage-${{ matrix.stage }}`
(`coverage-test-multi`, `coverage-test-standalone`). The upload step uses `if: success()` and
`if-no-files-found: warn`. `ci-code-coverage.min-line` is not set.

First numbers (local Linux x64, dynamic profiler, 2026-11-14):

| Leg | Line rate | Branch rate | Lines covered / valid |
|-----|-----------|-------------|-----------------------|
| test-multi | 44.0% | 35.6% | 18897 / 42919 |
| test-standalone | 40.2% | 37.4% | 14113 / 35121 |

Per package, test-multi: Nuru.Build 83.2%, Nuru.Analyzers 30.4%, Nuru 57.0%, Nuru.Search 37.5%,
Nuru.McpHost 88.3%, Nuru.Daemon 85.4%, Nuru.Mcp 59.4%. Per package, test-standalone: Nuru.Build 71.0%,
Nuru.Analyzers 68.7%, Nuru 2.9%. The two reports are not merged; the analyzer is covered mostly by the
standalone generator tests. Each report lists only `TimeWarp.Nuru*` packages, with no runner, test, Jaribu,
Amuru, or Mediator assemblies. The multi leg took 3m56s under coverage, compared with about 2m without it.

### Gates

- `./bin/dev build` (after `dotnet run --file tools/dev-cli/dev.cs -- self-install`, because dev-cli changed): exit 0, 0 warnings.
- `./bin/dev test`: exit 0. Multi-mode total 2185, 2179 passed, 6 skipped. Standalone 45 files, 0 failed. Coverage off, no `artifacts/coverage`.
- `ganda repo audit`: the first run passed with one advisory warning: `vscode-window-icon` reported that `.vscode/settings.json` was missing. `ganda repo audit --fix` wrote that file, which is git-ignored, so nothing is committed. On the rerun, 44 passed, 0 failed, 1 skipped (`self-hosted-runner`, public repo), and `ci-code-coverage` PASS ("CI collects code coverage and uploads ..."). No `[ganda.audit]` severity was changed.

### How to validate

Smoke:

- `dotnet tool install --global dotnet-coverage` (if missing)
- `./bin/dev test --phase multi --coverage`
- `./bin/dev test --phase standalone --coverage`
- `./bin/dev test`
- `ganda repo audit`

Expect:

- Both coverage runs exit 0 and print `Code coverage results: <repo>/artifacts/coverage/coverage.cobertura.xml`. The multi run prints `NURU_CI_PHASE=multi: standalone phase skipped.` once from the coverage child and does not run the standalone phase in the parent.
- Each Cobertura file lists only packages named `TimeWarp.Nuru*` (multi: Build, Analyzers, Nuru, Search, McpHost, Daemon, Mcp; standalone: Build, Analyzers, Nuru).
- `./bin/dev test` with no flag exits 0 (2179 passed, 6 skipped, standalone 0 failed) and writes no `artifacts/coverage`.
- `ganda repo audit` reports `ci-code-coverage` PASS.
- On the PR, the `test-multi` and `test-standalone` jobs each upload an artifact named `coverage-test-multi` / `coverage-test-standalone`.

### Review

Round 1, effort 2, roster general. Counts: bug 0, suggestion 0, nit 0 (all open/fixed/wontfix zero). Disposition: clean. The implementer restatement matches the task; no misread. Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Depends on

- timewarp-jaribu 060

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-11)
- Implement (2026-11-14): the first coverage run failed with CS0103 `RunfileCoverageOptions`. The global
  NuGet cache held a 1.0.0-beta.16 packed on 2026-11-07 from a local timewarp-jaribu repro feed
  (task 048), before 060 merged. That entry was moved aside to `/tmp`, so restore fetched the real
  nuget.org beta.16. CI runners start with an empty cache and do not hit this. A machine with the same
  stale entry needs the same step: remove `~/.nuget/packages/timewarp.jaribu/1.0.0-beta.16`.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e7e486b1-a125-4563-bb00-7475aa2d9223, max-turns 500, Done — 2026-11-13T18:15:40Z
- Review oracle round 1: Grok 4.7 (grok, vendor xai), body tw-implementation-review, reviewer-cursor, effort 2, roster general, turn cap 120 (cursor session uncapped; no --max-turns flag), session not reported, disposition clean — 2026-11-14. No findings. See `review/round-1/merged.md` and `review/disposition.md`.
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-13T18:21:44Z

## Review

- Round 1: Done

## Notes

Steve, 2026-11-11: coverage belongs in Jaribu, not a per-repo wrapper. If 060 stalls, the
fallback is wrapping the runner in `dotnet-coverage collect` inside test-command.cs, which passes
the same audit check. `## Depends on` here names another repo's task, which the ganda gate does
not parse; it is documentation, not a merge wait.
