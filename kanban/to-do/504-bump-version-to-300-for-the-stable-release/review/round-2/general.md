# Round 2 — general
**Date:** 2026-10-14
**Scope reviewed:** fix delta `89a3351a..b007143a` (commit b007143a "docs: point 3.0 readers at the real changelog range"): `changelog.md` (`[3.0.0]` lead, line 12) and `documentation/user/guides/migrating-to-3.0.md` ("What did not move", lines 177-180). Kitchen files excluded.

## Summary

The fix pass rewrote the migration guide's changelog pointer and the `[3.0.0]` lead paragraph. Both now
match the changelog. The prose-only delta has no new defects and adds no trailing whitespace
(`git diff --check` is clean).

## Re-verification of round 1

- **M1 (bug) — fixed.** The guide now says `changelog.md` has a section for each published 3.0 beta
  through `3.0.0-beta.79`, that `## [3.0.0]` lists changes since beta.79 (including beta.80's), and that
  later work is under `## [Unreleased]`. The "were not kept" sentence is gone. Checked against the
  changelog headings. Every published beta from beta.2 through beta.79 has a heading, some shared (for
  example beta.66/67, beta.53/54, and beta.39/40/41). Unpublished betas are marked "not published" (77,
  78, 69) or folded into the next section (beta.21 into beta.22). No `refs/tags` entry exists for beta.1,
  beta.16, or beta.21.
- **M2 (nit) — fixed.** The `[3.0.0]` lead opens with "Changes since `3.0.0-beta.79`; beta.80 has no
  separate section." It points at "the first Packaging entry under Fixed, task 503", and the first
  `**Packaging**` bullet under `### Fixed` (changelog.md:21) is the 503 entry.

## Issues

None.
