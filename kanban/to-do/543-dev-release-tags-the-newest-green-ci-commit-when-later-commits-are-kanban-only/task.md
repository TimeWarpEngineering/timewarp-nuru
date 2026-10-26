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

## Checklist

- [ ] Step 8 walks back from HEAD (`git rev-list` over first-parent `origin/master`) to the
      newest commit with a successful `workflow.yml` run; every commit after it must be
      kanban-only (`git diff --name-only <c>^ <c>` or one `git diff --name-only <target> HEAD`)
- [ ] The pure decision (target sha, or refusal with reason) is in `release-guard.cs` as a
      `GuardVerdict`-style classifier with unit tests: HEAD green; HEAD kanban-only on top of
      green; a non-kanban commit in between; no green run in range
- [ ] Tag, the dry-run "Would run" lines, and the "Target commit" output use the target sha. When
      it is not HEAD, print `✓ HEAD is N kanban-only commit(s) past <sha> (CI run <id>)`
- [ ] Update the Design region (D1 step 8, D3)
- [ ] Update the `tw-release` skill's guard table: the "no successful CI run at HEAD" row
- [ ] Release a Nuru.DevCli beta so timewarp-architecture can take it

## Out of scope

- Configurable non-shipping paths. `kanban/` is the only case seen. Reading `workflow.yml` path
  filters is more code for no observed case.

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-25)
- Reopened 2026-10-26 (Steve "yes"): PR #357 carried only this spec and a .gitignore line, but merging it with `--task-id 543` moved the task to done. No implementation has landed; 4.0.0-beta.6 does not contain it.

## Notes

Requested by Steve 2026-10-26 ("yes" to filing it) after the beta.26 race in timewarp-architecture.
