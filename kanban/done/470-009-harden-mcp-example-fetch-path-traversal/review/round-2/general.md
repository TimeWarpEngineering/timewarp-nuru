# Round 2 — general
**Date:** 2026-09-23
**Scope reviewed:** post-fix delta for M1 (`TryResolveRawContentUri` percent-encoding + resolved allowlist AbsolutePath; mcp-08 regression)

## Summary

Re-verified M1 against the updated guard: `%` is rejected fail-closed, and resolved AbsolutePath must remain under `master/<matchedPrefix>`. mcp-08 (9), mcp-07 (5), and mcp-01 (12) all pass. No new issues on the fix delta.

## Issues

### M1 — Severity: bug
- File: source/timewarp-nuru-mcp/services/github-cache-service.cs:117-171
- Description: Prior percent-encoded `..` allowlist bypass.
- Suggestion: (implemented) reject `%`; assert AbsolutePath under `master/<prefix>`; regression test.
- Status: fixed
