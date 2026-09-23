# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** local uncommitted MCP path-hardening diff (`github-cache-service.cs`, `get-example-tool.cs`, `cache-management-tool.cs`, mcp-07/mcp-08 tests)

## Summary

The change correctly introduces a shared `TryResolveRawContentUri` allowlist, safe cache ids/names, and invariant round-trip meta parsing. Literal `..`, schemes, and absolute roots are rejected; mcp-08 covers the stated M13 cases and tests pass. One encoding gap remains: percent-encoded `..` (`%2e%2e`) bypasses the string `..` check and can leave the `samples/` / `documentation/` trees while the post-resolve check only asserts the org/repo AbsolutePath prefix.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-mcp/services/github-cache-service.cs:122
- Description: `normalized.Contains("..")` does not see `%2e%2e` / `%2E%2E`. `Uri.TryCreate` normalizes those segments, so e.g. `samples/%2E%2E/x` resolves to `.../timewarp-nuru/master/x` and still passes `AllowedRepoPathPrefix`. That violates the requirement that paths stay under the allowlist (`samples/` / `documentation/`) even though cross-repo escape via deeper `..` is blocked by the AbsolutePath repo check. Verified with a local Uri probe; HttpClient would fetch the normalized AbsoluteUri.
- Suggestion: After resolve, require AbsolutePath under `/TimeWarpEngineering/timewarp-nuru/master/` + matched allowlist prefix; optionally reject `%` (or unescape then re-check `..`) fail-closed. Add a regression test for `%2e%2e`.
- Status: open
