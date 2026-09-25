# Run generator-17 and check-version-04 in CI

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M14, M15). Suggestion folded: M29.

## Description

454-001 class regression: committed tests that never run on the CI path.

M14: `generator-17-local-function-config.cs` is in CiTestExcludes because it is a top-level-statements program. The exclude comment says run standalone; `run-ci-tests.cs` `standaloneTests` never includes it.

M15: `check-version-04-endpoint-zero-package.cs` is entirely `#if !JARIBU_MULTI` so multi-mode compiles it as a no-op; CI second phase does not invoke it. Endpoint coverage for delimiter-only `--package` (458-005) never runs on CI.

M29: individual methods in `generator-19` and `generator-20` gated `#if !JARIBU_MULTI` never run on the standalone phase (files themselves are multi-included).

## Requirements

- Add generator-17 and check-version-04 to `standaloneTests` in `run-ci-tests.cs`.
- Prefer listing check-version-04 in CiTestExcludes (or document the `#if` inert pattern).
- Extract generator-19/20 standalone-only cases into dedicated excluded+listed files, or add those files to the second phase (M29).
- Confirm generator-28..42 second phase stays invoked.

## Checklist

- [x] generator-17 on standaloneTests (M14)
- [x] check-version-04 on standaloneTests (M15)
- [x] generator-19/20 gated methods (M29)
- [x] CI still runs generator-28 family

## Notes

Evidence: parent 470 `review/round-1/merged.md` M14, M15, M29. `tests/ci-tests/run-ci-tests.cs:19-35`.

Implementation review (effort 1, general): `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Session: review-oracle (ganda task-work).

## Results

Wired CI standalone second phase so previously inert / excluded tests actually run.

- **M14:** Added `generator-17-local-function-config.cs` to `standaloneTests` in `tests/ci-tests/run-ci-tests.cs` (already in `CiTestExcludes`).
- **M15:** Added `check-version-04-endpoint-zero-package.cs` to `standaloneTests` and to `CiTestExcludes` so multi-mode no longer silently compiles an empty `#if !JARIBU_MULTI` file.
- **M29:** Added `generator-19-group-filtering.cs` and `generator-20-parameterized-service-constructor.cs` to the second phase (files stay multi-included for ungated methods; gated `NoFilter_IncludesAll` / `Gen20KanbanQuery` cases run standalone).
- **generator-28..45:** Unchanged entries remain in both `CiTestExcludes` and `standaloneTests`.

### Review disposition

- **Effort / roster:** 1 — general
- **Rounds:** 1
- **Final counts:** bug/suggestion/nit all 0 open, 0 fixed, 0 wontfix
- **Disposition:** clean (no findings raised)
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke**

```bash
dotnet run tests/timewarp-nuru-tests/generator/generator-17-local-function-config.cs
dotnet run tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-19-group-filtering.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-20-parameterized-service-constructor.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-28-interpreter-cycle-guard.cs
```

**Expect**

- generator-17: prints `PASSED: Local function ConfigureServices works!`, exit 0
- check-version-04: 1/1 passed (`Delimiter_only_package_input_…`), exit 0
- generator-19: 7/7 passed including `No Filter_ Includes All`, exit 0
- generator-20: 5/5 passed including `Should_resolve_singleton_with_constructor_deps_in_endpoint`, exit 0
- generator-28: 4/4 passed, exit 0
- `rg 'generator-17|check-version-04|generator-19|generator-20|generator-28' tests/ci-tests/run-ci-tests.cs` shows all five stems in `standaloneTests`
- `rg check-version-04 tests/ci-tests/Directory.Build.props` shows a `CiTestExcludes` entry
