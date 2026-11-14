# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** product commit `73a533c1` against `## Goal (implementer restatement)` and the original task. `## Results`, `### Gates`, and `### How to validate` read and not re-run.

## Summary

`LocateReleaseTargetAsync` still lists successful `workflow.yml` runs per commit and orders them with `OrderCandidateRuns`. It now lists each candidate's artifacts and keeps the run only when `CiRunPromotion.DescribeUnreleasableRun` returns null, which is when `SelectPackagesArtifact` finds a live `Packages-*` artifact. Other runs print `skipped run {id} ({event}, {reason}) at {short sha}` before the dry-run branch, so dry-run and a real release both show the line. A commit whose runs are all skipped falls through to the existing path check, so a kanban-only commit is walked and a shipping commit refuses with the Packages-* wording.

The restatement matches the task. The task's artifact check is the option implemented; expired artifacts are treated as unusable, which is the same rule the Actions download step already applies. The guide's guard 8 states the skip line and the new refusal. `report/analysis.md` answers the four Ramani questions and recommends not now, so no child task is required. There is no misread.

`## Results` covers the change: build, DevCli compile via `self-install`, `./bin/dev test` (4660 tests, 0 failed, including the five new cases), and audit (44 passed after the advisory fix). `### How to validate` names the guard-matrix runfile and the expected target, run id, and skip line. The file contains 37 `public static async Task` methods, matching that expect. Those gates were not re-run.

Checked and not filed:

- The observed history is replayed in `Probe_dispatch_run_at_head_is_skipped_for_the_push_run_one_kanban_commit_back`: a no-artifact `workflow_dispatch` at `64f9d13` and `Packages-43` on push run `42448301249` at `0d899d7` select `0d899d7`. Classification of live, missing, and expired artifacts is `DescribeUnreleasableRun`. The git/gh walk stays in the command, which is the split this guard-matrix file already documents.
- A `workflow_dispatch` that did upload a live `Packages-*` artifact still counts. `mode=merge` remains a valid way to produce one. Probe mode skips the `ci` job, so that run has no `Packages-*` artifact (`workflow.yml` release job, probe step only logs in).
- `gh api repos/{owner}/{repo}/actions/runs/{id}/artifacts` is the same call `DownloadPackagesArtifactAsync` already makes, and `RunArtifactListResponse` is already on `DevCliJsonContext`. A green master run uploads `staged-packages` plus one `Packages-*` artifact, under the default page size.
- Refusal text updates in the two existing assertions match `FormatNoSuccessfulRun`. The guide's step 3/6 locate-run refusals describe the Actions pipeline, which still separates "no successful run" from "no Packages artifact".
- Ramani claims checked against the local timewarp-ramani checkout at `1.0.0-beta.24`: SM027 is an undecided pair under `.Exhaustive()`, SM012 is a non-constant `.Refuse<T>` reason, SM022 is the partial-class generation warning, and the guide's two arrow lines are the only pipeline pictures. `TimeWarp.Nuru.DevCli` packs content with `SuppressDependenciesWhenPacking` and compiles nothing itself.
- `release-command.cs` still says a green HEAD costs one `gh` call. The walk now also lists artifacts, so a releasable HEAD costs the run list plus one artifacts call. The stop rule is unchanged. `changelog.md` has no Unreleased line; guard 8 of the releasing guide is the operator-facing note this task asked for.

## Issues

None.
