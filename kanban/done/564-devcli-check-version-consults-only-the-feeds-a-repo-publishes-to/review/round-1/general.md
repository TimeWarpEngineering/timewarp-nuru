# Round 1 — general
**Date:** 2026-10-31
**Scope reviewed:** `229f2697d9aee3d0bc2b2d254df94fa2e8eb00f9`..`c1e1c816e821dd84ceb3d5773b3f50591b13e0f4` (branch vs `master`). Product paths: `check-version-config.cs`, `nuget-version-service.cs`, `package-feed-lookup.cs`, `check-version-command.cs`, `release-command.cs`, `tools/dev-cli/endpoints/workflow-command.cs` (call site, unchanged signature of the handler), `.github/workflows/workflow.yml`, `.timewarp/dev.jsonc`, `documentation/developer/guides/releasing.md`, `source/timewarp-nuru-devcli/readme.md`, `source/Directory.Build.props` (version unchanged), and `tests/timewarp-nuru-tests/devcli/check-version-0{7,9}-*.cs`.

## Summary

`checkVersionConfig.feeds` is comma-separated `nuget.org` and/or `github-packages`, case-insensitive, trimmed, empty entries dropped. `PackageFeedsResolver` maps a missing key to nuget.org and an unknown name or a value that names no feed to an error. `check-version` and `dev release` both call that resolver on that key and print the error with exit 1 before any feed request; `check-version` leaves `Outcome` at `InvalidInput`, which it sets at the start of `Handle`. `LookupAsync` reads only the declared feeds and returns before `GetGitHubPackagesVersionsAsync`, so the credential provider is not called unless `github-packages` is declared. `Unknown` is only the declared-GitHub-Packages failure when the version is on no declared feed. With both feeds declared, `FormatUnknownRefusal` is the task 536 text; a GitHub-Packages-only repo omits the false "not on nuget.org" clause. `dev workflow` Step 2/6 still runs `CheckVersionCommand.Handler`, so the pipeline uses the same feed set. This repo's `nuget.config` and `dotnet nuget push` target nuget.org only, `<Version>` is still `4.0.0-beta.10`, and the `release` job no longer has `packages: read`. The new tests cover nuget.org-only (no GitHub request, no credential call), both-feeds 536 text, an unknown name before any request, and absent `feeds`. They stub the credential provider.

Verdict: no issues.

## Issues

None.
