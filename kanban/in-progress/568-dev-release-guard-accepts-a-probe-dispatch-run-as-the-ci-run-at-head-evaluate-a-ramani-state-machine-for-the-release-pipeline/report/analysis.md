# Ramani state machine for the release pipeline — evaluation (task 568)

## Executive Summary

Recommendation: **not now**. Ramani's `.Exhaustive()` checks that every (state, signal)
pair has a decision. It would not have caught this bug, because the guard took the right
transition on a predicate that was too weak. The release pipeline is a linear fail-fast
sequence, and its real state (tag, GitHub Release, NuGet versions, CI artifacts) lives
outside the process, so an in-memory run-to-completion machine adds a generator and a
package to every consuming repo without moving that state anywhere. No child task is filed.

## Scope

- `dev release` (DevCli content, compiled into every consuming repo's `tools/dev-cli`):
  `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`,
  `release-command.guards.cs`, `services/release-guard.cs`, `services/ci-run-promotion.cs`.
- The Actions-side `dev workflow` release mode, which is repo-local to this repo:
  `tools/dev-cli/endpoints/workflow-command.release.cs` and its partials.
- The releasing guide, `documentation/developer/guides/releasing.md`.
- TimeWarp.Ramani 1.0.0-beta.24 from the local checkout of timewarp-ramani.

## Methodology

I read the release code above and the guide. I read the Ramani sources and docs for
these points: builder shape (`samples/ramani-aot-host/door-machine.cs`), exhaustiveness
(`documentation/refusals-and-exhaustiveness.md`,
`source/timewarp-ramani-generators/diagnostics/refusal-diagnostics.cs`),
`MachineStep` (`source/timewarp-ramani/machine-step.cs`), the mermaid emitter
(`source/timewarp-ramani-generators/emitters/mermaid-emitter.cs`), do-activities
(`source/timewarp-ramani/state-builder.do-activity.cs`), and packaging
(`source/timewarp-ramani/timewarp-ramani.csproj`). I fixed the bug itself on this task
without Ramani, which shows how small that fix is in the current shape.

## Findings

### F1. Mapping onto states and signals

`dev release` is ten steps. Each one gathers state with `git` or `gh`, classifies it
with a pure function (`GuardVerdict`, `ReleaseTargetVerdict`), and either moves on or
exits 1. As a machine that is roughly `Version → Auth → Tree → Branch → Sync → Tag →
Publish → Target → (DryRun | Tagging → Pushing → Releasing) → Done`, with one
`GuardPassed` and one `GuardFailed(reason)` signal per state. That fits, but it is a
straight line. Every state has one way forward and one way out, so the table holds the
same information as the order of the `if` blocks in `Handle`.

Ramani keeps guards, entry, exit and effects synchronous. Only a state's `.Do`
activity can await IO. Each `git` or `gh` call would become a `.Do` activity, or would
run outside the machine and feed it signals. In both cases the classification (is this
run usable?) still happens in the same pure helpers before the signal exists.

Break-glass resume and partial-publish detection are where a machine looks more
useful. Their state is not in the process, though. It is "tag `v{X}` exists on origin",
"some packages are on NuGet", "the tagged commit's run has a live artifact". Each
`dev release` or `dev workflow` invocation works that state out again from the world.
A run-to-completion machine starts from its initial state on every invocation, so it
would model one pass through the classifiers, not the release's life across runs. The
cross-run states are the cells of `PublishStateClassifier` (None, Partial, All) times
the tag-pin check. They are already enumerated as enums and tested as a matrix.

### F2. Would `.Exhaustive()` have caught this bug?

No. `.Exhaustive()` reports SM027 when some (state, signal) pair has no unguarded
transition, `.Else()`, `Defer`, `Refuse` or `RefuseOthers`. It looks at whether a guard
exists, not at what the guard says. The step 8 bug had full coverage: at a commit, a
run was found (go to target) or not (walk on or refuse). The defect was that "a run was
found" meant "some successful non-`pull_request` run" instead of "a run the release
event can promote". In Ramani terms that is a weak guard predicate or a wrong signal
classification. Both are invisible to pair coverage.

What does catch it is what this task added: a classifier
(`CiRunPromotion.DescribeUnreleasableRun`) that applies the same rule as the Actions-side
download step (`SelectPackagesArtifact` returns Found), plus a guard-matrix test that
replays the observed timewarp-ramani history.

Exhaustiveness would help with a different kind of gap: a new signal (say a
`ProbeOnly` classification) added without a decision in some state. The current code
makes that gap unlikely because each step is an `if` over a two-valued verdict.

### F3. Generated refusals and the guide

- `Refuse<T>(reason)` needs a compile-time constant reason (SM012). Nearly every
  release refusal is built from runtime data: the branch name, ahead and behind counts,
  the tag, where it exists, the sha, the shipping path, the props version. Those would
  go through `RefuseOthers` with a factory, and factory reasons are not readable from
  `Definition()`. A generated table could list which signals each state refuses, but
  not the wording operators see. The guide's refusal text would still be maintained by
  hand. The current guard-matrix tests already pin that wording.
- The guide has no pipeline diagram today. It has two inline arrow lines
  (`clean → build → verify-samples → test` and
  `tag-gate → check-version → locate-run → download-artifact → verify → push`). The
  generated `Mermaid` constant could replace them with a stateDiagram-v2 that shows the
  refusal rows inside each state. For a straight line that adds little. It would pay
  off only if resume and break-glass were modelled in the same machine, and F1 says
  they cannot be, because their state lives outside the process.

### F4. Cost for DevCli consumers

- `TimeWarp.Nuru.DevCli` is source-only with `SuppressDependenciesWhenPacking=true`.
  Every consuming repo's `tools/dev-cli` declares the packages the content needs
  (Amuru, Nuru, Mediator). A Ramani machine in `release-command.cs` would make every
  consumer add `TimeWarp.Ramani` by hand, or the content stops compiling. That breaks
  every org repo's dev build at the next DevCli bump. The breakage is mechanical (one
  package line), but it falls on all of them for a change that brings them no
  behaviour.
