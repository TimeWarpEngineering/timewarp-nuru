# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** `git diff master...HEAD` (Directory.Packages.props, changelog.md,
documentation/user/guides/migrating-to-3.0.md, kanban 487 release-notes.md and task.md, kanban 502 task.md)

## Summary

The pin bump is correct. nuget.org lists `2.0.0` for both `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools`, and both
`PackageVersion` lines are `2.0.0`. No stale `2.0.0-beta.2` pin remains outside `kanban/`. The risk is in the
docs. All four doc edits say that every published 3.0 beta through beta.80 depends on Amuru 2.0.0-beta.2. The
published nuspecs on nuget.org show that only beta.80 does.

## Issues

### Issue 1 — Severity: bug
- File: changelog.md:13; documentation/user/guides/migrating-to-3.0.md:48-49;
  kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/release-notes.md:54-55;
  kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/task.md:66
- Description: Each file says "(Published) betas through 3.0.0-beta.80 depend on (`TimeWarp.Amuru`) 2.0.0-beta.2".
  That is false. The `timewarp.nuru.nuspec` files on nuget.org show:
  beta.80 → `2.0.0-beta.2`; beta.79 → `1.1.1`; beta.72–76 → `1.0.0`; beta.70–71 → `1.0.0-beta.32`;
  beta.68 → `1.0.0-beta.25`; beta.64–67 → `1.0.0-beta.22`; beta.63 → `1.0.0-beta.21`; beta.58–62 → `1.0.0-beta.17`;
  beta.53–57 have no Amuru dependency. Nuru 2.0.0 has no Amuru dependency either. The error came from the 502
  brief (Scope step 4), and the implementer copied it.
  Check with: `curl -s https://api.nuget.org/v3-flatcontainer/timewarp.nuru/3.0.0-beta.79/timewarp.nuru.nuspec | grep Amuru`.
- Suggestion: Name only the beta that is wrong. For example: "3.0.0-beta.80 depends on `TimeWarp.Amuru`
  2.0.0-beta.2; earlier 3.0 betas depend on Amuru 1.x." Make the same edit in all four files and in the 502
  `task.md` Results ("Docs updated" and "How to validate / Expect").
- Status: open

### Issue 2 — Severity: suggestion
- File: documentation/user/guides/migrating-to-3.0.md:46-54
- Description: This guide is for 2.x users. Nuru 2.0.0 has no Amuru dependency, so 3.0 adds `TimeWarp.Amuru` as a
  new transitive dependency. The section reads as if Amuru only moved from a beta to stable. A 2.x reader is not
  told that they now get a new package.
- Suggestion: Add one sentence such as "Nuru 2.x did not depend on Amuru; 3.0 adds it." You can fold this into
  the Issue 1 rewrite.
- Status: open
