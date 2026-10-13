# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** `git diff master...HEAD` (head `d70da00b`): `Directory.Packages.props`, `changelog.md`,
`documentation/user/guides/migrating-to-3.0.md`, `source/timewarp-nuru/timewarp-nuru.csproj`, and kanban 487 `release-notes.md` / `task.md`.

## Summary

The change moves both Mediator pins from `14.0.0-beta.5` to stable `14.0.0`. Both versions exist on nuget.org (flat index checked).
No other `TimeWarp.Mediator` pin or `#:package` reference exists in the repo. No NU5104 suppression was added. The csproj
comment and changelog entry are accurate. Risk is low. The one defect is a factual error in the user-facing migration guide
about which published betas carry the prerelease Mediator dependency.

## Issues

### Issue 1 — Severity: bug
- File: documentation/user/guides/migrating-to-3.0.md:22
- Description: The guide says "Published 3.0 betas through 3.0.0-beta.79 reference `TimeWarp.Mediator` 14.0.0-beta.4. Later Nuru
  builds, including stable 3.0.0, reference stable `TimeWarp.Mediator` 14.0.0". That is false for `3.0.0-beta.80`, which is
  published on nuget.org. Its nuspec (`https://api.nuget.org/v3-flatcontainer/timewarp.nuru/3.0.0-beta.80/timewarp.nuru.nuspec`)
  depends on `TimeWarp.Mediator.Contracts` and `.Generators` at `14.0.0-beta.4`. The `<Version>3.0.0-beta.80</Version>` bump
  (`bf64306d`, 2026-10-11) predates the beta.5 pin (`c74bcdb4`, 2026-10-13). A beta.80 user who follows the guide and pins
  `TimeWarp.Mediator.*` at 14.0.0 gets a version mismatch the guide says cannot happen. The task Results bullet under
  "### Docs updated" (task.md:80-81) repeats the same "through 3.0.0-beta.79" claim.
- Suggestion: Change "through 3.0.0-beta.79" to "through 3.0.0-beta.80" in the guide and in the task.md Results bullet. The
  next published beta (beta.81 or later) is the first with stable Mediator. Optionally name that first version once it is cut.
- Status: open

### Issue 2 — Severity: nit
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/release-notes.md:5-7
- Description: The inserted pre-paste sentence is a single ~150-character line in an otherwise wrapped paragraph. The
  header still says "Every bullet here was verified against `3.0.0-beta.79`". The new Mediator bullet (line 48) is not true
  of beta.79, which depends on Mediator beta.4. The parenthetical covers this, but the header claim no longer holds.
- Suggestion: Rewrap the paragraph. Say the Mediator bullet is verified against the 14.0.0 pin rather than beta.79.
- Status: open
