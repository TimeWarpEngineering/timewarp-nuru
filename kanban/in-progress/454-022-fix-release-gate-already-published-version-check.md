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

### Downstream repo review (2026-07-07)

Reviewed how downstream repos (Terminal, Ganda, Amuru, Builder, Jaribu) consume the DevCli source-only package:

- `build/TimeWarp.Nuru.DevCli.props` auto-includes ALL `content/any/endpoints/*.cs` and `content/any/services/*.cs` into any project referencing the `TimeWarp.Nuru.DevCli` package
- Downstream repos put a `dev.cs` runfile in `tools/dev-cli/` calling `.DiscoverEndpoints()` — picks up `[NuruRoute]` endpoints from auto-included content files
- Content files compile directly into downstream projects, using `namespace DevCli` + global usings for `TimeWarp.Nuru`, `TimeWarp.Terminal`, etc.
- None of the downstream repos test `CompareVersions` or `check-version-command` directly — zero tests exist anywhere in the ecosystem
- The dev-cli project itself is source-only (`<Compile Remove="**/*.cs" />`) — never compiles its own content files
- Current DevCli package version: `3.0.0-beta.71` (`source/Directory.Build.props:16`)

### Design questions (awaiting user decision)

**Q1: NuGet.Versioning dependency — add it, or hand-fix CompareVersions?**

- (a) Add `NuGet.Versioning`: Use `NuGetVersion` for proper SemVer 2.0 comparison. Most correct, but adds a transitive dependency to every downstream consumer (Terminal, Ganda, Amuru, etc.). AOT-compatible, so no AOT break.
- (b) Hand-fix `CompareVersions`: Parse pre-release suffixes manually (e.g. `1.0.0-beta` → version parts `[1,0,0]` + pre-release `"beta"`). A version with a pre-release suffix is older than the same version without. No new dependency, preserves the "no NuGet.* deps" design intent. Slightly less correct for exotic SemVer edge cases (dot-separated pre-release identifiers like `1.0.0-beta.1` vs `1.0.0-beta.2`), but sufficient for a release gate.

Recommendation: (b) hand-fix — the existing code deliberately avoids NuGet dependencies, and a release gate doesn't need full SemVer 2.0 spec compliance. It needs: `1.0.0-beta` < `1.0.0`, and `1.2.0` is found in `[1.2.0, 1.3.0]`.

**Q2: Test infrastructure — how to compile the content files for testing?**

- (a) Standalone runfile (`tests/timewarp-nuru-devcli-tests/check-version-01-compare-versions.cs`): Use `#:project` to reference a project with the needed deps, `<Compile Include>` the content files. Doesn't run in CI (the 454-023 trap).
- (b) New test directory wired into CI: Create `tests/timewarp-nuru-devcli-tests/` with a `Directory.Build.props` that `<Compile Include>`s the content files + references needed packages, AND wire it into `tests/ci-tests/Directory.Build.props` (glob + ProjectReference). Verify CI count increases.
- (c) Compile content files into the existing CI test assembly: Add `<Compile Include>` for the devcli content files + needed `ProjectReference`s directly to `tests/ci-tests/Directory.Build.props`. Tests go in `tests/timewarp-nuru-tests/devcli/`. This uses the existing CI wiring (no new directory) — the 454-023 lesson is automatically satisfied since `timewarp-nuru-tests/**` is already globbed.

Recommendation: (c) — put tests in `tests/timewarp-nuru-tests/devcli/` (already in the CI glob) and add the devcli content `<Compile Include>` + `ProjectReference` to the CI `Directory.Build.props`. This avoids creating a new test directory (the 454-023 trap) and the tests are automatically in CI.

**Q3: Should I extract the membership check into a testable static helper?**

The membership check is embedded in `HandleNuGetSearchAsync` (a method with heavy dependencies: `ConfigService`, `NuGetVersionService`, `Terminal`). To test it without HTTP, extract `IsVersionAlreadyPublished(string sourceVersion, IReadOnlyList<string> publishedVersions)` as a static helper.

- (a) Yes, extract it — makes the logic testable without HTTP mocking
- (b) No, just fix it in-place — the pattern is simple enough (`versions.Any(v => CompareVersions(version, v) == 0)`)

Recommendation: (a) — extracting it makes the fix verifiable without network access, and it's a clean separation.
