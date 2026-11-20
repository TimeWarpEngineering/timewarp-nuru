# Round 1 — general
**Date:** 2026-11-21
**Scope reviewed:** branch `task/600-dev-cli-ships-an-empty-dev-audit-command-each-repo` vs merge-base `9f6abfbc`, product diff and kitchen `## Results` / `### How to validate`.

## Summary

TimeWarp.Nuru.DevCli gains `audit` in `content/any/endpoints/audit-command.cs`. `AuditCommand` runs `DevAuditRunner` over `DevAuditChecks.All()` and, when any check failed, sets `Environment.ExitCode` to 1. That is the same exit path as `check-version` and `release`: a plain `RunAsync` returns `NuruCallContext.CurrentExitCode`, which is `Environment.ExitCode` when no call context is set, and `TelemetryBehavior` does not clear it. The new files do not invoke `ganda repo audit`.

`IDevAuditCheck` is `Name` plus `RunAsync`, and `DevAuditCheckResult.Pass` / `Fail` carry the message. The registry is a private `static partial void AddRepoChecks` on `DevAuditChecks`. DevCli content compiles into the consuming dev CLI, so a repo implements that partial once in its own `tools/dev-cli` and a repo that writes nothing gets the call erased. That is compile-time binding, which is the AOT-safe choice the task left open. The design comment and the readme both say why DI multi-registration was not used: source-generated DI does not resolve `IEnumerable<T>`. `tools/dev-cli/Directory.Build.props` already globs `content/any/endpoints/*.cs` and `content/any/services/*.cs`, and the package props do the same, so the new files ship without a hand-maintained file list. `dev.cs` only gains a help comment. This repo does not implement `AddRepoChecks`, so `./bin/dev audit` is the empty case.

`DevAuditRunner` runs every check, including after a failure. Each line is `PASS` or `FAIL`, the name, and wall time (`42ms` under one second, `2.3s` at or above). A non-empty message is written in full on the following lines. The last line is `dev audit: N passed, M failed`. An empty list prints `dev audit: no checks registered, nothing to run` and returns exit 0. A thrown exception other than `OperationCanceledException` becomes `Fail("{TypeName}: {Message}")`. Cancellation propagates. `audit-01-runner.cs` covers the empty runner, the empty registry, two passing lines, a failing message with the later check still run and exit 1, a throwing check, and the elapsed format. The three service files are compiled into both the standalone devcli runfiles and the CI multi-mode assembly. `audit-command.cs` stays out of those compiles, which matches the existing rule that a `[NuruRoute]` endpoint is not pulled into the multi-mode discovery set.

The readme adds the `audit` row, the three service rows, the extension example, the fast pre-test rule, the loop warning, and the 4.0.0-beta.17 route-clash migration note. `source/Directory.Build.props` moves 4.0.0-beta.16 to 4.0.0-beta.17, which is the single repo version, so DevCli can be taken by ganda task 700. Results name the partial-method mechanism, the one-commit move of ganda's own `audit` endpoint onto `AddRepoChecks`, and why Nuru's workflow stages (attestation, package layout, verify-samples) are not checks to register here. The workflow file's PR stages match that list.

The goal restatement matches the original task. It adds a Jaribu test and a ganda 700 migration note; both are present and do not narrow or contradict the wanted behavior. Recorded gates cover `./bin/dev build` (0 warnings, 0 errors), the standalone runner (6/6), the AOT `self-install` plus empty `dev audit` (exit 0), and `ganda repo audit`. This review read that record and the diff. It did not re-run those gates. No suspected bug needed a targeted command.

## Issues

<!-- None. -->
