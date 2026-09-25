# Round 1 — aux
**Date:** 2026-09-04
**Scope reviewed:** `source/timewarp-nuru-devcli/` (endpoints: check-version, release, clean, self-install; services: nuget-version, publish-state, release-guard, packable-project, nupkg-layout, ci-run-promotion, tag-assertion, attestation-verifier/config, repo-config); `tools/dev-cli/` (especially `workflow-command.cs` attestation openssl + release promote path); `source/timewarp-nuru-search/` (search-index, capabilities-client, search/index endpoints); `source/timewarp-nuru-build/` (`GenerateNuruJsonContextTask.cs`, targets); `source/timewarp-nuru-mcp/` (github-cache-service, get-example-tool, cache-management, validate/generate tools — correctness/security only); tests under `tests/timewarp-nuru-tests/devcli/`, `tests/timewarp-nuru-search-tests/`, `tests/timewarp-nuru-mcp-tests/`

## Summary

454-022 / 454-024 remediations are intact (full-list `IsVersionPublished`, SemVer §11 `CompareVersions`, MCP `ConcurrentDictionary` + non-colliding cache names). 454-023’s FTS quoting and LIKE `ESCAPE` are in place, but a residual NUL-byte crash remains. New defects cluster around release-gate fail-open on NuGet HTTP errors, divergent Version trimming in `check-version`, search `--version` printing the wrong field, and MCP example-fetch URL/disk path traversal when `examples.json` supplies `..` segments. Attestation pure verifier and openssl orchestration look sound (no signing-key misuse). Build JSON-context task is intentionally fail-soft.

## 454 regression check

| 454 ID | Still present? | Evidence |
|--------|----------------|----------|
| 454-022 (M22 + CompareVersions) | no | `nuget-version-service.cs:102-116` `IsVersionPublished` walks the full list via `CompareVersions == 0`; SemVer §11 pre-release ranking at `:59-94` (release > pre, numeric ids, prefix rule, case-insensitive alnum). `check-version-command.cs:129` and `release-command.cs:311` call `IsVersionPublished`. Covered by `tests/.../devcli/check-version-01-version-comparison.cs` (beta.9 &lt; beta.10, full-list membership, huge numeric ids). Display still uses `versions[^1]` at `check-version-command.cs:122` only for “latest” UX after CompareVersions aggregation — not for membership. |
| 454-023 (M23 + LIKE) | partially | Quoting/escape fixed: `SanitizeFtsQuery` double-quotes tokens (`search-index.cs:295-309`); `EscapeLikePattern` + `ESCAPE '\\'` (`:257-260`, `:311-317`); empty sanitized query early-returns (`:236-239`). Tests in `search-01-fts-sanitizer.cs` cover `(`, `***`, quotes, LIKE wildcards, and live FTS5 execute. **Residual:** embedded U+0000 still produces MATCH expressions that throw `SqliteException: unterminated string` (verified against current sanitizer logic); `SearchAsync` does not catch (`:273`). See Issue 4. |
| 454-024 (M24 + filename collision) | no | `github-cache-service.cs:3,11` uses `ConcurrentDictionary`; `GetSafeCacheFileName` keeps full relative path + extension (`:134-142`). `get-example-tool.cs:9-11` likewise ConcurrentDictionary for example memory cache. Tests in `mcp-07-cache-filename.cs`. |
| 454-031 (LOW aux sweep) | no | Folded into 454-022/023/024 as noted in 454 task checklist; cited LOW items (CompareVersions pre-release, LIKE wildcards, cache filename collision) are addressed in the files above. |

## Issues

### Issue 1 — Severity: bug
- File: `source/timewarp-nuru-devcli/content/any/services/nuget-version-service.cs:43-46`
- Description: `GetPackageVersionsAsync` returns `[]` for **every** non-success HTTP status. NuGet flat-container 404 for a never-published package ID is correctly “no versions,” but 429/5xx/auth errors are indistinguishable from that empty set. Callers treat empty as “not published”: `check-version-command.cs:117-120` `continue`s, so `alreadyPublished` stays empty → `PublishState.None` → “safe to release”; `release-command.cs:307-317` and workflow’s check-version step (`tools/dev-cli/endpoints/workflow-command.cs:378-385`) inherit the same fail-open. A transient NuGet outage can therefore clear the already-released gate and allow tagging/publishing of a version that is already on the feed (`--skip-duplicate` only no-ops at push time; the tag/GitHub Release still get cut).
- Suggestion: Treat only 404 (and maybe 400 for illegal IDs) as empty; for other statuses throw or return a distinct error so check-version/release abort fail-closed. Dispose `HttpResponseMessage` in a `using` while there.
- Status: open

### Issue 2 — Severity: bug
- File: `source/timewarp-nuru-devcli/content/any/endpoints/check-version-command.cs:202-203`
- Description: `GetVersionFromSource` returns `XElement.Value` **without** `.Trim()`. The twin in `release-command.cs:464-465` and workflow `ReadPropsVersion` (`workflow-command.cs:804`) both trim. A `<Version>` element with incidental whitespace/newlines (legal in MSBuild XML) yields a source string that never `CompareVersions`-equals NuGet’s trimmed versions → `IsVersionPublished` false → check-version reports safe while `dev release` / CI (trimmed) correctly refuse or use the real version. Divergent helpers invite exactly this gate skew.
- Suggestion: Trim in check-version (or extract one shared props-version reader used by check-version, release, and workflow).
- Status: open