- AOT: Ramani sets `IsAotCompatible`, the trim analyzer and the AOT analyzer, and
  `samples/ramani-aot-host` publishes NativeAOT with warnings as errors. Its only
  runtime dependency is `TimeWarp.Builder`. AOT is not a blocker.
- Runfiles: the generator ships inside the `TimeWarp.Ramani` package
  (`analyzers/dotnet/cs`), the same way Nuru's generator does, and Nuru's generator
  works from `#:package` runfiles. I found no Ramani sample or test that runs from a
  runfile, so that path is unverified. It also adds a second source generator run to
  every dev build (and runfile cache key) in every consumer.
- The machine must sit in a `partial` class (SM022). That fits DevCli's existing
  partial handler classes.

### F5. Actions side only

The Actions-side release mode (`tools/dev-cli/endpoints/workflow-command.release.cs`) is
repo-local to timewarp-nuru, not DevCli content, so adopting Ramani there costs only
this repo. It is the same linear shape (`tag-gate → … → push`), with a walk over
candidate runs in download-artifact. The same F1 and F2 limits apply: the decisions are
two-valued verdicts from pure classifiers, and the state that matters is outside the
process. One repo would gain a dependency and a second way to express the pipeline,
and other repos copying that workflow command would diverge.

## Recommendations

1. **Not now** for `dev release` (DevCli content): the cost falls on every consumer,
   and the benefit does not cover the class of bug that prompted this evaluation.
2. **Not now** for the Actions-side workflow command: no consumer cost, but also no
   gap that pair coverage would close.
3. Keep the current pattern (pure classifier, verdict record, guard-matrix test) and
   fix weak predicates where they are, as this task did for step 8. A predicate that
   must agree with a later pipeline step should call that step's own classifier, as
   `DescribeUnreleasableRun` reuses `SelectPackagesArtifact`.
4. Revisit if one process ever needs to hold release state across several external
   events, for example a long-running release daemon that watches the release run and
   resumes it. Then the state is in the process, and Ramani's exhaustiveness and
   diagram would describe something real.
5. For timewarp-ganda task 567 (worktree gc refusing from an unexpected phase): that
   case is closer to Ramani's strength, because the bug class there is "a (phase,
   action) pair nobody decided", which is what SM027 reports. Judge it on its own
   terms; this evaluation does not carry over.

No child task is filed, because the recommendation is not to adopt.

## References

- TimeWarp.Ramani docs: `documentation/refusals-and-exhaustiveness.md` in timewarp-ramani
- `documentation/developer/guides/releasing.md` (guard 8, promotion pipeline)
- `source/timewarp-nuru-devcli/content/any/services/ci-run-promotion.cs`
  (`DescribeUnreleasableRun`, `SelectPackagesArtifact`)
- `tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs` (task 568 cases)
