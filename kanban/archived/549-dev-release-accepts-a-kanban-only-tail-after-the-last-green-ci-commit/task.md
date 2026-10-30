# dev release accepts a kanban-only tail after the last green CI commit

## Description

`dev release` refuses when no successful `workflow.yml` run exists at exactly HEAD
(`release-command.cs` step 8, `gh run list --commit {headSha} --status success`).

Since taratibu task 055, every `ganda task work` merge ends with a `land-done` Compute that pushes a
kanban-only commit (`land kanban NNN`) to master after the PR merge. `workflow.yml` push triggers are
path-filtered (`source/**`, `tests/**`, `tools/**`, `.github/workflows/**`, props, `global.json`), so
that commit never gets a CI run. Result: master HEAD never has CI, and every release is refused.

Seen 2026-10-27 cutting timewarp-taratibu v1.0.0-beta.14: the bump merge `8a10b2a` had green CI
(run 40439976663), HEAD `358ce69` (`land kanban 056`, `kanban/` only) had none. Worked around by
`gh workflow run workflow.yml -f mode=merge` on HEAD.

## Requirements

- From HEAD, walk first-parent back to the newest commit with a successful `workflow.yml` run.
  Accept it only if every commit in between touches **only** `kanban/` paths. Any other path in the
  tail → refuse with the current message (plus the offending path).
- **Recommended:** tag that green commit, not HEAD. The tag, the `locate-run` lookup and the
  promoted `Packages-*` artifact then all agree on one sha with no change to the release workflow.
  Print the skipped kanban-only commits in the dry-run output.
- Alternative (only if tagging HEAD is required): tag HEAD and pass the green sha to `locate-run`;
  that changes the release pipeline and its tag-pin gate — justify before choosing it.
- Keep the other guards unchanged (dirty tree, not on master, out of sync, tag exists, published).
- The allowed tail paths are `kanban/` only — not configurable in this task.

## Checklist

- [ ] Guard walks back over a kanban-only tail to the newest green CI commit
- [ ] Non-kanban path in the tail refuses and names the path
- [ ] Tag targets the green commit; dry-run shows target sha and skipped commits
- [ ] `release-01-guard-matrix` covers: green HEAD, kanban-only tail, mixed tail, no green run
- [ ] tw-release skill / `releasing.md` note the kanban-only tail rule

## Session

- Created: 3be2678b-2a8c-49e5-83cd-2f1aafa278aa (2026-10-27)

## Notes

Steve 2026-10-27: "HOW is Directory.Build.props change considered Kanban only????????" → "yes create the task".

## Closed

- 2026-10-30: archived as a duplicate of 543 (PR #371, merged at 83dbe451). `dev release` now tags the
  newest green first-parent commit when every later commit changes only `kanban/`, and any other path
  refuses, naming the commit and path. The dry run prints the count of skipped board commits, not each
  one. The `tw-release` skill note goes to timewarp-flow. Steve "yes" 2026-10-30.
