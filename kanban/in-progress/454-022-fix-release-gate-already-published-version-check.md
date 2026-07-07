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

### Answers (reviewer, 2026-07-07)

**A1 → (b) hand-fix, with one scope correction.** Do NOT treat dot-separated
pre-release identifiers as an exotic edge to skip — they are THE common case in this
ecosystem: the repo's own history is `1.0.0-beta.32`, `3.0.0-beta.71`, Jaribu
`1.0.0-beta.13`. A release gate that ranks `beta.9 > beta.10` (lexical) will
green-light or block the wrong releases here. Required precedence rules (~30 lines,
straight from SemVer 2.0 §11):
- release > same-version pre-release (`1.0.0` > `1.0.0-beta`)
- pre-release identifiers split on `.`; numeric identifiers compare numerically,
  alphanumeric lexically (ordinal); numeric < alphanumeric
- when one identifier list is a prefix of the other, shorter < longer
- build metadata (`+...`) stripped/ignored for both comparison and membership
- compare case-insensitively (NuGet treats `1.0.0-Beta` == `1.0.0-beta`); tolerate a
  missing/4th version part as 0 (NuGet normalization)
Test cases: `1.0.0-beta < 1.0.0`, `1.0.0-beta.2 < 1.0.0-beta.10`,
`1.0.0-alpha < 1.0.0-beta`, `1.0.0-beta < 1.0.0-beta.1`, `1.2.0+build == 1.2.0`,
`1.0.0-BETA == 1.0.0-beta`.

**A2 → (c) modified: compile ONLY the service file into CI, never the endpoint file.**
`check-version-command.cs` carries `[NuruRoute("check-version")]` whose handler
requires `IRepoConfigService`. Endpoints are collected GLOBALLY in the multi-mode
compilation, so including that file gives every unfiltered `.DiscoverEndpoints()`
test app an endpoint with an unregistered service → NURU050 build errors across CI
(verified failure mode: Gen20KanbanQuery in 454-001, 50 errors). So:
- `<Compile Include="...devcli/content/any/services/nuget-version-service.cs" />` in
  tests/ci-tests/Directory.Build.props (plain service class, `namespace DevCli`, safe)
- tests in `tests/timewarp-nuru-tests/devcli/` (already globbed — CI count must rise)
- the endpoint file stays uncompiled in CI; its logic becomes testable via A3

**A3 → (a) extract, but INTO nuget-version-service.cs** (not a new file, and not a
helper left in the command file): e.g.
`public static bool IsVersionPublished(string sourceVersion, IEnumerable<string> publishedVersions)`
using the fixed comparer. Reasons: (1) the command file can't be compiled into CI
(A2), so logic left there is untestable; (2) a new content file would require a
`build/TimeWarp.Nuru.DevCli.props` glob check — services/*.cs is auto-included, so
adding to the existing service file is zero packaging risk for downstream repos
(Terminal, Ganda, Amuru, Builder). `HandleNuGetSearchAsync` then calls it with the
FULL versions list (replacing the `versions[^1]` comparison).

### Finalized Implementation Plan (2026-07-07)

#### Files to modify
1. `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs` — rewrite `CompareVersions` with full SemVer 2.0 §11, add `IsVersionPublished` + 5 private helpers
2. `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs` — line 168: replace `string.Equals(version, highestVersion, ...)` with `NuGetVersionService.IsVersionPublished(version, versions)`
3. `tests/ci-tests/Directory.Build.props` — add `<Compile Include>` for 5 service files ONLY (not the endpoint file)
4. `tests/timewarp-nuru-tests/devcli/check-version-01-version-comparison.cs` — new, 11 tests

#### Step 1: Rewrite CompareVersions (nuget-version-service.cs)
Full SemVer 2.0 §11: strip build metadata (`+...`), split pre-release on `.`, numeric identifiers compare numerically (beta.9 < beta.10), alphanumeric lexically ordinal case-insensitive, numeric < alphanumeric, prefix rule (shorter < longer), release > same-version pre-release, missing parts normalized to 0. Add 5 private helpers: `GetCoreVersion`, `GetPreRelease`, `CompareCoreVersions`, `ComparePreRelease`, `IsNumeric`.

#### Step 2: Add IsVersionPublished (nuget-version-service.cs)
`public static bool IsVersionPublished(string sourceVersion, IEnumerable<string> publishedVersions)` — iterates full list, returns true if `CompareVersions(sourceVersion, published) == 0`. Uses `IEnumerable<string>` per A3.

#### Step 3: Fix HandleNuGetSearchAsync (check-version-command.cs)
Replace line 168 `string.Equals(version, highestVersion, ...)` with `NuGetVersionService.IsVersionPublished(version, versions)`. Keep `highestVersion` (line 161) and `latestNuGetVersion` tracking (line 163) — only the membership check changes.

#### Step 4: CI wiring (tests/ci-tests/Directory.Build.props)
Add `<Compile Include>` for EXACTLY these 5 files (explicit, not glob — git-tag-check-service.cs and repo-config-service.cs would pull unwanted deps):
- `nuget-version-service.cs`
- `dev-cli-json-context.cs`
- `repo-config.cs`
- `check-version-config.cs`
- `check-version-strategy.cs`
Do NOT compile `check-version-command.cs` — its `[NuruRoute]` + `IRepoConfigService` breaks every `.DiscoverEndpoints()` test in multi-mode (A2).

#### Step 5: Create test file (tests/timewarp-nuru-tests/devcli/)
11 tests: 7 CompareVersions (release > prerelease, numeric identifiers, alpha < beta, prefix rule, build metadata, case-insensitive, missing parts) + 4 IsVersionPublished (in list, not in list, prerelease in full list, build metadata ignored).

#### Step 6: Verify
1. `ganda runfile cache --clear`
2. `dotnet run tests/ci-tests/run-ci-tests.cs` — CI count must increase by 11 (the 454-023 lesson)
3. `dotnet run tests/timewarp-nuru-tests/devcli/check-version-01-version-comparison.cs` (standalone)

#### Risks
- `DevCliJsonContext` source generator must fire in CI assembly (SDK-builtin for .NET 10)
- `System.Linq` `.All(char.IsDigit)` — if unavailable, inline foreach loop
- CS1998 — every async test needs `await Task.CompletedTask;`
- Do NOT compile the endpoint file into CI

(End of file - total 104 lines)
