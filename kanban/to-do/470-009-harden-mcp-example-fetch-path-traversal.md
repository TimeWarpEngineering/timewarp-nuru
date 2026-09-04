# Harden MCP example fetch path traversal

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M13). Nit folded: M40.

## Description

MCP is frozen for features (2026-07-14). This is a correctness/security bug only.

`FetchFromGitHubAsync` (`get-example-tool.cs:105-110`, `github-cache-service.cs:78-83`) builds `new Uri(base + path)` with no check that `path` stays under the repo tree. .NET Uri normalizes `..`: a manifest `Path` of `../../../evil-org/evil-repo/main/secret` leaves `timewarp-nuru/master/`. Example paths come from remote `samples/examples.json`.

Disk cache uses `Path.Combine(CacheDirectory, $"{name}.cache")` without rejecting `../` in `name`.

M40: meta timestamps written with `"O"` but read with culture-sensitive `DateTime.TryParse`.

Do not add MCP features or revive 454-033 examples-manifest work.

## Requirements

- Reject paths/ids containing `..`, absolute roots, or URI schemes; require Path under `samples/` (or an allowlist).
- Assert the resolved URI host+path still under `raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/`.
- Sanitize cache file names (hash or GetSafeCacheFileName) instead of raw ids.
- Parse meta with InvariantCulture + RoundtripKind (M40).
- Tests (mcp-07 style).

## Checklist

- [ ] Path/URI allowlist (M13)
- [ ] Cache name sanitization
- [ ] Meta timestamp parse (M40)
- [ ] Tests
- [ ] No new MCP features

## Notes

Evidence: parent 470 `review/round-1/merged.md` M13, M40. Sources: aux + security (collapsed).
