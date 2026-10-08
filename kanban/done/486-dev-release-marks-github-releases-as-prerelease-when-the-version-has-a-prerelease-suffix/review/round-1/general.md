# Round 1 — general
**Date:** 2026-10-08
**Scope reviewed:** branch `task/486-dev-release-marks-github-releases-as-prerelease-wh` vs `origin/master` (merge-base `3b3cc5da`). Product commits `7eaece03` and `184edebb`. Files: `source/timewarp-nuru-devcli/content/any/services/release-guard.cs`, `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`, `tests/timewarp-nuru-tests/devcli/release-02-prerelease-flag.cs`, `source/timewarp-nuru-devcli/readme.md`, `documentation/developer/guides/releasing.md`, `changelog.md`.

## Summary

`dev release` now decides `--prerelease` from the `<Version>` already returned by `PropsVersionReader` (`3.0.0-beta.79` today). `ReleaseGuard.IsPrerelease` treats a `-` before any `+` build metadata as prerelease, which matches `NuGetVersionService.GetPreRelease` for real SemVer strings; `NuGet.Versioning` is not referenced by this source-only package. Dry-run, the post-push recovery line, and `Shell.Builder("gh").WithArguments(...)` all use one argument list, so the printed command and the executed argv cannot drift. Docs (readme command, releasing guide step 3, and the prerelease bullet) and the Unreleased DevCli changelog entry match that behavior. `release-02-prerelease-flag.cs` passed 6/6, including beta vs stable and a hyphen that appears only in build metadata. Risk is low.

## Issues

<!-- none -->
