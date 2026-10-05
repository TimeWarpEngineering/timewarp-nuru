# Backfill the Nuru changelog from 3.0.0-beta.20 through beta.77

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding Changelog from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`changelog.md` stops at `3.0.0-beta.19` (2025-12-10). The package version is `3.0.0-beta.78`. beta.20 through beta.77 are not logged. Unreleased already points at the 3.0 migration guide and the mediator move. This parent task did not invent per-beta entries.

Write the missing dated sections from the git history and GitHub releases between beta.19 and beta.77. Do not fabricate entries. Where a beta has no user-facing change, say so in one line or group the quiet betas. Keep the Unreleased section for changes since beta.78.

Evidence: `changelog.md`, `source/Directory.Build.props`. Parent record: `review/findings.md` (changelog row) and `review/supporting.md` (checked notes).

## Checklist

- [ ] `changelog.md` has a dated section for the span `3.0.0-beta.20` through `3.0.0-beta.77`, grounded in commits or GitHub releases
- [ ] Unreleased still describes changes after beta.77, including the migration guide
- [ ] No invented feature is attributed to a beta that did not ship it

## How to validate

Smoke:

```bash
rg -n "^## \[3\.0\.0-beta\.(2[0-9]|[3-6][0-9]|7[0-7])\]" changelog.md
```

Expect: The heading search shows the backfilled span. A reader can see what shipped after beta.19 without opening the commit log. Unreleased remains above those sections.

## Session

- Created: 539474 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
