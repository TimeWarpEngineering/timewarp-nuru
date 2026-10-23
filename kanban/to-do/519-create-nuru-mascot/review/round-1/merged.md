# Round 1 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: readme.md:1
- Description: The root `readme.md` is packed as the NuGet readme (`source/Directory.Build.props:23`; `timewarp-nuru`, `timewarp-nuru-analyzers`, `timewarp-nuru-devcli` csproj files). The new hero uses a relative `src`, which nuget.org cannot resolve, so the package pages would open with a broken image.
- Suggestion: Point `src` at `https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png`. Keep the rest of the line unchanged.
- Source: general
- Disposition notes: Fixed (implement-fix-2). The hero `src` is `https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png`. Alt text and width are unchanged. The raw URL 404s until this branch merges to master; that is the URL nuget.org can load after merge.

## Duplicates / conflicts

- None (single reviewer).

## Verified, no finding

- The five PNGs match the mascot kit byte for byte. Dimensions: 1024x1024 for each of the three heroes (RGB, RGB, RGBA), 1200x630 for the OG card, and 1920x1080 for the end card. The SHA-256 digests match Results.
- The README diff only adds lines. The new line is plain ASCII.
- Checklist items 1 to 5 are ticked. "Steven approves the final art" is still unticked.
- The diff touches only `assets/mascot/`, `readme.md`, and the task file.
- The goal restatement matches the task and its implementation notes, so no misread was found.
