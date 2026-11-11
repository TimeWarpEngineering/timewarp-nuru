# Collect CI code coverage through Jaribu runfile coverage mode

## Description

`ganda repo audit` warns `ci-code-coverage`: CI runs tests but collects no coverage. Nuru runs
tests in runfile mode (`dev workflow --stage test-multi|test-standalone` → `dotnet <runner>.cs` →
`RunAllTests()`), which has no coverage hook today. Jaribu task 060 adds a runfile coverage mode
(relaunch under `dotnet-coverage`, Cobertura output in `artifacts/coverage`). This task adopts it.

## Requirements

- Bump `TimeWarp.Jaribu` to the prerelease that ships 060.
- `tools/dev-cli/endpoints/test-command.cs` turns coverage on in CI mode (`--coverage` flag or
  `JARIBU_COVERAGE=1`), results dir `artifacts/coverage`, module filter to `TimeWarp.Nuru*`
  product assemblies.
- `.github/workflows/workflow.yml`: install `dotnet-coverage` in the test stages and add an
  `actions/upload-artifact` step with `path: artifacts/coverage` and a per-matrix-leg `name`
  (`coverage-test-multi`, `coverage-test-standalone`) so the legs do not collide.
- `ganda repo audit` reports `ci-code-coverage` PASS (the check reads `dotnet-coverage`/`--coverage`
  in dev-cli source plus the upload step; its auto-fix cannot do this for non-MTP runners).
- Do not set `ci-code-coverage.min-line` yet; first look at the numbers.

## Checklist

- [ ] Jaribu bumped
- [ ] test-command.cs enables coverage in CI
- [ ] Workflow installs dotnet-coverage and uploads artifacts
- [ ] Audit PASS on the branch
- [ ] Record the first line-rate numbers under Results

## Depends on

- timewarp-jaribu 060

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-11)

## Notes

Steve, 2026-11-11: coverage belongs in Jaribu, not a per-repo wrapper. If 060 stalls, the
fallback is wrapping the runner in `dotnet-coverage collect` inside test-command.cs, which passes
the same audit check. `## Depends on` here names another repo's task, which the ganda gate does
not parse; it is documentation, not a merge wait.
