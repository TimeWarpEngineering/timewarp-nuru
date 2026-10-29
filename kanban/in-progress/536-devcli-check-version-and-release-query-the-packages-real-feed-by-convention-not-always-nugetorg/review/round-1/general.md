# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** `4b3df3020aa80553b586bd1508f8749407c667b1`..`d29417f853629f32b5e1cb7f28454b5363330529` (branch vs `master`). Product paths: DevCli publish-gate services and commands, `tools/dev-cli/endpoints/workflow-command.cs`, `.github/workflows/workflow.yml`, `documentation/developer/guides/releasing.md`, `source/timewarp-nuru-devcli/readme.md`, `changelog.md`, and `tests/timewarp-nuru-tests/devcli/check-version-0{7,9}-*.cs`.

## Summary

`check-version`, `dev release` guard 7, and the release pipeline's Step 2/6 now treat a version as published when nuget.org or the org GitHub Packages feed lists it. Both feed URLs are constants, the flat container comes from the service index `PackageBaseAddress/3.0.0` resource, and a GitHub Packages miss (no token, non-404 status, timeout, malformed body) is `Unknown` unless nuget.org already lists the version. The `release` job is the only `workflow.yml` job that runs that gate, and it has `packages: read`. The pipeline abort for an unreadable feed names the feed and status and does not say "version already released".

Verdict: no issues.

## Issues

None.
