# DevCli remaining 470 findings

Parent: 470 (2026-09-04 full-repo review). Suggestions/nits: M23, M24, M41.

## Description

M23: `packable-project-service.cs:126-140` — MSBuild exit 0 with unparseable JSON silently omits the project from the packable set used by check-version, release, and promote.

M24: `GenerateNuruJsonContextTask.cs:58-70` — any exception in ExecuteCore is a warning + return true with empty GeneratedFiles (intentional fail-soft). Unexpected bugs become silent ToString fallback.

M41: Windows successful self-install leaves `dev.exe.old`.

NuGet fail-open / Version trim / package-id path are **470-007**, not this task.

## Requirements

- If MSBuild exit is 0 but Properties cannot be parsed, throw naming the project (M23).
- Keep fail-soft for “no DSL”; fail or emit a highly visible diagnostic for unexpected JSON-context exceptions (M24).
- Best-effort delete `oldExe` after successful Windows self-install (M41).

## Checklist

- [x] M23 packable parse fail-loud
- [x] M24 JSON-context unexpected errors
- [x] M41 self-install .old cleanup
- [x] Tests where practical

## Notes

Evidence: parent 470 `review/round-1/merged.md` M23, M24, M41. 458 versioning policy is out of scope.

## Results

M23: `GetPackableProjectsAsync` now uses `TryParseGetPropertyOutput`. Exit 0 with missing/unparseable Properties JSON throws `InvalidOperationException` naming the project path — same fail-loud posture as nonzero exit, blank PackageId, and duplicate IDs. A successful parse with `IsPackable=false` still excludes the project without throwing. `ParseGetPropertyOutput` remains a soft wrapper returning `(false, null)` when TryParse fails.

M24: Outer `Execute` catch on `GenerateNuruJsonContextTask` now `Log.LogError` + High-importance stack + `return false` (fails the build) for unexpected exceptions. Expected “no DSL” paths in `ExtractFromDelegateRoutes` / method-body catches stay fail-soft `LogMessage`.

M41: After successful Windows publish, `WindowsExeReplace.TryDeleteOldExecutable` best-effort deletes `dev.exe.old` (IO/UnauthorizedAccess → warning, non-fatal). Helper lives in a NuruRoute-free service file so tests can compile it without endpoint discovery collisions.

### Files changed

- `source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs`
- `source/timewarp-nuru-devcli/content/any/services/windows-exe-replace.cs` (new)
- `source/timewarp-nuru-devcli/content/any/endpoints/self-install-command.cs`
- `source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs`
- `tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs`
- `tests/timewarp-nuru-tests/devcli/self-install-01-old-exe-cleanup.cs` (new)
- `tests/timewarp-nuru-tests/devcli/Directory.Build.props`
- `tests/ci-tests/Directory.Build.props`

### Test outcomes

- `packable-projects-01-parse.cs`: 21 passed (was 19; added TryParse unparseable coverage + not-packable distinguishable from unparseable)
- `self-install-01-old-exe-cleanup.cs`: 2 passed
- `packable-projects-02-derivation-fixture.cs`: 3 passed
- `dotnet build source/timewarp-nuru-build/timewarp-nuru-build.csproj`: succeeded
- `dotnet build tools/dev-cli/dev.cs`: succeeded; `dev --help` lists `self-install`

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-470-014-devcli-remaining-470-findings
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs
dotnet run tests/timewarp-nuru-tests/devcli/self-install-01-old-exe-cleanup.cs
dotnet run tests/timewarp-nuru-tests/devcli/packable-projects-02-derivation-fixture.cs
dotnet build source/timewarp-nuru-build/timewarp-nuru-build.csproj
dotnet build tools/dev-cli/dev.cs
```

**Expect**

- 01: 21 passed. `No_properties_marker_is_unparseable`, `Invalid_json_after_opening_brace_is_unparseable`, `Missing_properties_key_is_unparseable`, and `Null_properties_object_json_is_unparseable` assert `TryParseGetPropertyOutput` returns false; `Successful_not_packable_parse_is_distinguishable_from_unparseable` asserts TryParse true with `IsPackable=false`.
- self-install-01: 2 passed. Existing temp `.old` file is deleted; absent path returns true.
- 02: 3 passed (live msbuild fixture unchanged).
- Build projects succeed with 0 errors.

**Automated gate**

```bash
ganda runfile cache --clear
dotnet run tests/ci-tests/run-ci-tests.cs
# expect: exit 0, 0 failed; multi-mode includes PackableProjectParse (21) and WindowsExeReplace (2)
```

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Final counts:** bug/suggestion/nit all 0 open, 0 fixed, 0 wontfix
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified smoke: packable-projects-01 21 passed, self-install-01 2 passed, `timewarp-nuru-build` build succeeded

## Session

- 2026-09-23: implementer (ganda task work, oracle implement). Product fixes for M23/M24/M41 + tests. Next host nodes: review, open-pr.
- 2026-09-23: review oracle (ganda task work, tw-implementation-review effort 1). Round 1 general — disposition clean. Next host nodes: open-pr (no apply-review sibling).
