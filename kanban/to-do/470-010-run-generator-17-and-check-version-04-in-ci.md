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

- [ ] generator-17 on standaloneTests (M14)
- [ ] check-version-04 on standaloneTests (M15)
- [ ] generator-19/20 gated methods (M29)
- [ ] CI still runs generator-28 family

## Notes

Evidence: parent 470 `review/round-1/merged.md` M14, M15, M29. `tests/ci-tests/run-ci-tests.cs:19-35`.
