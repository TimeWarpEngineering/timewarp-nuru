# Add a shared dev benchmark contract to the dev-cli (list, run --base, verdict)

## Description

Implement, size M. Put one reusable `dev benchmark` contract in the Nuru dev-cli so every TimeWarp repo answers the same three questions the same way. Part of the cross-repo perf sweep, umbrella timewarp-ganda 599.

The contract:

- `dev benchmark list` prints the named benchmarks the repo offers. An empty list means the repo does not take part in a perf sweep.
- `dev benchmark run <name> --base <git-ref>` builds and runs the base ref and the head in the same run on the same machine, then compares ratios. It does not compare against a stored absolute number, because shared CI runners are too noisy for that.
- The output is machine readable: the ratio, the noise band, and one verdict of `win`, `noise`, or `regression`.

Shared plumbing lives in Nuru as a reusable dev-cli piece or package (the part that checks out base, runs both sides, computes the ratio and noise band, and prints the verdict). Each repo implements only its own named benchmarks and plugs them in.

Closest prior art, to read before designing:

- timewarp-mediator `tools/dev-cli/endpoints/benchmark-command.cs`, `dev benchmark --gate`, baselines under `documentation/benchmarks/`. It already compares against competitor libraries in the same run.
- timewarp-jaribu `tools/dev-cli/endpoints/benchmark-*.cs` and the nightly `.github/workflows/benchmark.yml` (done task 054). Different shape.
- Nuru's own `benchmarks/timewarp-nuru-benchmarks` and the stored `tests/benchmarks/aot-invocation-baseline.json`. That file is an absolute number, so it is weak and gets replaced here.
- timewarp-flexbox has a layout benchmark project that CI does not run. It is a later conformer, not part of this task.

## Goal (implementer restatement)

Ship one `dev benchmark` contract in the shared DevCli source-only package
(`source/timewarp-nuru-devcli/content/any`) so every TimeWarp repo answers "is head faster or
slower than base?" the same way, and convert Nuru to it.

Acceptance:

- `dev benchmark list` prints the repo's named benchmarks, one per line (`--json` for an array).
  No benchmarks registered prints nothing and exits 0: the repo is not in a perf sweep.
- `dev benchmark run <name> --base <git-ref>` checks out the base ref in a temporary git
  worktree, builds the named benchmark in both the base tree and the current working tree,
  runs both sides interleaved in the same process on the same machine, and compares the medians
  as a ratio (head / base). No stored absolute number is read or written.
- The result is one JSON object on stdout: benchmark, unit, base and head (ref, commit, median,
  MAD, sample count), `ratio`, `noiseBand`, and `verdict` of `win`, `noise`, or `regression`.
  Progress goes to stderr.
- The plumbing (registry, runner, statistics, base checkout, JSON, a reusable published-process
  timing benchmark) lives in DevCli and has Jaribu tests. A repo only implements
  `IDevBenchmark` and registers it in `DevBenchmarks.AddRepoBenchmarks`.
- The DevCli readme documents the contract for repo authors.
- Nuru registers its own benchmarks (`aot-startup`, `operation-dispatch`).
- `tests/benchmarks/aot-invocation-baseline.json` and the script that wrote it are deleted.
- The DevCli version is bumped so a release can be cut after merge (the release itself waits for
  an explicit ask, per the org rule).

## Related

- ganda 599 (umbrella, "Agent perf sweep across repos")
- mediator 038 (conform to this contract)
- jaribu 063 (conform to this contract)
- flow 204 (tw-perf-sweep skill)

## Checklist

- [x] Design the contract: command names, `--base` semantics, ratio and noise-band definition, and the machine-readable verdict shape (`win` / `noise` / `regression`)
- [x] Build the shared plumbing as a reusable dev-cli piece or package, with tests
- [x] Document the contract for repo authors (what a repo implements, what the plumbing does)
- [x] Convert Nuru's own benchmarks to conform: named benchmarks behind `dev benchmark list`, same-run A/B for `run --base`
- [x] Delete the stored absolute baseline `tests/benchmarks/aot-invocation-baseline.json` and anything that read it
- [ ] Release or publish the shared piece so mediator 038 and jaribu 063 can consume it (ships in the unreleased 4.0.0-beta.17; `dev release` after merge waits for Steven's explicit ask)

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-13)
- Implemented 2026-11-21 (ganda task work, implement oracle, Claude Opus 5.5).
- Commit 7f1dfd2e: the post-commit `ganda repo attest` failed with "Text file busy" on `bin/dev`
  (the dev CLI binary was being replaced while in use); this follow-up commit re-runs the hook.

## Results

### Contract (as built)

- Routes live in DevCli (`source/timewarp-nuru-devcli/content/any/endpoints/benchmark-*.cs`):
  `dev benchmark list [--json]` and
  `dev benchmark run <name> --base <ref> [--samples 30] [--warmup 3] [--noise-floor 0.02]`.
