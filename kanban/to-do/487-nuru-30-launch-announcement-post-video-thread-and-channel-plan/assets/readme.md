# Launch assets

Video and hero were produced 2026-10-09 against 3.0.0-beta.79. Thread screenshots were produced
2026-10-14 against published nuget.org `TimeWarp.Nuru` 3.0.0-beta.80.

| File | What | How it was made |
|------|------|-----------------|
| `nuru-3-launch.mp4` | 54 s terminal screencast, 1600×900, native upload for X / YouTube Shorts / blog | `vhs nuru-3-launch.tape` (VHS v0.10, Catppuccin Mocha). Replace `<demo-dir>` with a folder holding `app.cs` (= `../demo/app.cs.txt`), `app.full.cs` (same file, beta.79 workaround line kept), and the AOT binary `app`. The dir must be a git repo (`git init`) to silence TimeWarp.Build.Tasks, and bash-completion must be installed. |
| `nuru-3-launch.tape` | The VHS script | Mirrors `../video-script.md` beat for beat. |
| `nuru-3-hero.jpg` | Blog cover, 1600×900, 193 KB | Grok Imagine (`grok-imagine-image-quality`, 16:9) from the prompt in `../hero-image.md`, upscaled from 1280×720 with ffmpeg lanczos. Goes beside the stub in TheFreezeTeamBlog as `Image: nuru-3-hero.jpg`. |
| `nuru-3-og.jpg` | 1200×630 OpenGraph / LinkedIn crop of the hero | ffmpeg crop |
| `nuru-3-hero-alt.jpg` | Second Grok candidate, 1280×720 | Keep for the day-7 process-story post or the dev.to cover |
| `shot-2.png` … `shot-6.png` | Thread posts 2–6, 1600×900, Catppuccin Mocha | VHS v0.10. `Screenshot` did not write a file here, so each png is the last GIF frame (`ffmpeg`). `shot-5.png` is the top 900 px of a 4600 px frame so the REPL session and the start of the Emacs profile both fit. Tapes: `shot-2.tape` … `shot-6.tape`. |

`shot-2.png` filters `--capabilities` to the deploy endpoint plus `invocation` (the full document is 51 lines). The filter used at capture time:

```python
import json, sys
data = json.load(sys.stdin)
data["endpoints"] = [item for item in data["endpoints"] if item["pattern"].startswith("deploy")]
print(json.dumps(data, indent=2))
```

Publish the demo outside a git repo before shooting capabilities or the AOT binary. A git checkout stamps `version` as `1.0.0+<commit>`, and this machine's global NuGet cache still holds a local task-488 pack of `3.0.0-beta.80` that is not the nuget.org package. The 2026-10-14 shots used `NUGET_PACKAGES` pointed at a clean folder and a non-git publish directory.

Measured 2026-10-14 on that binary: `ls -lh` prints **6.0M** (6,187,808 bytes, 5.90 MiB). A quiet `time ./app greet x` prints **0.003s**. Median of 20 runs: **2.52 ms**. The VHS frame `shot-6.png` reads **0.006s**; the thread post matches the frame (`~6 ms`). The blog and the r/dotnet draft keep **~3 ms**.

The 2026-10-09 recording measured 5.9 MB and a 2.7 ms median (`time` showed 6 ms). The mp4 is still that take.

Re-record on release day: in `nuru-3-launch.tape` change `TimeWarp.Nuru@3.0.0-beta.79` to `@3.0.0`, drop the `app.full.cs` swap (launch gate #1 fixed means the shown file runs as-is), and recapture `shot-4.png` after `demo/app.cs.txt` pins `3.0.0`. VHS on this machine needs a Chrome binary. `~/.local/bin/google-chrome` points at a missing Playwright Chromium; prepend a wrapper that execs `chromium-1243` (or whatever is installed) with `--no-sandbox`.
