# Refresh outdated NuGet packages in TimeWarpEngineering/timewarp-nuru

## Description

Refresh outdated NuGet packages in TimeWarpEngineering/timewarp-nuru.
Filter: (all)
Outdated:
  Serilog.Extensions.Logging 10.0.0 -> 11.0.0 (dev-only: samples/endpoints/09-logging/serilog/structured-logging.cs)
Every outdated pin is dev-only. Bump them. Do not change <Version> in source/Directory.Build.props: the wave merges this task and does not release this repo.
Edit Directory.Packages.props in this repo only.

## Goal (implementer restatement)

Bump the one outdated dev-only pin, `Serilog.Extensions.Logging` 10.0.0 -> 11.0.0, in this repo's
`Directory.Packages.props`. Leave `<Version>` in `source/Directory.Build.props` alone (no release).

Acceptance criteria:
- `Directory.Packages.props` pins `Serilog.Extensions.Logging` at 11.0.0; no other file changes besides this kitchen.
- `./bin/dev build` passes.
- The consumer sample `samples/endpoints/09-logging/serilog/structured-logging.cs` restores and runs with 11.0.0.
- `ganda repo audit` passes.

## Session

- Minted by ganda task wave run 20261119T023451Z-133310
- Implementer: bumped `Serilog.Extensions.Logging` 10.0.0 -> 11.0.0 in `Directory.Packages.props`. `source/Directory.Build.props` `<Version>` untouched.
- Boy-scout: `ganda repo audit` raised an advisory `vscode-window-icon` warning (`.vscode/settings.json` missing). `ganda repo audit --fix` wrote that file. It is gitignored, so nothing to commit.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-19T03:58:53Z
- Reviewer: grok session 01a1f1f5-37bf-7640-b034-77d5dd3900d2 (2026-11-19). Effort 1, roster general.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1f1f5-37bf-7640-b034-77d5dd3900d2, max-turns 80, artifacts general.md, merged.md, disposition.md — 2026-11-19T04:03:35Z

## Results

Changed: `Directory.Packages.props`, where `Serilog.Extensions.Logging` goes from 10.0.0 to 11.0.0. No test file was added or edited. The only consumer of the package is the sample
`samples/endpoints/09-logging/serilog/structured-logging.cs`, and no Jaribu test covers it, so the sample run is the coverage.

Gates:
- `./bin/dev build`: pass (0 warnings, 0 errors)
- `dotnet run --file samples/endpoints/09-logging/serilog/structured-logging.cs -- order A1 9.99`: pass (exit 0, Serilog INF/DBG lines emitted; runfile `project.assets.json` resolves `Serilog.Extensions.Logging/11.0.0`)
- `ganda repo audit`: pass after `--fix` (first run: 1 advisory warning `vscode-window-icon`, fixed locally; rerun: "Repository passes all audit checks.")

### How to validate

Smoke:
```bash
dotnet run --file samples/endpoints/09-logging/serilog/structured-logging.cs -- order A1 9.99
```

Expect: exit 0. Output includes `[.. INF] Processing order A1 for $9.99` and `[.. INF] Order A1 completed successfully`.

Review disposition: round 1, effort 1, roster general. Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0 / 0 / 0; nit 0 / 0 / 0. Outcome: clean. The implementer restatement matches the original task. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done
