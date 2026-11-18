# Round 1 — general
**Date:** 2026-11-18
**Scope reviewed:** branch `task/598-move-dev-cli-content-gh-and-dotnet-shell-calls-to` vs merge-base `9bb70be1`, product diff and kitchen `## Results` / `### How to validate`.

## Summary

The change pins `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at 3.0.0-beta.9, rewrites every raw `Shell.Builder("gh")` / `Shell.Builder("dotnet")` call site onto the typed builders (or `DotNet.Builder()` with the same argument list), documents the DevCli content dependency, and bumps `source/Directory.Build.props` to 4.0.0-beta.16. The goal restatement matches the original task: documenting the dependency is one of the two allowed choices, and leaving `dev release` until a clean master is the release rule, with the version bump and the unchecked release checklist item recorded here. No `#pragma`, `NoWarn`, editorconfig downgrade, or `ExcludeAssets="analyzers"` was added for AMURU0001. A repo search finds no remaining `Shell.Builder("gh"|"dotnet")` or `Shell.Run("gh"|"dotnet")`.

The typed calls match the previous argv. Decompiled Amuru.Tools 3.0.0-beta.9: `Gh.Run().List().WithJson(...)` emits one `--json` of comma-joined fields; `Gh.Api` and `Gh.Run().Download` match `gh api` and `gh run download <id> --name --dir`; `WithNoValidation` returns the same gh builder, so `ReleaseGuard.BuildReleaseCreate` still executes the flags `ToArguments()` prints; `DotNet.NuGet().WithWorkingDirectory().Push()` passes that working directory into the push builder; `DotNet.Run().WithFile().WithArguments()` emits `dotnet run --file <path> -- <app args>`; `DotNet.Tool().Run("roslynator")` emits `dotnet tool run roslynator` plus the tool arguments. `release-02` updates the expected flag order to the builder's order and states why. msbuild `-nodeReuse:false` and `dotnet format` stay on `DotNet.Builder()` with the same arguments. timewarp-amuru task 154 is on the amuru to-do board and names those gaps plus stdin and timeout.

`DotNetBuilder` has `WithTimeout` and does not have `WithStandardInput`, so the task's `DotNet.Builder()` fallback cannot pipe stdin. routing-33b and daemon-03 build once with `DotNet.Build` and run the apphost through `Shell.Builder(apphost)`, which can take stdin and a timeout. Recorded results say routing-33b passed 4/4 and daemon-03 passed 1/1. The recorded gates cover `./bin/dev build`, the net11 DevCli consumer check, the CI multi-assembly compile, the touched devcli/routing/daemon/mcphost runfiles, and `ganda repo audit` (including the Jaribu pin-currency follow-up). This review did not re-run those gates.

One targeted probe, not a finding: `dotnet tool run nosuchtool fix sample.csproj --supported-diagnostics IDE0001` (reason: `analyze` now builds `dotnet tool run roslynator` without a `--` before `--supported-diagnostics`). The CLI reported only that the tool name is missing, so the dashed argument is forwarded.

## Issues

<!-- None. -->
