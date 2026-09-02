# Round 2 — general
**Date:** 2026-09-02
**Scope reviewed:** post-fix working tree of samples/aspire-otel/{apphost.cs,nuru-client.cs,overview.md,readme.md} vs origin/master

## Summary

M1–M6 remain fixed: wrong `_aspire-host-otel` paths, standalone `-- --interactive`, shebang/launch-profile/`nuru-repl-client` claims, invented `.aspire/settings.json`, mandatory `terminalCommandsEnabled`, and the restating AppHost comment are all corrected in the current tree. Two leftover doc defects remain in the fix delta: the File-Based Apps intro still shows a bare `./nuru-client.cs` that will not enter REPL, and `readme.md` mixes `cd samples/aspire-otel` with a repo-root-relative standalone `dotnet run` path.

## Prior findings

| ID | Prior status | Re-verify | Notes |
|----|--------------|-----------|-------|
| M1 | fixed | confirmed-fixed | No `_aspire-host-otel` left; overview uses `samples/aspire-otel` at lines 10 and 76. Directory does not exist under the old name. |
| M2 | fixed | confirmed-fixed | Step 2 standalone is `./nuru-client.cs --interactive` (overview:110). AppHost still `.WithArgs("--", "--interactive")` (apphost.cs:37). Related leftover bare invocation tracked as new Issue 1. |
| M3 | fixed | confirmed-fixed | Both runfiles and overview snippets show `#!/usr/bin/env -S dotnet --`. launchSettings described as optional `dotnet run --launch-profile …` helpers. `nuru-repl-client` qualified to launch-profile AppHost only (overview:125, 190). |
| M4 | fixed | confirmed-fixed | No `.aspire/settings.json` claim; overview notes directory discovery / `--apphost ./apphost.cs`. No `.aspire/` directory present. |
| M5 | fixed | confirmed-fixed | readme.md, apphost.cs header, and overview Step 1/2 split dashboard Terminal (no flag) from CLI attach (`terminalCommandsEnabled` then `aspire terminal attach`). |
| M6 | fixed | confirmed-fixed | Restating AppHost/`--interactive` comment gone; nuru-client.cs:47 is standalone guidance only. |

## Issues

### Issue 1 — Severity: bug
- File: samples/aspire-otel/overview.md:16
- Description: File-Based Apps intro still documents `./nuru-client.cs` with no args as the standalone run. `nuru-client.cs` does not set `AutoStartWhenEmpty`, and the generated REPL gate only accepts exact `["--interactive"]` or `["-i"]`, so this invocation never enters REPL (it falls through to normal CLI/help). Step 2 correctly uses `./nuru-client.cs --interactive`; the intro contradicts that and will mislead anyone copying the first block.
- Suggestion: Change line 16 to `./nuru-client.cs --interactive` (or `-i`), matching Step 2.
- Status: open

### Issue 2 — Severity: bug
- File: samples/aspire-otel/readme.md:9-19
- Description: The Run It block now starts with `cd samples/aspire-otel` then later runs `dotnet run samples/aspire-otel/nuru-client.cs -- greet Alice`. Following the block top-to-bottom, the cwd is already `samples/aspire-otel`, so that path resolves to a non-existent nested `samples/aspire-otel/samples/aspire-otel/...` and the standalone recipe fails. Pre-fix the block was entirely repo-root relative; the M5 rewrite introduced the `cd` without updating the standalone path.
- Suggestion: After the `cd`, use `./nuru-client.cs greet Alice` (or `dotnet run ./nuru-client.cs -- greet Alice`). Alternatively drop the `cd` and keep all commands repo-root relative.
- Status: open
