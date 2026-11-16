# Update outdated package pins (Amuru beta.5, Contracts, Mediator, CsCheck 5)

## Description

`ganda nuget outdated` on master (2026-11-16) reports six pins behind. Bring them to the
latest with `ganda repo audit --fix`, then make the build and tests pass.

| Package | Pinned | Latest |
|---|---|---|
| TimeWarp.Amuru | 3.0.0-beta.2 | 3.0.0-beta.5 |
| TimeWarp.Amuru.Tools | 3.0.0-beta.2 | 3.0.0-beta.5 |
| TimeWarp.Contracts | 2.0.0-beta.32 | 2.0.0-beta.33 |
| TimeWarp.Mediator | 15.0.0-beta.8 | 15.0.0-beta.9 |
| TimeWarp.Mediator.Generators | 15.0.0-beta.8 | 15.0.0-beta.9 |
| CsCheck | 4.9.1 | 5.0.1 (major) |

## Checklist

- [ ] `ganda repo audit --fix` and commit what it changes in `Directory.Packages.props`.
- [ ] `ganda nuget outdated` reports nothing outdated. If audit --fix leaves a pin behind
      (CsCheck 5 is a major), bump it by hand.
- [ ] Fix whatever the new versions break in source and in the runfile sources the
      solution-wide build does not cover (`source/timewarp-nuru-devcli/content/`,
      `tools/dev-cli/`).
- [ ] `./bin/dev build` passes.
- [ ] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` and
      `dotnet run tests/ci-standalone/run-standalone-tests.cs` pass.

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-16)

## Notes

Steven 2026-11-16: "update".

Last round was PR #409 (task 587 pins on feature/generator-fixes).
