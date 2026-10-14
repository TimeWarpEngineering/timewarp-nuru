# Round 3 — general
**Date:** 2026-10-14
**Scope reviewed:** branch `task/487-nuru-30-launch-announcement-post-video-thread-and` vs `origin/master` (merge-base `27ed3d92`), commits `70aec67b` and `94e68fd2`: the beta.80 re-verify pass. 18 files, 207 insertions, 66 deletions (273 lines). This includes the blog one-liner, `assets/` (five new png and tape files plus the readme), `channels.md`, `demo/readme.md`, `release-notes.md`, `task.md`, `video-script.md`, and `x-thread.md`.

## Summary

This pass moves the launch kit from beta.79 to the published nuget.org `3.0.0-beta.80`. It ticks the shipped launch gates, captures `shot-2.png` … `shot-6.png`, and re-measures the AOT binary (6.0 MB, ~3 ms quiet, 0.006 s in the VHS frame). I checked each screenshot against `x-thread.md` and the claimed outputs. The migration guide gate holds: line 58 and later describe a flat `endpoints` list, and the guide no longer names beta.79. No launch copy tells readers to add `InterceptorsNamespaces`. The edited posts keep the same character counts. Most of the risk is in the AOT numbers. They now appear in the blog, the r/dotnet draft and thread post 6, but they were measured on a package that kanban 503 showed was a Debug build. The diff also drops the step that told the release-day operator to re-measure.

## Issues

### Issue 1 — Severity: bug
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/x-thread.md:27
- Description: The public AOT figures (6.0 MB, ~3 ms in the blog at `documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md:122` and in `channels.md:85-86`, ~6 ms in thread post 6) come from published `3.0.0-beta.80`. Kanban 503 (`kanban/done/503-…/task.md:185-192`) found that published beta.79 and beta.80 ship `AssemblyConfiguration("Debug")` with `DebuggableAttribute` 0x107 (DisableOptimizations). `3.0.0` will be the first Release-built package, and it also changes the Mediator and Amuru dependencies from beta to stable, which `release-notes.md:9-14` itself records. The launch numbers therefore describe a different build from the one being announced. This diff also removes the only release-day re-measure instruction ("Re-measure on the day and keep the text equal to the screenshot" in the old `x-thread.md` post-6 note). The new release-day list in `assets/readme.md:50` recaptures only `shot-4.png` and the video. The re-verify block in `task.md` treats beta.80 as final ("bit-identical in API"). That holds for the API, but not for build configuration, dependencies, or binary size and startup.
- Suggestion: Add a release-day step to `assets/readme.md` (and the task's Launch prep): publish `demo/app.cs.txt` pinned to `3.0.0`, re-measure `ls -lh` and median startup, recapture `shot-6.png`, and update the blog, the r/dotnet draft and thread post 6 to match. Restore the "re-measure on the day" note on post 6. In `task.md`, scope the "bit-identical" sentence to the API and say that beta.80 is a Debug build.
- Status: open

### Issue 2 — Severity: suggestion
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/x-thread.md:28
- Description: Copy that ships on the same day gives two startup times for the same binary. Thread post 6 says "~6 ms" because the VHS frame read `0.006s` under recorder load. The blog (`2026-xx-xx-nuru-3.0-release.md:122`) and the r/dotnet draft (`channels.md:86`) say "~3 ms", the quiet-shell median of 2.52 ms. The implementer chose this on purpose and documented it (`video-script.md:24-26`, `assets/readme.md:44`). Readers who follow the thread link to the blog will still see the claim halve. Of the two, the slower number goes in the higher-reach channel.
- Suggestion: Use one figure everywhere. Retake `shot-6.png` until the frame shows the quiet-shell result (for example with `./app greet x` warmed and VHS idle, or several runs with the best shown). Otherwise caption post 6 with the median and say the frame is a single run. Fold this into the Issue 1 release-day retake.
- Status: open

## Checked, no issue

- `shot-2.png`: `--capabilities` filtered to `deploy` plus `invocation`. The `invocation` values match Results. The `| python3 one-endpoint.py` pipe is visible, which is honest. The filter source is in `assets/readme.md`.
- `shot-3.png`: the `--json-args` success line and the `Unknown key 'bogus' … Known names: env, tag.` error match the readme and Results.
- `shot-4.png`: `#:package TimeWarp.Nuru@3.0.0-beta.80` with no `#:property` line. Release-day recapture is recorded.
- `shot-5.png`: `-i` REPL, `greet`, `deploy --tag`, and `key-bindings --profile Emacs` match the post 5 brief.
- `shot-6.png`: `6.0M` and `real 0m0.006s` match the post 6 brief.
- "6.0 MB": `ls -lh` rounds 6,187,808 bytes (5.90 MiB) up to `6.0M`. That is what a reader reproducing the step will see, so I did not raise it.
- Migration guide gate tick: the guide no longer says beta.79, and it describes the flat `endpoints` list (line 58 and later).
- Post character counts: posts 4 and 6 were edited with the same lengths. Every post is ≤ 280.
