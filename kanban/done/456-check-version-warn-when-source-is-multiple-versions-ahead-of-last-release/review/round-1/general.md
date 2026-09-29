# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** branch `task/456-check-version-warn-when-source-is-multiple-version` vs `origin/master` — `PrereleaseDistance`, `CheckVersionCommand` (`--strict`, distance line, warning), DevCli readme, check-version-07/08 tests, and the ci-tests compile/exclude wiring.

## Summary

`PrereleaseDistance.TryGetIncrements` returns a number only when both sides are `{core}-{label}.{number}` and `NuGetVersionService.CompareVersions` says the cores match (so `2.0` and `2.0.0` match). A core change, a different label, a stable version, extra prerelease identifiers, build-metadata-only noise, a missing latest version, or a numeric identifier that does not fit in `int` returns null. The warning count is distance minus one, which matches the incident shape (beta.9 published, beta.14 in source → distance 5, warning 4). `IsStrictFailure` is `increments > 1`, so distance 0, 1, negative (source behind), and null stay advisory. `CheckVersionCommand` prints the distance line after the two version lines, keeps the existing already-released exit 1, and sets exit 1 for `--strict` only after that classification. The release workflow still constructs `new CheckVersionCommand()` with `Strict` left false, so a deliberate jump warns and the pipeline continues. `check-version --help` lists `--strict`. Re-ran `check-version-08` (14 passed) and `check-version-07` (7 passed). No bugs, suggestions, or nits.

## Issues

<!-- none -->
