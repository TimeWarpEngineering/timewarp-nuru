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

## Related

- ganda 599 (umbrella, "Agent perf sweep across repos")
- mediator 038 (conform to this contract)
- jaribu 063 (conform to this contract)
- flow 204 (tw-perf-sweep skill)

## Checklist

- [ ] Design the contract: command names, `--base` semantics, ratio and noise-band definition, and the machine-readable verdict shape (`win` / `noise` / `regression`)
- [ ] Build the shared plumbing as a reusable dev-cli piece or package, with tests
- [ ] Document the contract for repo authors (what a repo implements, what the plumbing does)
- [ ] Convert Nuru's own benchmarks to conform: named benchmarks behind `dev benchmark list`, same-run A/B for `run --base`
- [ ] Delete the stored absolute baseline `tests/benchmarks/aot-invocation-baseline.json` and anything that read it
- [ ] Release or publish the shared piece so mediator 038 and jaribu 063 can consume it

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-13)

## Notes

- Compare ratios, not stored absolute numbers; shared CI runners are too noisy.
- Bench jobs stay off the PR path.
- Order in the sweep: this is step 1. Mediator 038 and 035 wait on it.
- Eval: https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-11-agent-perf-sweep.md
- Filed only; do not start until Steven says so.
- why: Steven 2026-11-13, perf-sweep split across repos
