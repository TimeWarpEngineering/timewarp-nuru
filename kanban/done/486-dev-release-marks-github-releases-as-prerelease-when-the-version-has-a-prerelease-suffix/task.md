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

- [x] `release-command.cs`: `--prerelease` appended to both the printed `releaseCommand` and the executed `gh` arguments when the version is prerelease
- [x] `tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs` (or a new `release-02-*.cs`) covers: `3.0.0-beta.79` → `--prerelease` present; `3.0.0` → absent; dry-run output matches
- [x] `source/timewarp-nuru-devcli/readme.md` and `documentation/developer/guides/releasing.md` updated
- [x] `changelog.md` Unreleased: DevCli entry
- [ ] PR merged

## Notes

- Found 2026-10-08 after cutting beta.79 (task 483). nuget.org side was always correct.
- Existing releases: `v3.0.0-beta.74`–`beta.79` are flagged as full releases on GitHub. Whether to
  flip them with `gh release edit` is a one-off maintainer action, not part of this change.
- 2026-10-08: review oracle (ganda task work, tw-implementation-review effort 1). Round 1 general — disposition clean. Next host nodes: open-pr / done (no apply-review sibling).

## Session

- Created: claude 2412bd45 (2026-10-08)
- Implement: claude (ganda task work, 2026-10-08)
- Review: grok `01a11c3f-1285-7b72-a806-f65957b8dd6c` (2026-10-08)
- Reviewer general: grok subagent `01a11c42-dfb4-7c70-abd0-131ee3ac6a7e` (2026-10-08)
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 80 — 2026-10-08T16:09:01Z

## Results

- `ReleaseGuard.IsPrerelease(version)` — SemVer check: a `-` before any `+build` metadata
  (NuGet.Versioning is not referenced by the source-only DevCli package, so plain string check).
- `ReleaseGuard.BuildReleaseCreateArguments(tag, version)` builds the gh argument list once
  (appends `--prerelease` for prerelease versions); `FormatReleaseCreateCommand` renders it.
  `release-command.cs` uses the same list for the `--dry-run`/recovery string and the executed
  `WithArguments([.. releaseCreateArguments])`, so they cannot drift.
- New test `tests/timewarp-nuru-tests/devcli/release-02-prerelease-flag.cs` (6 tests):
  `3.0.0-beta.79` → `--prerelease`; `3.0.0` → absent; `+build-7` metadata not prerelease;
  exact formatted command strings for both.
- Docs: `source/timewarp-nuru-devcli/readme.md`, `documentation/developer/guides/releasing.md`
  (step 3 + "Prerelease versions" bullet); `changelog.md` Unreleased DevCli entry.
- Existing releases were NOT edited. To flip the already-cut betas, the maintainer can run:
  `for v in 74 75 76 79; do gh release edit v3.0.0-beta.$v --prerelease; done`
  (adjust the list to whichever `v3.0.0-beta.*` releases exist; `gh release edit vX --prerelease`).

### How to validate

Smoke:
```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/devcli/release-02-prerelease-flag.cs
dotnet run tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs
# On a clean, synced master with a beta <Version> and green CI:
dotnet run tools/dev-cli/dev.cs -- release --dry-run
```

Expect: both test files pass (6/6, 22/22). The dry-run "Would run:" block ends with
`gh release create v3.0.0-beta.N --title v3.0.0-beta.N --generate-notes --verify-tag --prerelease`;
for a stable `<Version>` the line has no `--prerelease`.

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Final counts:** bug 0, suggestion 0, nit 0 (0 open, 0 fixed, 0 wontfix)
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified during review: `dotnet run tests/timewarp-nuru-tests/devcli/release-02-prerelease-flag.cs` passed 6/6. `dotnet run tests/timewarp-nuru-tests/devcli/release-01-guard-matrix.cs` passed 22/22.

