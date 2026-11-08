# dev release guard accepts a probe dispatch run as the CI run at HEAD; evaluate a Ramani state machine for the release pipeline

## Description

Two parts. The first is a bug fix in DevCli's `dev release` guard. The second is an evaluation of whether the release pipeline (guards, target selection, promotion, resume) would be clearer and safer as a TimeWarp.Ramani state machine.

### Part 1: the guard bug

Observed in timewarp-ramani on 2026-11-08 while cutting v1.0.0-beta.23:

1. PR #17 merged; the master push run (42448301249) built `Packages-43` at commit 0d899d7.
2. Two kanban-only commits landed after it, so HEAD was 64f9d13.
3. `gh workflow run workflow.yml --ref master -f mode=probe` ran the trusted-publishing probe at HEAD (run 42448431673). Probe mode does an OIDC login only: no build, no pack, no artifact.
4. `dev release --dry-run` printed `✓ successful CI run found (42448431673)` and targeted 64f9d13.

Had the release been cut, the `release:published` run would have failed at locate-run, because the only successful run at the tagged commit has no `Packages-*` artifact. The intended target was the push run one kanban-only commit back, which the board-only walk (DevCli 4.0.0-beta.8+) would have found if the probe run had not been present.

Cause: `CiRunPromotion.OrderCandidateRuns` (`source/timewarp-nuru-devcli/content/any/services/ci-run-promotion.cs`) filters out only `pull_request` runs and prefers `push`, so a `workflow_dispatch` run at HEAD is accepted as a candidate regardless of what it did. `ReleaseCommand` (`release-command.cs`, around line 561) then stops walking back as soon as any candidate exists at a commit. The guard never asks whether the run has a packages artifact.

Fix options, pick one or combine:

- In the release command's walk, after `OrderCandidateRuns`, list each candidate run's artifacts (`gh api repos/{owner}/{repo}/actions/runs/{id}/artifacts`) and keep only runs where `SelectPackagesArtifact` returns a usable artifact. The existing release-publish step already does this lookup, so the helper exists. A commit whose only green runs have no artifact is then treated like a commit with no run, and the kanban-only walk continues.
- Additionally or instead, let `OrderCandidateRuns` drop `workflow_dispatch` runs whose inputs mark them as probe. The run list JSON does not carry inputs, so this needs an extra `gh run view --json` call per dispatch run; the artifact check above is simpler and also covers cancelled-after-build and other no-artifact cases.

Either way the dry-run line should say which run was skipped and why, for example `skipped run 42448431673 (workflow_dispatch, no Packages-* artifact)`.

### Part 2: evaluate Ramani for the pipeline

`dev release` is already a hand-written state walk: eight sequential guards, target selection, tag, GitHub Release, then the Actions side with tag-gate, check-version, locate-run, download-artifact, verify, push, plus break-glass resume and partial-publish detection. The guard code is pure classifiers returning `GuardVerdict` records, and the refusal table in the releasing guide is a transition table in prose.

TimeWarp.Ramani (nuget.org, 1.0.0-beta.23 or later) generates a run-to-completion state machine from a builder, with `.Exhaustive()` for compile-time proof that every (state, signal) pair is decided, per-state `Refuse<T>(reason)` for named refusals, a `MachineStep` result from `SendAsync` carrying outcome, reason and configuration (task 013 there), a mermaid emitter for the diagram, and a Blazor diagram component.

Write a short evaluation under this task's kitchen (`rfc/` or `report/` subfolder) answering:

- Would the guard sequence, resume paths and break-glass map cleanly onto states and signals, and would `.Exhaustive()` catch the kind of gap this bug is (a transition taken on an insufficient condition)? Note that Ramani's exhaustiveness is about pair coverage, not predicate strength, so be honest about what it would and would not have caught here.
- Would a generated refusal reason replace the hand-maintained refusal table in `documentation/developer/guides/releasing.md`, and could the mermaid output replace the pipeline diagram in that guide?
- Cost: DevCli is shipped as content files compiled into each consuming repo's `tools/dev-cli`; adding a Ramani package reference there affects every repo's dev build. State whether that is acceptable and whether the generator is AOT and runfile friendly in that context.
- Recommendation: adopt, adopt for the Actions-side workflow command only, or not now, with reasons.

Do not rewrite the pipeline on this task. If the recommendation is to adopt, file a child task with the plan.

## Checklist

- [ ] Reproduce with a unit test in `tests/timewarp-nuru-tests/devcli/` (guard matrix style): a HEAD with only a no-artifact dispatch run and a green push run one kanban-only commit back selects the push run
- [ ] Candidate runs without a usable `Packages-*` artifact are skipped, and the dry-run output says so
- [ ] Releasing guide refusal table updated for the new wording
- [ ] Ramani evaluation written under the kitchen with a clear recommendation
- [ ] Child task filed if the recommendation is to adopt
- [ ] `./bin/dev build` and `./bin/dev test` pass
- [ ] `ganda repo audit` clean

## Session

- Created: 4791cf15-d6b7-4af4-98f6-e16d3874da55 (2026-11-08)

## Notes

Filed from the timewarp-ramani v1.0.0-beta.23 release session. Workaround used there: `gh workflow run workflow.yml --ref master -f mode=merge` at HEAD to produce an artifact on the exact commit, then `dev release`.

Related: timewarp-ganda task 567 (worktree gc "unhandled" refusal) is another case of a hand-written phase machine refusing from an unexpected state; the Ramani evaluation here may inform that one.
