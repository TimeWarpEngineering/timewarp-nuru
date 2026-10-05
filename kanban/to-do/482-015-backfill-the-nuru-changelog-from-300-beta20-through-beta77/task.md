# Backfill the Nuru changelog from 3.0.0-beta.20 through beta.77

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding Changelog from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`changelog.md` stops at `3.0.0-beta.19` (2025-12-10). The package version is `3.0.0-beta.78`. beta.20 through beta.77 are not logged. Unreleased already points at the 3.0 migration guide and the mediator move. This parent task did not invent per-beta entries.

Write the missing dated sections from the git history and GitHub releases between beta.19 and beta.77. Do not fabricate entries. Where a beta has no user-facing change, say so in one line or group the quiet betas. Keep the Unreleased section for changes since beta.78.

Evidence: `changelog.md`, `source/Directory.Build.props`. Parent record: `review/findings.md` (changelog row) and `review/supporting.md` (checked notes).

## Checklist

- [x] `changelog.md` has a dated section for the span `3.0.0-beta.20` through `3.0.0-beta.77`, grounded in commits or GitHub releases
- [x] Unreleased still describes changes after beta.77, including the migration guide
- [x] No invented feature is attributed to a beta that did not ship it

## How to validate

Smoke:

```bash
rg -n "^## \[3\.0\.0-beta\.(2[0-9]|[3-6][0-9]|7[0-7])\]" changelog.md
```

Expect: The heading search shows the backfilled span. A reader can see what shipped after beta.19 without opening the commit log. Unreleased remains above those sections.

## Session

- Created: 539474 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

- Implemented: claude implement oracle (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.

## Results

`changelog.md` now has dated sections for 3.0.0-beta.20 through beta.77, inserted between Unreleased and beta.19 (51 headings). Each section comes from the GitHub release notes for that tag, checked against `git log` between adjacent tags. Facts found along the way:

- **Not published:** beta.21 (no tag, release, or NuGet package) and beta.77 (version set on 2026-08-27, bumped to beta.78 on 2026-09-25, never tagged). beta.21 is covered by a note under beta.22. beta.77 has a one-line section that points at Unreleased.
- **NuGet only:** beta.70 is on NuGet but has no tag or release. Its section says so and does not list contents.
- **Grouped betas:** these pairs and triples were re-releases or retries with the same notes: 26/27, 39/40/41, 47/48, 53/54, 66/67. Each group shares one heading.
- **Unreleased fix:** three Unreleased entries had already shipped in beta.42 (commit 1a45f3df): hierarchical `--capabilities` JSON, `GroupCapability`, and `GroupHierarchyBuilder`. They moved to beta.42, with a note that beta.56 replaced that structure.
- **Unreleased scope:** Unreleased now opens with a line saying it covers changes since beta.76, the last published beta. The migration guide and Mediator entries are unchanged.
- **Release notes vs. commits:** for beta.22 and beta.24, the release notes left out the large source-generator and fluent-API work. The changelog lists it from the commits on the tag ancestry (`v3.0.0-beta.20..v3.0.0-beta.22`, `v3.0.0-beta.23..v3.0.0-beta.24`).

### How to validate

Smoke:

```bash
rg -n "^## \[3\.0\.0-beta\.(2[0-9]|[3-6][0-9]|7[0-7])\]" changelog.md
gh release view v3.0.0-beta.56 --json body -q .body
```

Expect: the first command lists the backfilled headings from beta.77 down to beta.20, all below `## [Unreleased]` and above `## [3.0.0-beta.19]`. The beta.56 section matches the release notes (flat `Endpoints` / `EndpointCapability`). Unreleased no longer lists the beta.42 capabilities entries.
