# Launch assets (produced 2026-10-09 against 3.0.0-beta.79)

| File | What | How it was made |
|------|------|-----------------|
| `nuru-3-launch.mp4` | 54 s terminal screencast, 1600×900, native upload for X / YouTube Shorts / blog | `vhs nuru-3-launch.tape` (VHS v0.10, Catppuccin Mocha). Replace `<demo-dir>` with a folder holding `app.cs` (= `../demo/app.cs.txt`), `app.full.cs` (same file, beta.79 workaround line kept), and the AOT binary `app`. The dir must be a git repo (`git init`) to silence TimeWarp.Build.Tasks, and bash-completion must be installed. |
| `nuru-3-launch.tape` | The VHS script | Mirrors `../video-script.md` beat for beat. |
| `nuru-3-hero.jpg` | Blog cover, 1600×900, 193 KB | Grok Imagine (`grok-imagine-image-quality`, 16:9) from the prompt in `../hero-image.md`, upscaled from 1280×720 with ffmpeg lanczos. Goes beside the stub in TheFreezeTeamBlog as `Image: nuru-3-hero.jpg`. |
| `nuru-3-og.jpg` | 1200×630 OpenGraph / LinkedIn crop of the hero | ffmpeg crop |
| `nuru-3-hero-alt.jpg` | Second Grok candidate, 1280×720 | Keep for the day-7 process-story post or the dev.to cover |

Measured on this machine during recording: AOT binary **5.9 MB**, startup **2.7 ms median** (20 runs; `time` shows 6 ms). The thread and script said "~4 MB, ~8 ms"; use the measured numbers.

Re-record on release day: in `nuru-3-launch.tape` change `TimeWarp.Nuru@3.0.0-beta.79` to `@3.0.0`, drop the `app.full.cs` swap (launch gate #1 fixed means the shown file runs as-is), re-run.
