# Round 3 — general
**Date:** 2026-09-02
**Scope reviewed:** post-M7/M8 working tree of samples/aspire-otel/{apphost.cs,nuru-client.cs,overview.md,readme.md} vs origin/master

## Summary

M1–M8 all remain fixed after the M7/M8 doc corrections. The File-Based Apps intro now uses `./nuru-client.cs --interactive`, and the readme standalone recipe after `cd samples/aspire-otel` is `./nuru-client.cs greet Alice`. No new defects found in the fix delta; the documented DSR leak was not re-raised.

## Prior findings

| ID | Prior status | Re-verify | Notes |
|----|--------------|-----------|-------|
| M1 | fixed | confirmed-fixed | No `_aspire-host-otel` anywhere under samples/aspire-otel. overview.md uses `cd samples/aspire-otel` at lines 10, 76, 109; apphost.cs header at 14. |
| M2 | fixed | confirmed-fixed | Step 2 standalone is `./nuru-client.cs --interactive` (overview.md:110). AppHost still `.WithArgs("--", "--interactive")` (apphost.cs:37; overview snippet:146). |
| M3 | fixed | confirmed-fixed | Real shebang `#!/usr/bin/env -S dotnet --` in apphost.cs:1, nuru-client.cs:1, and overview.md:22/136. launchSettings described as optional (overview.md:25, 163–167). `nuru-repl-client` qualified to launch-profile AppHost only (overview.md:125, 190). |
| M4 | fixed | confirmed-fixed | No `.aspire/settings.json` claim. overview.md:12 notes directory discovery / `--apphost ./apphost.cs`. |
| M5 | fixed | confirmed-fixed | Dashboard Terminal needs no flag; `terminalCommandsEnabled` only for CLI attach in readme.md:12–15, apphost.cs:17–20, overview.md:97–103. |
| M6 | fixed | confirmed-fixed | Restating AppHost/`--interactive` comment gone; nuru-client.cs:47 is standalone guidance only. |
| M7 | fixed | confirmed-fixed | File-Based Apps intro standalone is `./nuru-client.cs --interactive` (overview.md:16), matching Step 2. |
| M8 | fixed | confirmed-fixed | After `cd samples/aspire-otel` (readme.md:9), standalone is `./nuru-client.cs greet Alice` (readme.md:18–19). |

## Issues

<!-- New findings only. Zero issues is valid. -->
