# Round 1 — general
**Date:** 2026-11-28
**Scope reviewed:** branch `task/540-enable-the-green-key-ledger-skip-on-nuru-after` vs `master` (commits 44290254..85363b84): `.github/workflows/workflow.yml`, `tools/dev-cli/services/green-key-*.cs`, `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/Directory.Build.props`, `tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs` and its props, and the CI include/exclude lists.

## Summary

The change ports ganda's green-key-v1 ledger into nuru's dev CLI in shadow mode. Pull requests observe inside `classify`. Every other event observes in a parallel `green-key` job. The `ci` job logs the end line and uploads `green-key-v1-<key>` after a same-repo full success. Nothing is skipped: the stage matrix does not read any green-key output, and `GreenKeyShadow.Enabled` pins the effective run kind to Full. The key rules match ganda: same line layout, the event classes, the fork rule, the force trailer, and the master carry rule.

The exclusions match `paths-ignore`, and I found no job that reads an excluded path. Both observe sites run on the same runner (wslc for same-repo runs), so the PR and master keys can match. The recorded gates cover the change: build, the 14-test standalone rules file, the dev.cs type-check, and the audit.

The main gap is scope. Most of the original task (enable the skip, the master and release proofs, minutes saved) is deferred to the shadow window, and no card tracks it. A smaller risk: the shadow logger can turn a green master run red, and release promotion only counts runs with status `success`.

## Issues

### Issue 1 — Severity: bug
- File: kanban/in-progress/540-enable-the-green-key-ledger-skip-on-nuru-after-ganda-and-architecture-prove-out/task.md:36
- Description: The task asks to enable the skip, prove that a tree-equal master push is skipped with `ci` green, prove that release still finds `Packages-*`, and record skips and minutes saved. This branch delivers only the shadow logger. Checklist items 3–7 stay unchecked, and the restatement says they "wait on the shadow window". The walk's later host nodes merge the PR and move this card to done, so those requirements are closed with no task trail. No follow-up card, `## Depends on` entry, or Notes pointer carries them.
- Suggestion: File a follow-up card with `ganda kanban create`, for example "Read nuru's green-key shadow window and enable the skip", with `## Depends on: 540`. Copy over the deferred requirements and checklist items 3–7, and the ganda 709 sizing note. Then name that card in 540's `## Results` and `## Notes`, and mark 540's deferred checklist items as moved.
- Status: open

### Issue 2 — Severity: suggestion
- File: .github/workflows/workflow.yml:109
- Description: The ledger is meant to never fail CI ("continue-on-error so the ledger cannot fail the required ci check"), but only the finish and upload steps are guarded. (a) The new `green-key` job has no `continue-on-error`. If its checkout, setup-dotnet, or dev.cs compile fails (for example a NuGet restore flake on the wslc runner), the master push run concludes `failure` even when `ci` is green and `Packages-*` was uploaded. `dev release`'s `LocateReleaseTargetAsync` (`source/timewarp-nuru-devcli/content/any/endpoints/release-command.guards.cs:333-336`) only lists runs with `--status success`. That commit then cannot be promoted, which goes against "Release must still find a `Packages-*` for every master commit it can promote." (b) The `ci` job's new `Checkout repository` and `Setup .NET` steps (lines 320, 327) are unguarded too. A failure there fails the required check and skips the `Packages-*` upload. The `ci` job used to need no checkout. This also adds a checkout, an SDK setup, and a dev.cs compile to the critical path of every run, and `## Results` does not record how long that takes.
- Suggestion: Add job-level `continue-on-error: true` to `green-key`. Add `continue-on-error: true` to the two new `ci` setup steps, and gate the finish step on their outcome. Record the added `ci` wall time in Results, because it offsets the minutes the skip will later save.
- Status: open

### Issue 3 — Severity: nit
- File: .github/workflows/workflow.yml:342
- Description: `GREEN_KEY_PIPELINE` maps every non-success `needs.stage.result` to `failed`, including `cancelled`. Superseded pull request runs are cancelled (`cancel-in-progress`). In a cancelled run the new Checkout and Setup steps have the default `success()` condition and do not run, so the `always()` finish step runs `dotnet run --file tools/dev-cli/dev.cs` with no checkout and errors under `continue-on-error`. The run gets no end line. If those steps did run, a superseded hit would be logged as `false_skip=true`, the signal that blocks enabling the skip.
- Suggestion: Use `!cancelled()` on the finish step. Or add a `cancelled` pipeline value that `FinishFromEnvironment` accepts with `rate=out false_skip=false`, plus a rules test for it.
- Status: open

### Issue 4 — Severity: nit
- File: tools/dev-cli/services/green-key-ledger.cs:371
- Description: `PredictMasterCarryAsync` probes every live marker run for the key, pull request runs included. Ganda's rule is "another green **master** run with the same key". Pull request runs never upload `Packages-*`, so each pull request row is a wasted `gh api` call. Each green re-push of a pull request uploads another marker for the same key, so pull request rows can use up `CarryRunLimit` (8) before an older master run is probed.
- Suggestion: Add `head_branch:.workflow_run.head_branch` (or the event) to the jq projection, and pass only master rows to `CarryCandidateRuns`.
- Status: open

### Issue 5 — Severity: nit
- File: .github/workflows/workflow.yml:119
- Description: The `green-key` job's `runs-on` includes a `github.event_name == 'pull_request' && …head.repo…` clause, but the job has `if: github.event_name != 'pull_request'`, so that clause can never match.
- Suggestion: Reduce it to `vars.CI_HOSTED_ONLY != 'true' && github.event_name == 'push' && fromJSON(...) || 'ubuntu-latest'`.
- Status: open

## Checked and fine

- Stage jobs: `stageJob.ShouldNotContain("green_key")` holds. The matrix reads only `needs.changes.outputs.code`.
- The `changes` outputs default to empty, so `needs.changes.outputs.green_key || needs['green-key'].outputs.green_key` falls through correctly on push.
- The PR checkout is `fetch-depth: 0`, so the force-trailer read of `pull_request.head.sha` works. The master job is `fetch-depth: 1` and reads the trailer at `GITHUB_SHA` = HEAD.
- The wslc image bakes zsh, and zsh is installed only in `test-multi`. The dpkg-status runner line is hashed in jobs that install nothing, so it is stable across runs.
- No job reads `documentation/`, `skills/`, `.agent/`, `.claude/`, `assets/mascot/`, `changelog.md`, or the avatar svg (searched the csproj/props/targets files, tests, and tools).
- Forks: `Fork` disposition means `allowWrite=false`, so `green_key_writer=false` and no marker is uploaded. Release, schedule, and dispatch are `Excluded`.
- A path-filter skip (`code=false`) logs `pipeline=path-skipped rate=out` from classify and stages no marker.
