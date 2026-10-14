# Round 3 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

- M1–M6 (rounds 1–2): still fixed. The beta.80 delta does not reopen them. Results still lists those corrections under "Earlier corrections that still hold".

## Issues

### M7 — Severity: bug — Status: fixed
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/x-thread.md:27 (also `documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md:122`, `channels.md:85-86`, `assets/readme.md:50`, `task.md` re-verify block)
- Description: The launch AOT figures (6.0 MB, ~3 ms / ~6 ms) were measured on published `3.0.0-beta.80`. Kanban 503 proved that package is a Debug build (DisableOptimizations). `3.0.0` will be Release-built with stable Mediator and Amuru dependencies. The diff also removed the post-6 "re-measure on the day" instruction. The release-day list now recaptures only `shot-4.png` and the video.
- Suggestion: Add a release-day AOT re-measure on `3.0.0`, recapture `shot-6.png`, and update the blog, r/dotnet and post 6 to match. Restore the post-6 re-measure note. In `task.md`, scope "bit-identical" to the API and say that beta.80 is Debug-built.
- Source: general
- Disposition notes:
- Fixed: 2026-10-14 fix pass (this branch). Release-day re-measure step added to `task.md` Launch, `assets/readme.md`, `demo/readme.md`, and the post-6 note in `x-thread.md`; "bit-identical" scoped to the API in `task.md`; blog, r/dotnet and post 6 flagged as Debug-build figures to refresh.

### M8 — Severity: suggestion — Status: fixed
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/x-thread.md:28 (vs blog `:122`, `channels.md:86`)
- Description: Thread post 6 says ~6 ms (one VHS frame under recorder load). The blog and r/dotnet say ~3 ms (quiet median) for the same binary. These ship the same day.
- Suggestion: Use one figure everywhere. Retake `shot-6.png` so the frame reflects the quiet result, or caption the median. Do this with the M7 retake.
- Source: general
- Disposition notes:
- Fixed: 2026-10-14 fix pass. Post 6 now says "single-digit milliseconds" (no figure that conflicts with `shot-6.png`); blog and r/dotnet keep the quiet ~3 ms median. `shot-6.png` was not recaptured: VHS retakes still read 5 to 6 ms because the recorder loads the machine.

## Duplicates / conflicts

- None. M8 is separate from M7: it applies even if the 3.0.0 numbers turn out the same as beta.80.
