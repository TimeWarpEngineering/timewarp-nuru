# Round 1 — security
**Date:** 2026-09-04
**Scope reviewed:** Whole-repo security pass on origin-home product tree (`38480f57` / product `648369f6`, `3.0.0-beta.77`). Surfaces: analyzer-host crash (dsl-interpreter, extractors), untrusted route/CLI input into generated C# / shell completion / FTS / Process, static mutable caches (MCP, REPL, search), secrets (REPL history, telemetry, SafeExceptionConverter, MCP/GitHub), completion templates, DevCli attestation (openssl argv + temp files), HTTP (NuGetVersionService, check-updates, MCP GitHub client), capabilities JSON deserialization. Re-verified 454 security claims (no rubber-stamp).

## Summary

454’s core security claims still hold: SQL is parameterized with a working FTS5 sanitizer; process launches go through Amuru `Shell.Builder` + `WithArguments` (no shell string concat); DSL interpreter cycle guard and fail-soft catches remain; MCP memory caches use `ConcurrentDictionary`; attestation verify shells openssl with argv into a 0700 temp dir and never touches `~/.timewarp/ganda/keys/`.

New defects found this round center on **local secret persistence**: REPL history (and the search index DB) are created world-readable (`0644`) despite an explicit history-security story (`repl-03b`, default ignore patterns). Secondary hardening gaps: MCP example fetch trusts remote manifest paths without a repo-prefix allowlist (Uri `../` normalization can leave `…/master/`), and DevCli NuGet flat-container URLs embed `packageId` without escaping (`--package` is a reachable CLI input).

## Re-verification of 454 security claims

- SQL injection:
  - **Still clean.** `SearchIndex` binds all user values with `AddWithValue` (`source/timewarp-nuru-search/services/search-index.cs:159-163`, `:189-196`, `:211-215`, `:254-271`, `:413`). Dynamic SQL only adds fixed clauses for optional filters (`:251-261`); `CA2100` is suppressed with that rationale (`:266-268`).
  - **454-023 still fixed.** `SanitizeFtsQuery` (`:295-309`) wraps each token as `"escaped"*` (internal `"` doubled) and `SearchAsync` early-returns on empty sanitized query (`:236-239`). `EscapeLikePattern` + `ESCAPE '\'` (`:259-260`, `:311-317`) still present. Not re-opened.

- Command injection:
  - **Still clean.** DevCli release/workflow, clipboard, capabilities client, and packable-project MSBuild all use `Shell.Builder(...).WithArguments(...)` (argv builders). Clipboard paste/copy pipes text via `WithStandardInput` rather than embedding in `-Command` (`source/timewarp-nuru/repl/input/repl-console-reader.clipboard.cs:95-106`, `:205-209`).
  - Attestation openssl: `Shell.Builder("openssl").WithArguments("pkeyutl", "-verify", "-pubin", "-inkey", pemPath, …)` (`tools/dev-cli/endpoints/workflow-command.cs:668-669`) — no shell metacharacter path. Temp dir via `Directory.CreateTempSubdirectory` (`:654`); verified 0700 on this host.

- Analyzer-host crash:
  - **454-003 still fixed.** `ResolveIdentifier` pre-caches `VariableState[symbol] = null` before evaluating (`source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:453-463`); re-entrant lookup returns the sentinel (`:456-457`).
  - **454-011 still present.** Fail-soft `catch (Exception ex) when (ex is not OperationCanceledException)` on interpreter entry points (`:71`, `:98`, `:140`, `:176`, `:241`). Extension-method lowerer uses a `visiting` `HashSet<IMethodSymbol>` cycle guard (`source/timewarp-nuru-analyzers/generators/extractors/extension-method-lowerer.cs:50`, `:82-84`).
  - Generated C# string embedding goes through `EmitterStringUtils.EscapeForStringLiteral` (help/completion emitters) — route/description text is not raw-injected into literals.

- Concurrency:
  - **454-024 still fixed.** `GitHubCacheService.MemoryCache` is `ConcurrentDictionary` (`source/timewarp-nuru-mcp/services/github-cache-service.cs:11`). `GetExampleTool.MemoryCache` likewise (`source/timewarp-nuru-mcp/tools/get-example-tool.cs:11`).
  - `ReplSession.CurrentSession` is a publish/clear mirror with `ReferenceEquals` guard so a concurrent session’s finally cannot null a newer instance (`source/timewarp-nuru/repl/repl-session.cs:112-131`). Not a mutable shared cache of command state.
  - `SearchIndex` is an instance DI service with one `SqliteConnection` — no static mutable index map found.

