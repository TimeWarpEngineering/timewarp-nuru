# Round 4 — general
**Date:** 2026-10-14
**Scope reviewed:** fix commit `76150f37` (round-3 M7, M8): blog Native AOT bullet, `assets/readme.md`, `channels.md`, `demo/readme.md`, `task.md`, `video-script.md`, `x-thread.md`. Cross-checked against the full branch diff vs `origin/master` (merge-base `27ed3d92`).

## Summary

The fix pass adds a release-day AOT re-measure on Release-built `3.0.0` in every place a reader of the kitchen would look (Launch checklist, `assets/readme.md`, `demo/readme.md`, the post-6 note, the r/dotnet draft note and an HTML comment beside the blog bullet). It scopes "bit-identical" to the API and says beta.80 is a Debug build (kanban 503). Thread post 6 now says "single-digit milliseconds", which is true for both the VHS frame (6 ms) and the quiet median (2.52 ms). That removes the ~6 ms vs ~3 ms conflict without a retake. The blog and r/dotnet both still say ~3 ms. A grep finds no leftover `~6 ms` in public copy and no "bit-identical" anywhere. All eight thread posts are ≤ 280 chars; post 6 is 190.

M7 and M8 are fixed. No new issues.

Observation (not filed): the blog's `<!-- RELEASE DAY … -->` comment sits at column 0 between two list items. CommonMark ends the list there and starts a new one, and the comment would go into the published page source if nobody deletes it. The release-day step tells the editor to update that bullet, so the comment will be edited anyway. Nothing renders visibly, so this stays a note and not a finding.

## Issues

None.
