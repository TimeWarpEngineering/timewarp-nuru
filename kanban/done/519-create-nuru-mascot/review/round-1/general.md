# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** `master..task/519-create-nuru-mascot` (aaad9047): `assets/mascot/*.png`, `readme.md`, task file.

## Summary

The change copies five mascot PNGs into `assets/mascot/` and adds a centered hero image at the top of `readme.md`. I checked every claim in Results. All five files match `/home/steve/masoko/mascot-kit/` byte for byte (`cmp` exit 0). The SHA-256 digests and dimensions match Results. The README diff only adds lines, and the first line is the requested ASCII-only snippet. The hero shows the shield, the green scarf, and a single chest letter N. Checklist items 1 to 5 are ticked and Steven's approval is still open. No product edits fall outside `assets/mascot/`, `readme.md`, and the task file. The one risk: the root `readme.md` also ships as the NuGet package readme, and the new image uses a relative path.

## Issues

### Issue 1 — Severity: suggestion
- File: readme.md:1
- Description: The root `readme.md` is the NuGet package readme. `source/Directory.Build.props:23` sets `<PackageReadmeFile>readme.md</PackageReadmeFile>`, and `source/timewarp-nuru/timewarp-nuru.csproj:108`, `source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj:79`, and `source/timewarp-nuru-devcli/timewarp-nuru-devcli.csproj:36` pack `$(RepositoryRoot)readme.md`. The new hero uses the relative path `src="assets/mascot/nuru-hero.png"`. nuget.org does not resolve relative image paths, and the PNG is not packed. After the next release, the TimeWarp.Nuru, analyzers, and dev CLI package pages would open with a broken image. It is the first line of the page. Every other image in the README uses an absolute URL from an allowed host (shields.io, deepwiki).
- Suggestion: Keep the same markup and change `src` to an absolute URL that nuget.org accepts, such as `https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png`. GitHub and nuget.org both render that URL once the branch is merged. The task's snippet was marked "for example", so the README edit stays within scope.
- Status: open