- Secrets:
  - MCP GitHub fetches use anonymous HTTPS to fixed `raw.githubusercontent.com/.../master/` / `api.github.com` hosts — **no GitHub token** in headers (no token-leak surface in MCP cache).
  - `SafeExceptionConverter.Read` throws `NotSupportedException` (`source/timewarp-nuru/serialization/safe-exception-converter.cs:12-13`) — no exception deserialization gadgets. Write omits `Data`/`TargetSite`.
  - Attestation `KnownKeys` holds **public** Ed25519 material only (`source/timewarp-nuru-devcli/content/any/services/attestation-verifier.cs:205-209`); comments explicitly forbid using `~/.timewarp/ganda/keys/` and require throwaway keys for tests (`:52-71`).
  - **New gaps:** history/index file modes and ignore-pattern coverage — see Issues.

## Areas reviewed as clean (with evidence)

| Area | Evidence |
|------|----------|
| Shell completion install path traversal via `appName` | Generated install routes use `Path.GetFileNameWithoutExtension(Environment.ProcessPath)` (`interceptor-emitter.cs` ~1177–1219), so `../` cannot appear in the completion filename from the built-in `--install-completion` path. |
| Bash/zsh/fish/pwsh callback argv | Templates invoke `{{APP_NAME}} __complete …` / `ProcessStartInfo` with `UseShellExecute = false` (`pwsh-completion-dynamic.ps1:13-22`). Word tokens are re-quoted in pwsh (`:18`). |
| check-updates HTTP | Emits HTTPS `api.github.com/repos/{owner}/{repo}/releases` only after `RepositoryUrl` matches a GitHub URL regex (`check-updates-emitter.cs:43`, `:72-80`, `:183-184`). TLS default (no custom cert bypass found). |
| Capabilities JSON | `CapabilitiesClient` deserializes with source-generated `CapabilitiesJsonSerializerContext` and catches `JsonException` (`capabilities-client.cs:40-60`). |
| DevCli attestation path / key handling | Pure `AttestationVerifier.Evaluate`; openssl only after `ReadyToVerify`; temp PEM/sig/payload under `CreateTempSubdirectory` then deleted (`workflow-command.cs:652-698`). No reads under `~/.timewarp/ganda/keys/`. |

## Issues

### Issue 1 — Severity: bug
- File: `source/timewarp-nuru/repl/repl-history.cs:187`
- Description: `Save()` persists history with `File.WriteAllLines(historyPath, merged)` and never sets a restrictive Unix mode. On Linux with a typical umask `022`, new files are `0644` (owner/group/other readable). Confirmed on this host: existing `~/.nuru/history/*` entries are `-rw-r--r--`, and a fresh `File.WriteAllLines` yields `OtherRead=True`. Default path is `~/.nuru/history/<app>` (`:205-224`) with `PersistHistory` defaulting to `true` (`source/timewarp-nuru/options/repl-options.cs:40`). Despite `repl-03b` / default `HistoryIgnorePatterns`, residual secrets that miss the patterns (or custom empty patterns) land in a world-readable file on multi-user machines.
- Suggestion: Create/replace the history file with `UnixFileMode.UserRead | UnixFileMode.UserWrite` (e.g. `FileStream` + `File.SetUnixFileMode`, or write-temp-then-replace). On Windows, rely on user-profile ACLs (document). Optionally `chmod` the `~/.nuru` / `history` directories to `0700` when creating them (`:219-220` currently `Directory.CreateDirectory` only). Add a `repl-03b` assertion on file mode after `Save()`.
- Status: open

### Issue 2 — Severity: suggestion
- File: `source/timewarp-nuru-search/services/database-path.cs:15` (DB created via connection open in `search-index.cs:14-24`)
- Description: Search index lives at `~/.nuru/index.db`. On this host the live DB is `-rw-r--r--`, and `~/.nuru` / `~/.nuru/history` are `drwxr-xr-x`. The DB stores CLI names, route patterns, descriptions, and full `capabilities_json` / `endpoint_json` (`search-index.cs:36-55`, `:155-162`). Same world-readable posture as history for any indexed CLI metadata.
- Suggestion: When creating the DB (or after first open), set file mode to owner-only; create `~/.nuru` as `0700`. Low urgency if the index never holds secrets, but it is the same trust directory as history.
- Status: open

