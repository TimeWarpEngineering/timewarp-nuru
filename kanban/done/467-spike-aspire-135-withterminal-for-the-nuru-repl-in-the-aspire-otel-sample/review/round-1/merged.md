# Round 1 — merged findings
**Date:** 2026-09-02
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 4 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: samples/aspire-otel/overview.md:10
- Description: Run instructions still say `cd samples/_aspire-host-otel`. That directory does not exist (renamed long ago to `samples/aspire-otel`). The same wrong path is repeated at line 79 in Step 1, while Step 2’s standalone block correctly uses `samples/aspire-otel`. Anyone following the updated WithTerminal flow hits a missing directory before they can `aspire run`.
- Suggestion: Replace every `_aspire-host-otel` occurrence with `aspire-otel` (lines 10 and 79 at minimum).
- Source: general
- Disposition notes: Replaced both `_aspire-host-otel` paths with `aspire-otel` in File-Based Apps and Step 1.

### M2 — Severity: bug — Status: fixed
- File: samples/aspire-otel/overview.md:117
- Description: Newly added standalone guidance is `./nuru-client.cs -- --interactive`. With the runfile shebang `#!/usr/bin/env -S dotnet --`, that passes argv `["--", "--interactive"]` to the app. Nuru’s generated REPL gate only accepts exact `["--interactive"]` or `["-i"]`, so this invocation will not enter REPL. The `--` separator is required for Aspire/`dotnet run --file`, not for `./nuru-client.cs`.
- Suggestion: Document `./nuru-client.cs --interactive` (or `-i`). If showing `dotnet run …`, keep `--` only on that form: `dotnet run ./nuru-client.cs -- --interactive`.
- Source: general
- Disposition notes: Standalone docs now use `./nuru-client.cs --interactive`; AppHost `WithArgs("--", "--interactive")` left unchanged.

### M3 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/overview.md:24
- Description: The File-Based Apps and Shared Launch Settings sections still claim shebangs of `dotnet run --launch-profile http|AppHost` (lines 24–25, 191–193). Both runfiles actually use `#!/usr/bin/env -S dotnet --` with no launch profile. Task evidence also shows Aspire launches the client with `--no-launch-profile`, so the `AppHost` profile’s `OTEL_SERVICE_NAME=nuru-repl-client` is not applied for the managed REPL either. Line 132’s claim that a bare `./nuru-client.cs` session “still shows as `nuru-repl-client`” is therefore false under the current shebang.
- Suggestion: Align the docs with the real shebangs. Describe launchSettings as optional/`dotnet run --launch-profile …` helpers for standalone OTLP, and qualify the `nuru-repl-client` resource name accordingly. Note that Aspire-managed `nuruclient` gets OTLP from the AppHost injection path, not from the unused shebang profile.
- Source: general
- Disposition notes: Documented real `dotnet --` shebang; launchSettings as optional standalone helpers; qualified `nuru-repl-client` and AppHost OTLP injection.

### M4 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/overview.md:19
- Description: States that `aspire run` uses `.aspire/settings.json` to locate the AppHost, but `samples/aspire-otel/.aspire/settings.json` is not present. `aspire run` can discover/`--apphost` an AppHost in the directory without that file; the claim invents a missing setup artifact.
- Suggestion: Drop the settings.json sentence, or add a real `.aspire/settings.json` if the sample intends to rely on one.
- Source: general
- Disposition notes: Dropped the `.aspire/settings.json` claim; noted directory discovery / `--apphost ./apphost.cs` instead.

### M5 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/readme.md:10
- Description: DX overstatement: `aspire config set features.terminalCommandsEnabled true` is listed as a mandatory prelude to `aspire run` (also apphost.cs:15 and overview’s Step 1 flow). Aspire’s WithTerminal docs gate only the `aspire terminal` CLI behind that flag; the dashboard Terminal view works without it. Prior `dotnet run samples/aspire-otel/apphost.cs` / `./apphost.cs` still starts the AppHost and PTY for dashboard use, but is no longer mentioned.
- Suggestion: Split steps: run AppHost (`aspire run` or `./apphost.cs`); enable `features.terminalCommandsEnabled` only when using `aspire terminal attach` / `ps`. Keep dashboard Terminal as the zero-config path.
- Source: general
- Disposition notes: Split run vs CLI-attach in readme.md, apphost.cs header, and overview Step 1/2; flag only for `aspire terminal attach`/`ps`.

### M6 — Severity: suggestion — Status: fixed
- File: samples/aspire-otel/nuru-client.cs:47
- Description: Comment “AppHost passes --interactive so this process enters REPL under the PTY.” restates the AppHost wiring (WHAT) rather than a non-obvious constraint. The following standalone guidance line is useful; this one is narrating the change.
- Suggestion: Drop line 47, or replace with a WHY note only if keeping something about PTY/`--interactive` coupling that is not already in apphost.cs.
- Source: general
- Disposition notes: Removed the restating AppHost/`--interactive` comment; kept the standalone guidance line.

## Duplicates / conflicts

- None. Six distinct findings from the single general reviewer.
