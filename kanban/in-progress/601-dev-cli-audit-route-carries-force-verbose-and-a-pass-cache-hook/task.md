# DevCli audit route carries force, verbose and a pass-cache hook

## Description

`TimeWarp.Nuru.DevCli` 4.0.0-beta.17 ships a shared `audit` route. A repo adds
checks through `IDevAuditCheck` and `DevAuditChecks.AddRepoChecks`. The route
takes no options and has no way to skip a run.

timewarp-ganda already owns `dev audit` in `tools/dev-cli/endpoints/audit-command.cs`.
With DevCli beta.17 its dev-cli no longer compiles (`NURU_R003`, unreachable
route). It cannot move onto the shared route, because its command does three
things the shared one cannot express:

- `--force` and `--verbose,-v` options.
- A whole-run pass stamp (ganda task 707, `DevAuditStamp`): skip every check
  when this tree already passed, and record the pass only when all checks pass.
- A deps-only run that another command calls in process (ganda task 710,
  `DevAuditScope.DepsCheckOnly`, called from `pr-pipeline.cs`).

Source: timewarp-ganda task 705, `## Refactor proposal`, part 1 (2026-11-25).
timewarp-ganda task 725 moves ganda onto this route after the release.

## Wanted

- The shared `audit` route accepts `--force` and `--verbose,-v` and passes them
  to each check through a context argument (for example
  `RunAsync(DevAuditContext, CancellationToken)`).
- The runner has an optional pass-cache hook, or DevCli owns the tree-key
  stamp, so a repo gets "skip when this tree already passed" without its own
  command. `--force` bypasses it. The pass is recorded only when every check
  passes.
- A repo can run a named subset of its checks in process without going through
  the route.
- A DevCli release carries the change.

## Goal (implementer restatement)

Make DevCli's shared `dev audit` route able to carry ganda's audit command, so ganda can drop
its own `audit` endpoint (ganda task 725).

Acceptance criteria:

- `dev audit --force` and `dev audit --verbose` / `-v` parse on the shared route. Each check gets
  them through a `DevAuditContext` (repo root, force, verbose): `IDevAuditCheck.RunAsync` takes
  `(DevAuditContext, CancellationToken)`.
