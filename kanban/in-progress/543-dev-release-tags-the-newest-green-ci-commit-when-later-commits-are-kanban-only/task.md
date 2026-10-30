# dev release tags the newest green-CI commit when later commits are kanban-only

## Description

`dev release` (`source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`, Step 8)
requires a successful `workflow.yml` run at HEAD. Consumer repos' CI `push` path filters leave out
`kanban/`, so a board-only commit (`land kanban N`, `publish kanban N`) never gets a CI run. Any
session can push one at any moment.

Observed in timewarp-architecture, 2026-10-26, cutting v2.0.0-beta.26: the merge commit
`07addaa01` was green. HEAD was `land kanban 401`, which had no run, so the guard refused. A
dispatched CI run went green on that commit, but by then another session had pushed
`publish kanban 416`, and the guard refused again. A second dispatch happened to win the race.
Every release is a race against board traffic, and each lost round costs a full CI run that
builds nothing new.

## Rule

Tag the newest commit on `origin/master` that has a successful `workflow.yml` run, provided every
commit after it, up to HEAD, changes only paths under `kanban/`. Tag that commit, not HEAD.

- The release pipeline is unchanged. Its locate-run and tag-pin gates find the run at the tagged
  sha, and the artifact is byte-identical to the commit that was tagged.
- `<Version>` cannot differ, because the skipped commits touch only `kanban/`. Still read
  `<Version>` and run the tag and publish-state guards against the target commit.
- If any later commit touches a path outside `kanban/`, refuse as today, and name the first
  offending commit and path.
- If no green run is found within a bounded walk back (for example 50 commits), refuse with
  today's message.

## Goal (implementer restatement)

Today `dev release` refuses unless HEAD itself has a green `workflow.yml` run. Commits that only
change the board never get a run, so every release races board traffic. Change Step 8 so it walks
back first-parent history from HEAD (at most 50 commits). It stops at the newest commit that has a
successful push or release run. Every commit it passes on the way must change only `kanban/` paths.
The release then tags that commit, not HEAD.

Acceptance:

- A pure classifier in `release-guard.cs` takes the walked commits (newest first: sha, changed
  paths, green run id) and returns the target sha, run id, and skipped count, or a refusal. Unit
  tests cover four cases: HEAD green, kanban-only on top of green, a code commit in between, and
  no green run in range.
- When a commit outside `kanban/` sits between HEAD and the green run, the refusal keeps today's
  message and also names the commit and the path. When no green run is found in range, it keeps
  today's message.
- `<Version>` is re-read at the target commit and must match the working tree. The tag and
  publish-state guards run after the target is resolved.
- The tag, the dry-run "Would run" lines, and "Target commit" all use the target sha. When the
  target is not HEAD, the command prints `✓ HEAD is N kanban-only commit(s) past <sha> (CI run <id>)`.
- The Design region (D1 step 8, D3) is updated. The release pipeline itself is unchanged: the
  tagged commit is an ancestor of master and has the green run.

## Checklist

- [x] Step 8 walks back from HEAD (`git rev-list` over first-parent `origin/master`) to the
      newest commit with a successful `workflow.yml` run; every commit after it must be
      kanban-only (`git diff --name-only <c>^ <c>` or one `git diff --name-only <target> HEAD`)
- [x] The pure decision (target sha, or refusal with reason) is in `release-guard.cs` as a
      `GuardVerdict`-style classifier with unit tests: HEAD green; HEAD kanban-only on top of
      green; a non-kanban commit in between; no green run in range
- [x] Tag, the dry-run "Would run" lines, and the "Target commit" output use the target sha. When
      it is not HEAD, print `✓ HEAD is N kanban-only commit(s) past <sha> (CI run <id>)`
- [x] Update the Design region (D1 step 8, D3)
- [ ] Update the `tw-release` skill's guard table: the "no successful CI run at HEAD" row
      (follow-up: the skill lives in timewarp-flow `skills/tw-release`, so the change needs a task
      there; proposed row text is under ## Results)
- [ ] Release a Nuru.DevCli beta so timewarp-architecture can take it (post-merge: bump + `dev release`
      from master; not possible from the task branch)

## Out of scope

- Configurable non-shipping paths. `kanban/` is the only case seen. Reading `workflow.yml` path
  filters is more code for no observed case.

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)
- Reopened 2026-10-26 (Steve "yes"): PR #357 carried only this spec and a .gitignore line, but merging it with `--task-id 543` moved the task to done. No implementation has landed; 4.0.0-beta.6 does not contain it.

