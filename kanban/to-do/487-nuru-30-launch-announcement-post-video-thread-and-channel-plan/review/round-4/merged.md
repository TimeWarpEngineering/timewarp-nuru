# Round 4 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

Rollup of every finding raised across rounds 1–4 (M1–M8), final status:

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1–M6 (rounds 1–2): still fixed.
- M7 (bug, round 3): fixed in `76150f37`. Re-verified: the release-day AOT re-measure on `3.0.0` is in the `task.md` Launch checklist, `assets/readme.md`, `demo/readme.md`, the post-6 note, the r/dotnet draft note and a comment beside the blog bullet. "bit-identical" is now "API-identical", and the text says beta.80 is Debug-built.
- M8 (suggestion, round 3): fixed in `76150f37`. Re-verified: post 6 says "single-digit milliseconds" (190 chars), which matches both the 6 ms VHS frame and the 2.52 ms quiet median. The blog and r/dotnet both say ~3 ms. Public copy has no `~6 ms` left.

## Issues

None new.

## Duplicates / conflicts

- None.
