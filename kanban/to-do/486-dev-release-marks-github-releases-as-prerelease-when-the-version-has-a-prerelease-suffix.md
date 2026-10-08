# dev release marks GitHub Releases as prerelease when the version has a prerelease suffix

## Description

NuGet derives prerelease status from the version string (`3.0.0-beta.79` is prerelease on
nuget.org). `dev release` does not: it runs
`gh release create {tag} --title {tag} --generate-notes --verify-tag` with no `--prerelease`,
so every beta cut through it since beta.74 is a full GitHub Release and `v3.0.0-beta.79` currently
carries the **Latest** badge. Betas created by hand before `dev release` existed are a mix.

Make the GitHub Release flag follow the version the same way NuGet does, so the two always align
going forward: when `<Version>` in `source/Directory.Build.props` has a SemVer prerelease suffix
(anything after a `-`), pass `--prerelease`; otherwise do not.

## Requirements

- Decision is derived from the version already read in `release-command.cs` (`PropsVersionReader`),
  not re-typed. Use `NuGet.Versioning.NuGetVersion.IsPrerelease` if the package is already referenced
  (`NuGetVersionService` exists); otherwise a plain `-` check on the version string is acceptable.
- `--dry-run` prints the exact command that will run, including `--prerelease` when applicable
  (`releaseCommand` string and the executed `WithArguments(...)` must not drift).
- Do not retroactively edit existing releases on GitHub in this task; note the command to do so
  (`gh release edit vX --prerelease`) in Results for the maintainer.
- Docs that quote the command must match: `source/timewarp-nuru-devcli/readme.md` (~line 246) and
  `documentation/developer/guides/releasing.md` (step 3, ~line 95). Add one line to the guide's
  "Prerelease versions go through the same pipeline" bullet saying the GitHub Release flag mirrors
  the suffix.

## Checklist

- [ ] `release-command.cs`: `--prerelease` appended to both the printed `releaseCommand` and the executed `gh` arguments when the version is prerelease
- [ ] `tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs` (or a new `release-02-*.cs`) covers: `3.0.0-beta.79` → `--prerelease` present; `3.0.0` → absent; dry-run output matches
- [ ] `source/timewarp-nuru-devcli/readme.md` and `documentation/developer/guides/releasing.md` updated
- [ ] `changelog.md` Unreleased: DevCli entry
- [ ] PR merged

## Notes

- Found 2026-10-08 after cutting beta.79 (task 483). nuget.org side was always correct.
- Existing releases: `v3.0.0-beta.74`–`beta.79` are flagged as full releases on GitHub. Whether to
  flip them with `gh release edit` is a one-off maintainer action, not part of this change.

## Session

- Created: claude 2412bd45 (2026-10-08)
