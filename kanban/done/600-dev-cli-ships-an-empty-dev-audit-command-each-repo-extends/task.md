# DevCli ships an empty dev audit command each repo extends

## Description

`ganda repo audit` is the gate every walk, merge and manual audit runs. It has no way to run
checks that belong to one repo. timewarp-ganda task 700 adds the hook: when a repo's dev CLI
has `dev audit`, repo audit runs `./bin/dev audit` and fails on a non-zero exit. The
first user is ganda's `dev deps check`, which today runs only in CI, so the walk misses it.

This task gives every repo that dev CLI command through TimeWarp.Nuru.DevCli, so a repo
adds its checks without writing the command itself.

## Wanted

- **DevCli ships `dev audit`** (`content/any/endpoints/`). With no checks registered it
  prints that there is nothing to run and exits 0.
- **A repo adds checks without redefining the route.** Two endpoints cannot both own
  `audit`, so the shell needs an extension point, for example an `IDevAuditCheck`
  interface (name, run, pass or fail with a message) that the repo implements in its own
  `tools/dev-cli`. `dev audit` runs every registered check in order, prints one line each,
  and exits non-zero when any fails. How checks are found (DI registration or source
  generation) is the implementer's call; it must work in the AOT dev CLI. Say which in
  Results.
- **Output.** One line per check, with its wall time. A failing check's message is printed
  in full. The last line says how many passed and failed.
- **Readme.** Add `audit` to the endpoint table and document the extension point with a
  short example.

## Rules

- `dev audit` never calls `ganda repo audit`. ganda repo audit calls `dev audit`, so the
  other direction would loop.
- It is for pre-test checks: fast, no full test suite. Say so in the readme.

## Out of scope

- The ganda side (repo audit calling `dev audit`, and ganda's own checks) is
  timewarp-ganda task 700. It lands without waiting for this task.
- Adding checks for this repo. If Nuru has a repo-specific pre-test check in its CI
  pipeline today, list it in Results as a follow-up, do not move it here.

## Goal (implementer restatement)

TimeWarp.Nuru.DevCli gains a shared `dev audit` endpoint so every repo's dev CLI has the
command ganda task 700's `ganda repo audit` calls. With no checks it says there is nothing
to run and exits 0. A repo adds its own fast pre-test checks without defining the `audit`
route itself: it implements an `IDevAuditCheck` (name, run, pass/fail with a message) in its
own `tools/dev-cli` and registers it through an extension point that works in the AOT dev
CLI (no reflection). `dev audit` runs the checks in order, prints one line per check with
its wall time, prints a failing check's message in full, ends with a passed/failed summary
line, and exits non-zero when any check fails. It never calls `ganda repo audit`.

Acceptance:
- `dev audit` endpoint ships in `content/any/endpoints/`; empty registry exits 0.
- AOT-safe extension point for repo checks.
- A Jaribu test proves a failing check gives a non-zero exit and prints its message.
- Per-check line with wall time; summary line.
- DevCli readme: `audit` in the endpoint table, extension example, "fast pre-test checks only" rule.
- Results explain how ganda 700's own `dev audit` endpoint moves onto the extension point,
  and list any Nuru CI pre-test check as a follow-up (not moved here).

## Checklist

- [x] `dev audit` endpoint in DevCli; with no checks it exits 0
- [x] Extension point for repo checks, working under AOT
- [x] A failing check makes `dev audit` exit non-zero and prints its message (test)
- [x] Per-check line with wall time, and a summary line
- [x] Readme endpoint table and extension example
- [x] Results: how ganda 700's own `dev audit` endpoint moves onto this extension point
      once ganda takes the new DevCli version (a route clash otherwise)

## Results

### What changed

- `source/timewarp-nuru-devcli/content/any/endpoints/audit-command.cs`: `[NuruRoute("audit")]`
  `AuditCommand`. Runs `DevAuditRunner` over `DevAuditChecks.All()`; any failure sets
  `Environment.ExitCode = 1`. Never calls `ganda repo audit`.
- `content/any/services/dev-audit-check.cs`: `IDevAuditCheck` (`Name`, `RunAsync`) and
  `DevAuditCheckResult` (`Pass(message?)`, `Fail(message)`).
- `content/any/services/dev-audit-checks.cs`: the registry, `DevAuditChecks.All()`, filled by a
  `static partial void AddRepoChecks(List<IDevAuditCheck>)` the repo implements.
- `content/any/services/dev-audit-runner.cs`: runs checks in order (all of them, even after a
  failure), prints `PASS|FAIL <name> (<wall time>)`, a failing (or any non-empty) message in
  full, then `dev audit: N passed, M failed`. A throwing check fails with
  `<ExceptionType>: <message>`; cancellation propagates. Empty list prints
  `dev audit: no checks registered, nothing to run` and returns exit 0.
- Readme: `audit` row in the endpoint table, three service rows, a "Repo audit checks
  (`dev audit`)" section (fast pre-test checks only, never calls ganda repo audit, example,
  output sample), and a 4.0.0-beta.17 migration note about the route clash.
