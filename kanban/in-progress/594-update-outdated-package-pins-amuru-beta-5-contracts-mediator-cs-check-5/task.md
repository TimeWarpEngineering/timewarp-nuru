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

## Goal (implementer restatement)

Bring the six outdated central pins to the versions `ganda nuget outdated` reported on
2026-11-16, using `ganda repo audit --fix` for the non-major bumps and a hand bump when
audit leaves a major (CsCheck 4.9.1 → 5.0.1). After the pins move, source and the runfile
sources the solution build does not cover (`source/timewarp-nuru-devcli/content/`,
`tools/dev-cli/`, and any `#:package` that still names an old version) must compile and
the existing tests must pass. No new product behavior.

Acceptance:

- `Directory.Packages.props` pins TimeWarp.Amuru and TimeWarp.Amuru.Tools at 3.0.0-beta.5,
  TimeWarp.Contracts at 2.0.0-beta.33, TimeWarp.Mediator and TimeWarp.Mediator.Generators
  at 15.0.0-beta.9, and CsCheck at 5.0.1.
- `ganda nuget outdated` reports nothing outdated.
- `./bin/dev build` passes.
- After `ganda runfile cache --clear`, `dotnet run tests/ci-tests/run-ci-tests.cs` and
  `dotnet run tests/ci-standalone/run-standalone-tests.cs` pass. The walk gate is one
  `./bin/dev test`, which is the same suites.

## Checklist

- [x] `ganda repo audit --fix` and commit what it changes in `Directory.Packages.props`.
- [x] `ganda nuget outdated` reports nothing outdated. If audit --fix leaves a pin behind
      (CsCheck 5 is a major), bump it by hand.
- [x] Fix whatever the new versions break in source and in the runfile sources the
      solution-wide build does not cover (`source/timewarp-nuru-devcli/content/`,
      `tools/dev-cli/`).
- [x] `./bin/dev build` passes.
- [x] `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs` and
      `dotnet run tests/ci-standalone/run-standalone-tests.cs` pass.

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-16)
- Implementer: opencode session ses_e1dd251b3ffeYlN87OdRWte6v4 (2026-11-16)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e7035b14-987c-4e3e-beba-75397f6e601b, max-turns 500, Done — 2026-11-16T02:47:22Z

## Results

Brought the six outdated central pins current. `ganda repo audit --fix` moved the five
non-major pins. CsCheck 5 is a major, so audit left 4.9.1; that pin was bumped by hand
to 5.0.1. `ganda nuget outdated` then reported all 57 pins up to date.

Amuru 3.0.0-beta.3 through beta.5, Mediator 15.0.0-beta.9 (net11.0-only generator), and
Contracts 2.0.0-beta.33 compiled with no source change. The solution build and the
audit `--fix` self-install of `tools/dev-cli` both succeeded against those pins.
`source/timewarp-nuru-devcli/content/` is what that dev-cli compile consumes.

CsCheck 5 renamed `Gen.OneOfConst` to `Gen.OneOf` (`params T[]`). The argv fuzz runfile
is the only call site. The rename is the only product fix.

### How to validate

**Smoke**

```bash
ganda nuget outdated
./bin/dev build
ganda runfile cache --clear
./bin/dev test
```

`./bin/dev test` runs `tests/ci-tests/run-ci-tests.cs` with `NURU_CI_PHASE=all`, which
runs the multi-mode assembly and then `tests/ci-standalone/run-standalone-tests.cs`.

**Expect**

- `ganda nuget outdated` prints `All packages are up to date!`
- `Directory.Packages.props` pins TimeWarp.Amuru and TimeWarp.Amuru.Tools at
  `3.0.0-beta.5`, TimeWarp.Contracts at `2.0.0-beta.33`, TimeWarp.Mediator and
  TimeWarp.Mediator.Generators at `15.0.0-beta.9`, and CsCheck at `5.0.1`.
- `./bin/dev build` ends with `Build completed successfully!`
- `./bin/dev test` ends with `Tests completed successfully!` Multi-mode is 2238 total,
  2230 passed, 8 skipped, 0 failed. Standalone is 48 files, 0 failed.

**Automated gate**

| Command | Result |
|---|---|
| `./bin/dev build` | Pass. Each project 0 warning, 0 error. `Build completed successfully!` |
| `ganda runfile cache --clear` then `./bin/dev test` | Pass. First run failed compile on `Gen.OneOfConst` (CsCheck 5). After the rename, multi-mode 2238 total / 2230 passed / 8 skipped / 0 failed (215.9s); standalone 48 files, 0 failed (29.9s). `Tests completed successfully!` |
| `ganda repo audit` | Pass. 44 passed, 0 failed, 1 skipped. `Repository passes all audit checks.` |

**Not in scope:** a Nuru version bump or changelog entry. This task only moves consumed pins.

## Notes

Steven 2026-11-16: "update".

Last round was PR #409 (task 587 pins on feature/generator-fixes).