- `--base` is resolved with `git rev-parse --verify --end-of-options <ref>^{commit}` (no fetch)
  and checked out into a temporary detached worktree under the system temp dir with hooks off
  (`BenchmarkBaseCheckout`). Head is the working tree, uncommitted changes included.
- Samples are interleaved per round, order alternating (even rounds base first), after
  discarded warmup rounds (`DevBenchmarkRunner`).
- Ratio = median(head) / median(base). Noise band = max(floor, 3 x 1.2533 x
  sqrt(spread_b^2/n_b + spread_h^2/n_h)), spread = 1.4826 x MAD / median. Verdict: below
  1 - band win, above 1 + band regression, else noise (`BenchmarkComparison`).
- stdout is one `dev-benchmark/v1` JSON object; progress and a summary line go to stderr. Exit 0
  whenever a verdict exists; exit 1 with nothing on stdout when there is none.
- Registry: `DevBenchmarks.AddRepoBenchmarks` partial method (same shape as `DevAuditChecks`).
  Ready-made `ProcessInvocationBenchmark` (publish, AOT by default, time one process start) and
  `ReportedMetricBenchmark` (build, run, read a `key=value` stdout metric). Both pass
  `GeneratePackageOnBuild=false` because `-o` breaks packing of referenced packable projects.
- Nuru registers `aot-startup` (bench-nuru, AOT, ms) and `operation-dispatch`
  (`mediator-per-call-ns`) in `tools/dev-cli/benchmarks/nuru-benchmarks.cs`.
- Deleted `tests/benchmarks/aot-invocation-baseline.json` and `tests/scripts/benchmark-aot-invocation.cs`
  (the only reader/writer). Regenerated the `internals-visible-to.g.cs` files with
  `runfiles/generate-internals-visible-to.cs`; they were also stale against master and caught up.
- Docs: DevCli `readme.md` gains "Benchmark contract (`dev benchmark`)" and a 4.0.0-beta.17
  migration note; `changelog.md` Unreleased/Added entry. Version stays 4.0.0-beta.17 (not yet
  tagged; latest tag v4.0.0-beta.16), so no bump.
- Release: not cut. The org rule says a release waits for an explicit ask; after merge,
  `dev release` publishes 4.0.0-beta.17 for mediator 038 and jaribu 063.

### Gates

- `./bin/dev build` -> Build completed successfully.
- `./bin/dev self-install` (tools/dev-cli and DevCli content changed; compiles the content) -> installed, 0 warnings.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/benchmark-01-comparison.cs` -> 9/9 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/benchmark-02-runner.cs` -> 9/9 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/benchmark-03-base-checkout.cs` -> 3/3 passed.
- `dotnet build tests/ci-tests/run-ci-tests.cs` (compile only; the three files are multi-included) -> succeeded.
- `ganda repo audit` -> passed with 1 advisory (`vscode-window-icon`: `.vscode/settings.json` missing);
  `ganda repo audit --fix` created the git-ignored `.vscode/settings.json` -> "Repository passes all audit checks."

### Smoke runs

- `./bin/dev benchmark run operation-dispatch --base HEAD --samples 10 --warmup 1` -> 22 s,
  ratio 1.0003, band 2.7%, `noise`.
- `./bin/dev benchmark run aot-startup --base HEAD --samples 20` -> 27 s (two AOT publishes),
  base 2.713 ms, head 2.807 ms, ratio 1.0348, band 7.9%, `noise`. Temp worktree removed afterwards.

### How to validate

Smoke:

```bash
dotnet run --file tools/dev-cli/dev.cs -- self-install
./bin/dev benchmark list
./bin/dev benchmark run operation-dispatch --base HEAD --samples 10 --warmup 1 > /tmp/verdict.json
cat /tmp/verdict.json
./bin/dev benchmark run nope --base HEAD; echo "exit=$?"
dotnet run --file tests/timewarp-nuru-tests/devcli/benchmark-01-comparison.cs
dotnet run --file tests/timewarp-nuru-tests/devcli/benchmark-02-runner.cs
dotnet run --file tests/timewarp-nuru-tests/devcli/benchmark-03-base-checkout.cs
```

Expect:

- `list` prints two lines: `aot-startup<TAB>[ms] ...` and `operation-dispatch<TAB>[ns] ...`.
- The run prints progress on stderr and a JSON object on stdout with `"schema": "dev-benchmark/v1"`,
  `ratio` near 1.0, and `"verdict": "noise"` (same commit on both sides); exit 0.
- `run nope` prints `Unknown benchmark 'nope'. Known: aot-startup, operation-dispatch` and exits 1.
- `git worktree list` shows no `dev-benchmark-base-*` entry after a run.
- The three test files pass (9, 9, 3).

## Notes

- Compare ratios, not stored absolute numbers; shared CI runners are too noisy.
- Bench jobs stay off the PR path.
- Order in the sweep: this is step 1. Mediator 038 and 035 wait on it.
- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-11-agent-perf-sweep.md
- why: Steven 2026-11-13, perf-sweep split across repos
