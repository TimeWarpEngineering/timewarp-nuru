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

- [x] Path/URI allowlist (M13)
- [x] Cache name sanitization
- [x] Meta timestamp parse (M40)
- [x] Tests
- [x] No new MCP features

## Notes

Evidence: parent 470 `review/round-1/merged.md` M13, M40. Sources: aux + security (collapsed).

Implementation review (effort 1, general): `review/` — framework, round-1/2, disposition `clean`. Session: review-oracle (ganda task-work tw-implementation-review).

## Results

### What was implemented

Hardened MCP GitHub fetch and disk cache against path traversal (M13) and fixed culture-sensitive meta timestamp parsing (M40). No new MCP features.

- **Path/URI allowlist (`GitHubCacheService.TryResolveRawContentUri`)**: Rejects `..`, `%` (percent-encoding), absolute roots, and URI schemes; requires an allowlist prefix (`samples/` for examples; `samples/` + `documentation/` for shared cache fetches); asserts resolved URI is `https` on `raw.githubusercontent.com` with AbsolutePath under `/TimeWarpEngineering/timewarp-nuru/` **and** under `master/<matched allowlist prefix>` after Uri normalization.
- **Manifest filter**: `ParseManifest` drops entries whose `Id` fails `IsSafeCacheId` or whose `Path` fails the samples allowlist before they can drive fetch or cache keys.
- **Cache name sanitization**: `GetExampleTool` disk read/write uses `GetSafeCacheFileName`; pathological `..` / rooted names hash to SHA256 hex so `Path.Combine` cannot escape the cache directory.
- **M40**: Meta stamps written/read with `CultureInfo.InvariantCulture` + `DateTimeStyles.RoundtripKind` in `github-cache-service`, `get-example-tool`, and `cache-management-tool`.

### Files changed

- `source/timewarp-nuru-mcp/services/github-cache-service.cs` — path guard, safe id check, GetSafeCacheFileName hash fallback, invariant meta parse
- `source/timewarp-nuru-mcp/tools/get-example-tool.cs` — samples-only fetch guard, safe cache names, manifest filter, invariant meta parse
- `source/timewarp-nuru-mcp/tools/cache-management-tool.cs` — invariant meta parse
- `tests/timewarp-nuru-mcp-tests/mcp-07-cache-filename.cs` — hash-on-dotdot case
- `tests/timewarp-nuru-mcp-tests/mcp-08-path-traversal.cs` — new allowlist / traversal unit tests (incl. `%2E%2E` regression)
- `source/timewarp-nuru-mcp/internals-visible-to.g.cs` (+ nuru/parsing siblings) — regenerated for `mcp-08-path-traversal`
- `kanban/to-do/470-009-harden-mcp-example-fetch-path-traversal/review/` — implementation review trail

### How to validate

**Smoke:**
1. `dotnet run tests/timewarp-nuru-mcp-tests/mcp-08-path-traversal.cs`
2. `dotnet run tests/timewarp-nuru-mcp-tests/mcp-07-cache-filename.cs`
3. `dotnet run tests/timewarp-nuru-mcp-tests/mcp-01-example-retrieval.cs`

**Expect:**
1. mcp-08: 9 passed (rejects `../../../evil-org/...`, `%2E%2E` leaving samples, schemes, absolute roots, non-samples prefix; accepts samples/docs under repo URI; sanitizes cache ids/names).
2. mcp-07: 5 passed including hash-on-`../outside`.
3. mcp-01: 12 passed (live GitHub example list/fetch still works with samples allowlist).

### Implementation review disposition

- **Effort / roster:** 1 — `general`
- **Rounds:** 2
- **Final counts:** bug 0 open / 1 fixed; suggestion 0; nit 0
- **Disposition:** `clean` (M1 percent-encoded `..` allowlist bypass fixed on this task; round 2 confirmed)
- **Artifacts:** `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`