- 2026-10-30 implement (implementer-claude, headless task work): walk-back classifier, step 8
  orchestration, Design regions, tests, releasing guide, changelog. A dry-run smoke in a temp clone
  found `git show` passes the props file's UTF-8 BOM through, so `PropsVersionReader.Parse` failed.
  Fixed (BOM trimmed, unparseable XML at target refuses as unreadable) with a regression test.
  `ganda repo audit` passed with no fixes needed.

## Results

`dev release` step 8 now resolves a **release target** instead of requiring a run at HEAD:

- `ReleaseGuard.SelectReleaseTarget(headSha, commitsNewestFirst)` in `release-guard.cs` is the pure
  decision. It returns `ReleaseTargetVerdict` (target sha, run id, skipped count) or a refusal.
  `ReleaseTargetCommit` holds sha, changed paths, and green run id. `FindShippingPath` holds the
  `kanban/` rule, and `ReleaseTargetWalkLimit = 50`.
- `release-command.cs` `LocateReleaseTargetAsync` runs `git rev-list --first-parent --max-count=50 HEAD`.
  For each commit it calls `gh run list --commit <sha> --status success` (the
  `CiRunPromotion.OrderCandidateRuns` filter, so `pull_request` runs never count), then
  `git diff --name-only <sha>^1 <sha>`. It stops at the first green commit or the first path outside
  `kanban/`, so a green HEAD still costs one gh call.
- When the target is behind HEAD, `<Version>` is re-read with `git show <target>:source/Directory.Build.props`
  and must match (`ReleaseGuard.CheckTargetVersion`). The tag and publish guards keep their step
  numbers (6, 7) and check that same version.
- The tag, `Created tag`, the dry-run "Target commit", and the "Would run" lines use the target sha.
  When the target is not HEAD, the command prints `✓ HEAD is N kanban-only commit(s) past <sha> (CI run <id>)`.
- A refusal keeps today's text and appends `Commit <sha> changes <path> (and N more), outside kanban/, ...`.
  When no green run is found within 50 commits, the text is today's, unchanged.
- `PropsVersionReader.Parse(xml)` is split out of `Read` and ignores a leading BOM.
- The Design region (D1 step 8, D3), the releasing guide's guard 8 and step 1, and the changelog
  under Fixed are updated. `workflow.yml` is unchanged: locate-run and tag-pin match the run at the
  tagged sha, and the ancestor check passes because the target is on master.

Related: to-do task **555** ("dev release accepts the last green CI run when later commits only
touch kanban") is a duplicate of this task, filed 2026-10-30 from timewarp-architecture. Its open
question is answered by this task: option 1, tag the green commit. Close 555 once this merges.

**Follow-up for the `tw-release` skill** (timewarp-flow `skills/tw-release/SKILL.md`, guard table).
Proposed replacement row:

`| no successful CI run at HEAD (or a commit outside kanban/ after the last green run) | Wait for CI on HEAD or `gh run rerun <id>`. Kanban-only commits after a green run no longer refuse: dev release tags the green commit (Nuru.DevCli with task 543) |`

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-06-props-version-reader.cs
# End to end, from master once a kanban-only commit sits on a green merge and <Version> is unreleased:
dev release --dry-run
```

Expect:

- The guard matrix shows 32 passed, including `Green_head_is_the_target`,
  `Kanban_only_commits_on_top_of_green_target_the_green_commit`,
  `Code_commit_between_head_and_green_refuses_naming_commit_and_path`, and
  `No_green_run_in_range_refuses_with_todays_message`. The props reader shows 8 passed, including
  `Parse_ignores_leading_byte_order_mark`.
- The dry-run prints `✓ HEAD is N kanban-only commit(s) past <short> (CI run <id>)`, and its
  `Target commit:` and `git tag -a ... <sha>` lines name the green commit, not HEAD.
- Verified 2026-10-30 in a temp clone of origin/master `a17d0a0b`, with tags removed, a bogus
  package configured, and `GH_REPO` set. The dry-run printed
  `✓ HEAD is 3 kanban-only commit(s) past ef124d8 (CI run 40890333638)`. HEAD reset to `ef124d8`
  printed `✓ successful CI run found (40890333638)`. A version-bump commit plus a kanban commit on
  top refused with `Commit 54f48af… changes source/Directory.Build.props, outside kanban/`.
- `dotnet run tests/ci-tests/run-ci-tests.cs` (cache cleared): multi-mode 2159 passed, 6 skipped;
  standalone 0 failed. `ganda repo audit`: all checks pass.

## Notes

Requested by Steve 2026-10-26 ("yes" to filing it) after the beta.26 race in timewarp-architecture.
