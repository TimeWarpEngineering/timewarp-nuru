# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** branch `task/543-dev-release-tags-the-newest-green-ci-commit-when-l` vs `master` (`a17d0a0b..03ff8dbb`)

## Summary

Step 8 walks first-parent history from HEAD, at most 50 commits, and tags the newest commit `CiRunPromotion.OrderCandidateRuns` accepts. Commits it passes must list only `kanban/` paths, `<Version>` at that commit must match the working tree, and the tag, dry-run lines, and "Target commit" use that sha. The pure classifier and its four required cases match the rule. The path list the walk feeds that classifier is not the commit's real path set when git reports a rename.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:565
- Description: The walk decides kanban-only from `git diff --name-only <sha>^1 <sha>`. With the default rename detection, that command prints only the post-image. Verified with git 2.53: `git mv source/a.cs kanban/a.cs` then `git diff --name-only HEAD^ HEAD` prints `kanban/a.cs` and nothing else. `FindShippingPath` then treats the commit as kanban-only, so a shipping file moved into `kanban/` does not stop the walk. `dev release` can tag the older green commit, whose tree still contains that file. The same invocation with `--no-renames` prints both `kanban/a.cs` and `source/a.cs`. The pure classifier already refuses `source/a.cs`; it never sees that path. The task checklist names `--name-only`, and that listing drops the pre-image, so the rule "every commit after the target changes only paths under `kanban/`" is not what the walk enforces.
- Suggestion: Pass `--no-renames` so both sides of a rename are in the path list `SelectReleaseTarget` already classifies. Keep the flag explicit so `diff.renames` in the operator's git config cannot turn the check back off.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:404
- Description: When the target is behind HEAD, a failing `git show <target>:source/Directory.Build.props` leaves `targetVersion` null and discards stderr. `ReleaseGuard.CheckTargetVersion` then reports `<Version> at release target … is (unreadable) … the commits between them are not kanban-only. Wait for CI on HEAD.` (`release-guard.cs:273`). A git failure and an unparseable props file take the same sentence. The release still refuses. The recovery the sentence names does not match a failed `git show` or unparseable XML, which is the failure this change already hit before the BOM trim.
- Suggestion: On a non-zero `git show`, print stderr and refuse with that git error, the same way the other git steps in this command do. When parse throws, say the props file at the target is unreadable. Keep the version-mismatch sentence for the case where both reads return different versions.
- Status: open
