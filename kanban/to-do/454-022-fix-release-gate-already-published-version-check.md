# Fix Release Gate Already Published Version Check

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M22).

## Description

`source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs:161-171` —
`HandleNuGetSearchAsync` only compares the source version against `versions[^1]` (the
single highest published version) instead of testing membership in the full list.

Failure scenario: source `Version` is `1.2.0`, NuGet already has `[1.2.0, 1.3.0]`.
`highestVersion` is `1.3.0`, `1.2.0 != 1.3.0`, so `alreadyPublished` stays empty and the
tool reports "safe to release" even though 1.2.0 was already published. A release gate
that green-lights a duplicate publish defeats its purpose.

Related LOW in the same area (see also 454-031):
`source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:59-82` —
`CompareVersions` treats `1.0.0-beta`'s third part `0-beta` as `0` and the string
fallback then ranks `1.0.0-beta` NEWER than `1.0.0`. Wrong if reused beyond display.

## Checklist

- [ ] Test membership of source version in the full versions list
- [ ] Fix or replace CompareVersions with proper SemVer comparison (e.g. NuGetVersion)
- [ ] Tests: already-published-but-not-latest → flagged; pre-release ordering correct

## Notes

This file ships to downstream repos via the DevCli content package — coordinate version bump.

## Implementation guidance (reviewer, 2026-07-07)

- **Test wiring**: devcli content files (`source/timewarp-nuru-devcli/content/any/...`)
  are shipped source, not a compiled assembly. To test them in CI, add the specific
  files via `<Compile Include>` (or a small test shim) — and REGISTER whatever you add
  in `tests/ci-tests/Directory.Build.props`, then confirm the CI test COUNT increases
  (lesson from 454-023: green CI without the new tests counted proves nothing).
- **CompareVersions**: prefer a small, correct SemVer 2.0 comparer written INTO the
  content package over adding a NuGet.Versioning PackageReference — content-package
  source propagates to consumer repos, and a new package dependency there repeats the
  TimeWarp.Amuru.Tools split pain (consumers forced to add references). Test the
  comparer against: 1.0.0 > 1.0.0-beta; 1.0.0-beta.2 < 1.0.0-beta.10 (numeric ids);
  1.0.0-alpha < 1.0.0-beta (lexical); build metadata ignored.
- **The gate itself**: membership test against the FULL versions list, not versions[^1].