- A repo can opt into a pass cache (`IDevAuditPassCache`) through a partial-method hook.
  DevCli ships a ready tree-key stamp (`DevAuditTreeStamp`, a port of ganda's `DevAuditStamp`)
  so a repo needs no code of its own beyond one line. When this tree already passed, the run
  prints a skip line and runs nothing. `--force` runs every check anyway. A pass is recorded
  only when every check passed; a failed run records nothing.
- A repo can run a named subset of its checks in process (`DevAuditChecks.Named(...)` with
  `DevAuditRunner.RunAsync`), and that run neither reads nor records the pass.
- Jaribu tests cover the context reaching checks, the skip, `--force`, record-only-on-pass,
  the subset run, the tree stamp key, and the route accepting `--force -v`.
- DevCli readme (audit section and a migration note) is updated, and the version bumps to
  4.0.0-beta.18 so a release carries the change.

## Checklist

- [x] `--force` and `--verbose,-v` on the shared `audit` route, passed to checks through a context
- [x] Pass-cache hook or DevCli-owned tree stamp; `--force` bypasses it (both: `IDevAuditPassCache` hook plus `DevAuditTreeStamp`)
- [x] In-process entry point that runs a named subset of checks
- [x] Tests for the options, the skip, and the subset run
- [x] DevCli template and docs updated (no DevCli template lives in this repo; readme, new `dev-audit.md`, changelog)

## Session

- Created: 5e357388-dba6-4ffe-89e8-92df174ea48a (2026-11-25)
- 2026-11-25 implementer (ganda task work, Claude): implemented the route options, pass cache,
  tree stamp and subset run; fixed the pre-existing `ganda repo audit` failures (see Results).

## Notes

Steven chose option (b) on timewarp-ganda 705 on 2026-11-25: ganda ships Nuru
beta.17 now and holds DevCli at beta.16 until this lands.

The names `IDevAuditCheck`, `DevAuditChecks.AddRepoChecks` and `DevAuditRunner`
are from the ganda 705 implementer's report. Check them against the DevCli
source before starting.

## Results

### What changed

DevCli content (`source/timewarp-nuru-devcli/content/any/`):

- `endpoints/audit-command.cs`: `[Option("force")]` and `[Option("verbose", "v")]`. The handler
  builds `DevAuditContext(Git.FindRoot() ?? current directory, Force, Verbose)` and passes
  `DevAuditChecks.PassCache()` to the runner.
- `services/dev-audit-context.cs` (new): `DevAuditContext(RepoRoot, Force, Verbose)`.
- `services/dev-audit-check.cs`: `IDevAuditCheck.RunAsync(DevAuditContext, CancellationToken)`
  (breaking for a repo with checks; beta.17 shipped the old signature, ganda has not adopted it).
- `services/dev-audit-pass-cache.cs` (new): `IDevAuditPassCache` (`KeyAsync`, `HasPass`, `RecordPass`).
- `services/dev-audit-tree-stamp.cs` (new): `DevAuditTreeStamp.Create(ignoredEntries?)`, a port of
  ganda's `DevAuditStamp` (same key rule, same `artifacts/dev-audit/passed.key`), with the ignored
  top-level entries configurable (default `kanban`). Static `Create` so an unconstructed class does
  not fail CA1812 in consumers.
- `services/dev-audit-checks.cs`: `ConfigurePassCache(ref IDevAuditPassCache?)` partial hook,
  `PassCache()`, `Named(params names)` and `Select(checks, names)` (registry order; an unknown name
  throws `ArgumentException` listing the registered names).
- `services/dev-audit-runner.cs`: `RunAsync(checks, context, terminal, passCache, ct)`. With a cache
  and a key and no `--force`, a recorded pass prints `dev audit: these inputs already passed (<12
  hex>); nothing run. --force runs every check.` and returns `Skipped`. The key is recorded only when
  every check passed. A throwing `KeyAsync` counts as no key (printed with `-v`). An empty check list
  never touches the cache.

Version 4.0.0-beta.17 -> 4.0.0-beta.18 (`source/Directory.Build.props`), so the next DevCli release
carries this. Docs: `source/timewarp-nuru-devcli/dev-audit.md` (the audit section moved out of the
readme, plus Options / Skip a tree / Run a subset), readme table rows and a beta.18 migration note,
changelog entry.

Tests: `audit-01-runner.cs` updated to the new signatures; new `audit-02-endpoint.cs` (routes
`audit --force --verbose`, `audit --force -v`, `audit`, `audit --help`; standalone only, listed in
`CiTestExcludes` and `tests/ci-standalone/run-standalone-tests.cs`), `audit-03-pass-cache-and-subset.cs`,
`audit-04-tree-stamp.cs` (pure key/status rules, pass file round trip, one real temp git repo).
New content files added to `tests/timewarp-nuru-tests/devcli/Directory.Build.props` and
`tests/ci-tests/Directory.Build.props`; `audit-command.cs` only to the devcli test props.

Audit fixes (pre-existing failures on master, boy-scout):

- `package-pin-currency`: `ganda repo audit --fix` bumped the task-570 proof pin TimeWarp.Nuru
  4.0.0-beta.16 -> 4.0.0-beta.17 in `Directory.Packages.props`. Beta.17 reports NURU_A002 on the proof's
  two unordered `[Parameter]`s, so `proof/calc.cs` got `Order = 0` / `Order = 1`; `proof/capture.sh`
  reran 8 PASS / 0 FAIL with byte-identical captures.
- `source-file-line-cap` (now counts Markdown): split `changelog.md` (711 lines) into
  `changelog.md` (unreleased + 3.0.0) and `documentation/changelog/betas-2026.md` /
  `documentation/changelog/releases-2025.md`; renamed the six `*-iteration-N.md` debate files in
  `documentation/community-feedback/001-api-naming-error-handling/resolution-workspace/` to
  `*-opening-position.md`, `*-rebuttal.md`, `*-closing-position.md` (references updated); moved the
  DevCli audit docs out of the readme (821 lines, origin 857); recorded the 23 remaining over-cap
  Markdown files at their current counts in `.timewarp/line-cap-baseline.json` (each at or below its
  origin/master length, so it is an adoption).
- Left as is: `vscode-window-icon` warning (`.vscode/settings.json` missing); a warning, not a failure.

### Gates

- `./bin/dev build`: passed (0 warnings, 0 errors; packed TimeWarp.Nuru.DevCli 4.0.0-beta.18).
- `dotnet run --file tests/timewarp-nuru-tests/devcli/audit-01-runner.cs`: 6/6 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/audit-02-endpoint.cs`: 4/4 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/audit-03-pass-cache-and-subset.cs`: 14/14 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/audit-04-tree-stamp.cs`: 6/6 passed.
- `dotnet build tests/ci-tests/run-ci-tests.cs` (compile only, no tests run): succeeded, so the
  multi-mode assembly compiles with audit-01/03/04.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` (tools/dev-cli compiles the DevCli
  content): succeeded; `./bin/dev audit --force -v` printed the nothing-to-run line, exit 0.
- `ganda repo audit`: first run failed (`package-pin-currency`, `source-file-line-cap`, both
  pre-existing); after the fixes above, rerun passed (exit 0, one warning: `vscode-window-icon`).

### How to validate

Smoke:

```bash
./bin/dev build
dotnet run --file tests/timewarp-nuru-tests/devcli/audit-02-endpoint.cs
dotnet run --file tests/timewarp-nuru-tests/devcli/audit-03-pass-cache-and-subset.cs
dotnet run --file tests/timewarp-nuru-tests/devcli/audit-04-tree-stamp.cs
./bin/dev audit --help
```

Expect: build succeeds; the three runfiles report 4/4, 14/14 and 6/6 passed; `audit --help` shows
`audit [--force] [--verbose,-v]` with both options described.

### Follow-ups

- Release TimeWarp.Nuru.DevCli 4.0.0-beta.18 (`dev release` after merge), then timewarp-ganda task 725
  moves ganda's `dev audit` onto the shared route: its three steps become `IDevAuditCheck`s,
  `ConfigurePassCache` returns `DevAuditTreeStamp.Create()`, and `pr-pipeline.cs` calls
  `DevAuditRunner.RunAsync(DevAuditChecks.Named("deps-check"), context, terminal, passCache: null, ct)`.