- `source/Directory.Build.props`: 4.0.0-beta.16 -> 4.0.0-beta.17 (beta.16 is already published;
  ganda needs a new DevCli version to pick this up). csproj description and the dev.cs command
  comment mention `audit`.
- Test wiring: the three service files are added to `tests/timewarp-nuru-tests/devcli/` and
  `tests/ci-tests/` `Directory.Build.props`, so the new test runs standalone and in CI multi-mode.

### How checks are found: source generation via a partial method (not DI)

DevCli is source-only, so `DevAuditChecks` compiles into the consuming dev CLI's assembly and
the repo's `AddRepoChecks` body binds at compile time: no reflection, no DI, AOT- and
trim-safe. DI multi-registration was ruled out because Nuru's source-generated DI does not
resolve `IEnumerable<T>` (pinned in `generator-27-ienumerable-dependency.cs`). Proven under AOT:
`./bin/dev self-install` then `./bin/dev audit` prints the nothing-to-run line, exit 0, and
`audit` is in `dev --help`. A temporary two-check `AddRepoChecks` in `tools/dev-cli` (removed
before commit) gave PASS/FAIL lines, the two-line message, `1 passed, 1 failed`, exit 1.

### ganda 700 migration (once ganda takes DevCli 4.0.0-beta.17)

ganda 700 adds its own `dev audit` endpoint; with this DevCli both own `audit` and the build
fails on the route clash. When ganda bumps `TimeWarp.Nuru.DevCli`: delete ganda's audit
endpoint file, wrap `dev deps check`'s logic in a `DepsCheck : IDevAuditCheck` (return
`Fail(<the deps report>)` when it finds problems), and add one
`internal static partial class DevAuditChecks { static partial void AddRepoChecks(List<IDevAuditCheck> checks) => checks.Add(new DepsCheck()); }`
in ganda's `tools/dev-cli`. `ganda repo audit` keeps calling `./bin/dev audit` unchanged.
Do the bump and the move in one commit so no build sees two `audit` routes.

### Follow-up (not done here)

Nuru's CI has no fast, source-only pre-test check to move today. Its `dev workflow` gates are
the attestation verify (needs the ganda note, belongs to the pipeline), the package layout gate
(needs the build output) and verify-samples (a compile). None fits `dev audit`'s "fast, no
build" rule, so Nuru registers no checks. No follow-up task filed.

### How to validate

Smoke:
- `dotnet run --file tests/timewarp-nuru-tests/devcli/audit-01-runner.cs`
- `./bin/dev self-install && ./bin/dev audit; echo $?`

Expect:
- 6/6 tests pass (empty case, empty registry, passing lines with time, failing message and
  exit 1 with later checks still run, throwing check, elapsed format).
- `dev audit: no checks registered, nothing to run` and exit 0; `audit` listed in `./bin/dev --help`.

### Gates

- `./bin/dev build`: passed (0 warnings, 0 errors).
- `dotnet run --file tests/timewarp-nuru-tests/devcli/audit-01-runner.cs`: 6/6 passed (first run
  failed IDE1006 on a `const` local name; fixed, rerun green).
- `./bin/dev self-install` + `./bin/dev audit`: AOT publish OK, exit 0 (smoke, not a gate).
- `ganda repo audit`: passed with 1 advisory warning (`vscode-window-icon`: `.vscode/settings.json`
  missing). Boy-scout: `ganda repo audit --fix` fixed it (writes the git-ignored
  `.vscode/settings.json`, nothing to commit); the fix run reports all checks passing.
- Commit attest (post-commit hook) first failed with `Text file busy: bin/dev` because
  `tools/dev-cli/dev.cs` changed after the self-install; `./bin/dev self-install` cleared it. The
  attest's audit then failed `runfile-executable` (the new test runfile lacked +x once tracked);
  fixed with `chmod +x` / `git update-index --chmod=+x` in a follow-up commit.

### Review

- Round 1. Effort 2. Roster: general. Reviewer session not reported.
- Counts: bug 0 open / 0 fixed / 0 wontfix, suggestion 0, nit 0.
- Disposition: clean. No wontfix and no escalation. The goal restatement matches the original task.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- This review read the diff and the recorded gates. It did not re-run `./bin/dev build`, `./bin/dev test`, or `ganda repo audit`.

## Session

- Created: 64677b22-d705-4b25-b537-4b53732bcb92 (2026-11-21)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 9ace78e0-e547-4ec9-9c30-91c7ec717d2a, max-turns 500, Done — 2026-11-20T20:50:04Z
- Reviewer: grok (2026-11-21). Effort 2, roster general. Session not reported.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7), max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-21
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-20T20:57:02Z

## Review

- Round 1: Done

## Notes

- Steven 2026-11-21: "maybe even just a dev audit so the main audit just calls dev audit if
  dev audit exists run it and we put a shell inside of Nuru for dev audit". Then "yes file
  both".
- Filed from the timewarp-ganda cockpit session. The launch belongs to a timewarp-nuru
  session (`ganda herdr work 600 --repo timewarp-nuru`).
