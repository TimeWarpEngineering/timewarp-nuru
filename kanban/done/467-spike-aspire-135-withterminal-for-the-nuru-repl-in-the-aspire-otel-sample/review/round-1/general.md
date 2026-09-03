# Round 1 — general
**Date:** 2026-09-02
**Scope reviewed:** samples/aspire-otel/{apphost.cs,nuru-client.cs,overview.md,readme.md} vs origin/master

## Summary

The AppHost wiring is sound: SDK pin `13.5.3`, `.WithArgs("--", "--interactive").WithTerminal()`, and experimental suppressions match the spike verdict and Aspire’s `dotnet run --file … -- --interactive` launch line. Risk is concentrated in sample docs that still point at renamed paths, invent a required feature-flag step for dashboard use, and newly document a standalone REPL invocation that cannot enter REPL. Product code for the sample itself looks correct; the DSR leak is documented without over-claiming.

## Issues

### Issue 1 — Severity: bug
- File: samples/aspire-otel/overview.md:10
- Description: Run instructions still say `cd samples/_aspire-host-otel`. That directory does not exist (renamed long ago to `samples/aspire-otel`). The same wrong path is repeated at line 79 in Step 1, while Step 2’s standalone block correctly uses `samples/aspire-otel`. Anyone following the updated WithTerminal flow hits a missing directory before they can `aspire run`.
- Suggestion: Replace every `_aspire-host-otel` occurrence with `aspire-otel` (lines 10 and 79 at minimum).
- Status: open

### Issue 2 — Severity: bug
- File: samples/aspire-otel/overview.md:117
- Description: Newly added standalone guidance is `./nuru-client.cs -- --interactive`. With the runfile shebang `#!/usr/bin/env -S dotnet --`, that passes argv `["--", "--interactive"]` to the app. Nuru’s generated REPL gate only accepts exact `["--interactive"]` or `["-i"]` (`interceptor-emitter.cs` emits `routeArgs is ["--interactive"] or ["-i"]`), so this invocation will not enter REPL. The `--` separator is required for Aspire/`dotnet run --file`, not for `./nuru-client.cs`.
- Suggestion: Document `./nuru-client.cs --interactive` (or `-i`). If showing `dotnet run …`, keep `--` only on that form: `dotnet run ./nuru-client.cs -- --interactive`.
- Status: open

### Issue 3 — Severity: suggestion
- File: samples/aspire-otel/overview.md:24
- Description: The File-Based Apps and Shared Launch Settings sections still claim shebangs of `dotnet run --launch-profile http|AppHost` (lines 24–25, 191–193). Both runfiles actually use `#!/usr/bin/env -S dotnet --` with no launch profile. Task evidence also shows Aspire launches the client with `--no-launch-profile`, so the `AppHost` profile’s `OTEL_SERVICE_NAME=nuru-repl-client` is not applied for the managed REPL either. Line 132’s claim that a bare `./nuru-client.cs` session “still shows as `nuru-repl-client`” is therefore false under the current shebang.
- Suggestion: Align the docs with the real shebangs. Describe launchSettings as optional/`dotnet run --launch-profile …` helpers for standalone OTLP, and qualify the `nuru-repl-client` resource name accordingly. Note that Aspire-managed `nuruclient` gets OTLP from the AppHost injection path, not from the unused shebang profile.
- Status: open

### Issue 4 — Severity: suggestion
- File: samples/aspire-otel/overview.md:19
- Description: States that `aspire run` uses `.aspire/settings.json` to locate the AppHost, but `samples/aspire-otel/.aspire/settings.json` is not present. `aspire run` can discover/`--apphost` an AppHost in the directory without that file; the claim invents a missing setup artifact.
- Suggestion: Drop the settings.json sentence, or add a real `.aspire/settings.json` if the sample intends to rely on one.
- Status: open

### Issue 5 — Severity: suggestion
- File: samples/aspire-otel/readme.md:10
- Description: DX overstatement: `aspire config set features.terminalCommandsEnabled true` is listed as a mandatory prelude to `aspire run` (also apphost.cs:15 and overview’s Step 1 flow). Aspire’s WithTerminal docs gate only the `aspire terminal` CLI behind that flag; the dashboard Terminal view works without it. Prior `dotnet run samples/aspire-otel/apphost.cs` / `./apphost.cs` still starts the AppHost and PTY for dashboard use, but is no longer mentioned.
- Suggestion: Split steps: run AppHost (`aspire run` or `./apphost.cs`); enable `features.terminalCommandsEnabled` only when using `aspire terminal attach` / `ps`. Keep dashboard Terminal as the zero-config path.
- Status: open

### Issue 6 — Severity: suggestion
- File: samples/aspire-otel/nuru-client.cs:47
- Description: Comment “AppHost passes --interactive so this process enters REPL under the PTY.” restates the AppHost wiring (WHAT) rather than a non-obvious constraint. The following standalone guidance line is useful; this one is narrating the change.
- Suggestion: Drop line 47, or replace with a WHY note only if keeping something about PTY/`--interactive` coupling that is not already in apphost.cs.
- Status: open
