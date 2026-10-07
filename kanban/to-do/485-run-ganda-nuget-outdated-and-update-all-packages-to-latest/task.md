# Run ganda nuget outdated and update all packages to latest

## Description

Bring every NuGet reference in the repo to its latest stable version using `ganda nuget outdated`.
Snapshot on 2026-10-07 at master `fd5f4635` (57 packages checked, 6 outdated):

| Package | Current | Latest | Type |
|---------|---------|--------|------|
| Microsoft.CodeAnalysis.CSharp | 5.6.0 | 5.9.0 | minor |
| Microsoft.Build.Utilities.Core | 18.9.6 | 18.10.1 | minor |
| Spectre.Console.Cli | 0.55.0 | 0.57.2 | minor |
| Roslynator.Analyzers | 5.0.0 | 5.0.1 | patch |
| Roslynator.CodeAnalysis.Analyzers | 5.0.0 | 5.0.1 | patch |
| Roslynator.Formatting.Analyzers | 5.0.0 | 5.0.1 | patch |

Re-run the check at task start; the list may have moved.

## Requirements

- `Microsoft.CodeAnalysis.CSharp` is referenced by the source generator / analyzers package. Its version
  sets the minimum Roslyn a consumer's SDK must ship. Confirm the 5.9.0 bump does not raise the minimum
  SDK above what the TimeWarp repos use before accepting it; if it does, pin and note it.
- Roslynator patch bumps can introduce new diagnostics. Warnings are errors here, so fix any new findings
  rather than suppressing them.
- `Microsoft.Build.Utilities.Core` is used by `source/timewarp-nuru-build`; verify the build-tasks
  package still loads in a consumer `dotnet build`.

## Checklist

- [x] `ganda nuget outdated` re-run at task start; table above refreshed if different
- [x] `ganda nuget outdated --update` applied (or `--prompt` to skip any pinned package)
- [x] `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs` green
- [x] `dotnet publish -c Release -r linux-x64 -p:PublishAot=true` on a test app still clean (no new IL warnings)
- [x] Any new Roslynator diagnostics fixed, not suppressed
- [x] `changelog.md` Unreleased notes the dependency bumps that affect consumers (Roslyn minimum, if changed)
- [ ] PR merged

## Notes

- Follows the beta.79 dogfood cut (483). Keep this separate from the `3.0.0` bump so the release diff
  stays a version-only change.
- `ganda nuget outdated --dry-run` is read-only and safe to re-run from any checkout.
- 2026-10-07: review oracle (ganda task work, tw-implementation-review effort 1). Round 1 general — disposition clean. Next host nodes: open-pr / done (no apply-review sibling).

## Session

- Created: claude 2412bd45 (2026-10-07)
- Implement: claude (ganda task work, 2026-10-07)
- Review: grok `01a11726-522b-73f3-8db9-ecc916953ecb` (2026-10-07)

## Results

Re-ran `ganda nuget outdated` at task start: same 6 packages as the snapshot.

| Package | From | To | Decision |
|---------|------|----|----------|
| Roslynator.Analyzers | 5.0.0 | 5.0.1 | updated |
| Roslynator.CodeAnalysis.Analyzers | 5.0.0 | 5.0.1 | updated |
| Roslynator.Formatting.Analyzers | 5.0.0 | 5.0.1 | updated |
| Spectre.Console.Cli | 0.55.0 | 0.57.2 | updated (benchmarks only) |
| Microsoft.CodeAnalysis.CSharp | 5.6.0 | 5.9.0 | **pinned at 5.6.0** |
| Microsoft.Build.Utilities.Core | 18.9.6 | 18.10.1 | **pinned at 18.9.6** |

- **Roslyn pin**: SDK 10.0.301/10.0.302 ship Roslyn 5.6.0; only SDK 10.0.400 ships 5.9.0. Moving to 5.9.0
  would stop the analyzer loading on 10.0.3xx consumer SDKs, so it stays pinned. The pin comment in
  `Directory.Packages.props` now records the SDK mapping.
- **MSBuild pin**: 18.10.1 ships only `lib/net11.0` and `lib/net472` (checked in the NuGet cache), so the
  existing pin stays. The build-tasks package is unchanged, so consumer `dotnet build` loading is unaffected.
- **Roslynator 5.0.1**: Release build of `timewarp-nuru.slnx` is clean, with no new diagnostics and no suppressions.
- **Spectre.Console.Cli 0.57.2**: `bench-spectreconsole` builds. `timewarp-nuru-benchmarks` fails only on
  `cli-fx-command.cs` (CliFx API, CS0246/CS0234). That failure is pre-existing: the project is commented out of
  `timewarp-nuru.slnx`, and none of its Spectre code fails.
- CI: `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs` passed 3830 tests, skipped 12, failed 0, exit 0.
- AOT: `dotnet publish` of `tests/test-apps/timewarp-nuru-testapp-delegates` (linux-x64, PublishAot) produced no
  IL warnings. The binary runs. The 4 CS0436 (`Mediator` type conflict) warnings also appear on origin/master.
- `changelog.md` Unreleased notes the bumps and that the consumer Roslyn minimum is unchanged.

### How to validate

**Smoke**

```bash
ganda nuget outdated --dry-run
dotnet build timewarp-nuru.slnx -c Release
ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs
```

**Expect**

- `ganda nuget outdated` lists only `Microsoft.CodeAnalysis.CSharp` (5.6.0) and `Microsoft.Build.Utilities.Core`
  (18.9.6). Both are deliberate pins documented in `Directory.Packages.props`.
- The Release build succeeds with 0 warnings and 0 errors (no new Roslynator diagnostics).
- CI tests exit 0 with no failures.

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Final counts:** bug 0, suggestion 0, nit 0 (0 open, 0 fixed, 0 wontfix)
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified during review: `ganda nuget outdated --dry-run` reports only `Microsoft.CodeAnalysis.CSharp 5.6.0 -> 5.9.0` and `Microsoft.Build.Utilities.Core 18.9.6 -> 18.10.1` (2 outdated, both minor). SDK 10.0.301/10.0.302 ship Roslyn 5.6.0; SDK 10.0.400 ships 5.9.0. Cached `Microsoft.Build.Utilities.Core` 18.10.1 `lib/` is `net11.0` and `net472` only; 18.9.6 has `lib/net10.0`. `dotnet build benchmarks/aot-benchmarks/bench-spectreconsole/bench-spectreconsole.csproj -c Release` is 0 warnings, 0 errors.
