# dev release accepts the last green CI run when later commits only touch kanban

## Description

`dev release` (`source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`, step 8)
refuses unless a successful `workflow.yml` run exists for the exact HEAD commit:

```
Error: no successful CI run of workflow.yml exists for <sha>. The Release event would fail at locate-run ...
```

Kanban board commits (`ganda kanban publish`, `land kanban NNN`, task done/gc) land on master
after a feature merge and change only `kanban/`. CI does not run for them, so HEAD has no run,
and the release refuses even though the last code commit is green. Today the workaround is a
manual `gh workflow run workflow.yml --ref master -f mode=merge`, which rebuilds byte-identical
packages only to satisfy the check.

Seen in timewarp-architecture on 2026-10-30: PR #616 merged as `f5f735c4b` with green CI, then
three `land kanban` commits followed. Releasing 2.0.0-beta.29 needed a manual CI run on
`3cb8e87c3`.

## Goal

When every commit between the latest successful CI run on master and HEAD changes only paths
under `kanban/`, `dev release` releases that green run's commit (and its `Packages-*` artifact)
without a new CI run. Any non-`kanban/` change after the green run keeps today's refusal.

## Open question (Steve decides)

Which commit gets the tag:

1. **Tag the green commit.** `dev release` tags the last green commit, not HEAD. The workflow's
   locate-run and tag-pin gates keep matching the tag sha as they do now. The tag sits a few
   board commits behind master.
2. **Tag HEAD.** The workflow's locate-run walks back from the tag sha over `kanban/`-only
   commits to the green run. This changes the release workflow, and every repo that mirrors
   `workflow.yml` would need the same change.

If the answer needs changes outside this repo, stop and report them. Do not edit other repos.

## Checklist

- [ ] Decision on the open question recorded here
- [ ] `dev release` (and `--dry-run`) find the last green run and accept `kanban/`-only commits after it
- [ ] Any other path change after the green run still refuses, naming the commit and paths
- [ ] Dry-run output says which commit is released and which board commits it skipped
- [ ] Tests cover: HEAD green; kanban-only commits after green; a code commit after green
- [ ] Release workflow gates agree with the chosen tag commit

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-30), filed from the timewarp-architecture
  session at Steve's request.

## Notes

- Guard text and the `gh run list --commit <headSha> --status success` call: `release-command.cs`
  around step 8. `CiRunPromotion.OrderCandidateRuns` picks among candidate runs.
- Release workflow: `.github/workflows/workflow.yml` (locate-run treats a successful push run at
  the tag sha as tested, with a `Packages-*` artifact).