### Issue 3 — Severity: bug
- File: `source/timewarp-nuru-search/endpoints/search-query.cs:9-10,70`
- Description: `--version` is documented as “Show CLI version in results,” but the formatter emits `result.Endpoint.Kind` (`[{CliName}@{Kind}]`), not the indexed CLI version. `SearchResult` (`search-index.cs:426-433`) has no `Version` field; version lives on `CliInfo` / the `clis` table (`:436-440`). Operators asking for version get endpoint kind instead.
- Suggestion: Join/select `clis.version` in `SearchAsync` (or look it up), put it on `SearchResult`, and print that when `--version` is set.
- Status: open

### Issue 4 — Severity: bug
- File: `source/timewarp-nuru-search/services/search-index.cs:295-309` (crash surface `:273`)
- Description: `SanitizeFtsQuery` only escapes embedded `"` for FTS5 quoting. A user token containing U+0000 (e.g. `hello\0world`) is wrapped as `"hello\0world"*`; SQLite FTS5 then fails with `unterminated string`. `SearchAsync` runs `ExecuteReaderAsync` with no catch, so the CLI throws instead of returning “No results found” — same failure class as original M23, for a different character class. Reproduced with the current sanitizer against Microsoft.Data.Sqlite; Japanese/emoji tokens are fine.
- Suggestion: Strip or reject `\0` (and optionally other C0 controls) inside `SanitizeFtsQuery`, and/or catch `SqliteException` around MATCH and return empty results.
- Status: open

### Issue 5 — Severity: bug
- File: `source/timewarp-nuru-mcp/tools/get-example-tool.cs:105-110` (also `github-cache-service.cs:78-83`)
- Description: `FetchFromGitHubAsync` builds `new Uri(base + path)` with no validation that `path` stays under the repo tree. .NET `Uri` normalizes `..` segments: `../../../evil-org/evil-repo/main/secret` resolves to `https://raw.githubusercontent.com/evil-org/evil-repo/main/secret` (verified). Example `Path` values come from remote `samples/examples.json` (`:225-231`, `:281-284`). A compromised or malicious manifest can SSRF the MCP process to arbitrary `raw.githubusercontent.com` content. Shared `GitHubCacheService.FetchFromGitHubAsync` has the same join pattern; today’s tool callers pass hardcoded `DocPath` constants (lower practical risk), but the API is still unsafe if ever fed untrusted relative paths. Disk cache uses `Path.Combine(CacheDirectory, $"{name}.cache")` (`:117`, `:153`) without rejecting `../` in `name` — a manifest `Id` containing `../` can write/read outside the examples cache directory once the name passes `TryGetValue`.
- Suggestion: Reject paths/ids containing `..`, absolute roots, or URI schemes; require `Path` under `samples/`; resolve and assert the final URI host/path still under `raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/`; sanitize cache file names (e.g. hash or `GetSafeCacheFileName`) instead of raw ids.
- Status: open

### Issue 6 — Severity: suggestion
- File: `source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs:126-140,78-88`
- Description: On MSBuild exit 0, `ParseGetPropertyOutput` returns `(false, null)` for missing/malformed JSON rather than throwing. That project is then silently omitted from the derived packable set used by check-version, release, and promote verification. Exit ≠ 0 already fails loud (`:73-76`); blank PackageId with IsPackable fails loud (`:82-85`); duplicate IDs fail loud (`:100-114`). A successful-but-unparseable evaluation is the remaining silent-drop path and can ship an incomplete package set under an otherwise green release.
- Suggestion: If exit code is 0 but Properties cannot be parsed, throw naming the project (same fail-loud posture as the other guards).
- Status: open

### Issue 7 — Severity: suggestion
- File: `source/timewarp-nuru-build/GenerateNuruJsonContextTask.cs:58-70`
- Description: Any exception in `ExecuteCore` is logged as a warning and the task returns `true` with empty `GeneratedFiles`, relying on runtime `ToString()` fallback. That avoids breaking builds, but a real extraction/generation bug becomes a silent behavioral downgrade (user DTOs printed via ToString instead of JSON) with no hard signal in CI.
- Suggestion: Keep fail-soft for expected “no DSL in this compilation unit” cases; for unexpected exceptions consider `Log.LogError` + `return false` behind a property, or emit a non-fatal but highly visible diagnostic that CI can trend.
- Status: open

### Issue 8 — Severity: nit
- File: `source/timewarp-nuru-mcp/services/github-cache-service.cs:103-104` (same pattern `get-example-tool.cs:126-127`, `cache-management-tool.cs:110`)
- Description: Meta timestamps are written with `"O"` (`github-cache-service.cs:128`) but read with `DateTime.TryParse` without `CultureInfo.InvariantCulture` / `DateTimeStyles.RoundtripKind`. Usually works for round-trip ISO-8601; culture-sensitive parse is unnecessarily fragile for a machine-written stamp.
- Suggestion: Use `DateTime.TryParse(meta, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out ...)`.
- Status: open

### Issue 9 — Severity: nit
- File: `source/timewarp-nuru-devcli/content/any/endpoints/self-install-command.cs:52-108`
- Description: On Windows, a successful publish leaves `{outputDir}/dev.exe.old` in place after renaming the previous binary (`:66-67`). Next install deletes it only as a pre-step (`:60-64`). Not destructive, but litter after every successful self-install.
- Suggestion: Delete `oldExe` after a successful publish (best-effort), matching the failure-path rollback care already present.
- Status: open