### Issue 3 — Severity: suggestion
- File: `source/timewarp-nuru-mcp/tools/get-example-tool.cs:105-110`
- Description: `FetchFromGitHubAsync` builds `new Uri($"https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/{path}")` where `path` comes from the remote `samples/examples.json` manifest (`:69`, `:231-238`, `:281-283`) with **no allowlist** that the path stays under `samples/` (or even under this repo). .NET `Uri` normalizes `../` segments: `…/master/` + `../evil` → `…/timewarp-nuru/evil`; `…/master/` + `../../evil` → `…/TimeWarpEngineering/evil`. Absolute `https://evil.com/…` string-concat stays on githubusercontent as a path segment (not open SSRF to arbitrary hosts), but a poisoned/malicious manifest can still pull arbitrary **public** `raw.githubusercontent.com` content and present it as a Nuru example. `GitHubCacheService.FetchFromGitHubAsync` (`github-cache-service.cs:78-84`) has the same Uri shape; its current callers pass hardcoded `DocPath` constants (lower practical risk).
- Suggestion: Reject paths containing `..`, rooted/absolute URIs, or not starting with an allowlisted prefix (`samples/`, `documentation/`). Prefer `new Uri(baseUri, relative)` only after validation, and assert the resolved host+path still under the expected repo prefix. MCP is feature-frozen; this is a security/correctness harden only.
- Status: open

### Issue 4 — Severity: suggestion
- File: `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:40`
- Description: Flat-container URL is `$"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json"` with no `Uri.EscapeDataString` / package-id charset check. Verified: `packageId = "../evil"` normalizes to `https://api.nuget.org/evil/index.json` (escapes the `v3-flatcontainer` segment). Reachable from DevCli `--package` / `checkVersionConfig.packages` (`check-version-command.cs:7-14`, `:61-66`, `:113-114`) as well as derived PackageIds. Host stays `api.nuget.org` (not arbitrary-host SSRF), but path confusion can yield misleading empty/wrong version results or hit unintended API paths.
- Suggestion: Validate package IDs against NuGet’s id grammar (or reject `/`, `\`, `..`, `?`, `#`, whitespace) and/or `Uri.EscapeDataString` each segment. Keep HTTPS-only to `api.nuget.org`.
- Status: open

### Issue 5 — Severity: suggestion
- File: `source/timewarp-nuru/options/repl-options.cs:92-100`
- Description: Default `HistoryIgnorePatterns` cover `*password*`, `*secret*`, `*token*`, `*apikey*`, `*credential*`, and `clear-history`. `repl-03b-history-security.cs` exercises those defaults but does **not** cover common secret shapes that miss them, e.g. bare `Bearer eyJ…`, `Authorization: …`, `api_key=…` / `api-key=…` (underscore/hyphen break `*apikey*`), or provider prefixes like `sk-…`. Combined with Issue 1’s world-readable files, missed lines are more exposed.
- Suggestion: Extend defaults (e.g. `*bearer*`, `*authorization*`, `*api[_-]key*`, `*sk-*`) and add `repl-03b` cases. Document that ignore patterns are best-effort and `PersistHistory=false` / custom path ACLs are the hard control.
- Status: open

### Issue 6 — Severity: nit
- File: `source/timewarp-nuru/completion/completion/templates/pwsh-completion-dynamic.ps1:14` (filled by `dynamic-completion-script-generator.cs:38-41`)
- Description: `{{APP_PATH}}` is substituted from `Environment.ProcessPath` with no PowerShell escaping. Values are placed inside `"…"` for `$psi.FileName`. A path containing `"` (illegal on Windows; exotic on Unix) could break out of the string. Practical risk is near-zero for normal install layouts; bash/zsh/fish embed only the basename as a command word.
- Suggestion: Escape `'`/`"`/`$`/`\`` in `APP_PATH` (and optionally validate `APP_NAME` is a safe identifier) before template substitution.
- Status: open

### Issue 7 — Severity: suggestion
- File: `source/timewarp-nuru/telemetry/telemetry-behavior.cs:58-60` (mirrored in generated path `telemetry-emitter.cs:120`)
- Description: On failure, telemetry sets Activity status/detail and tag `error.message` to `ex.Message`. If OTLP export is enabled (`NuruTelemetryOptions.OtlpEndpoint` / `OTEL_EXPORTER_OTLP_ENDPOINT`), exception text that embeds user argv or secrets can leave the process. Export is opt-in; tags do not include raw argv today (`command.name` / `command.type` only at `:35-36`).
- Suggestion: Prefer `error.type` only by default, or redact/truncate `error.message` when exporting. Document that OTLP sinks must be trusted.
- Status: open
