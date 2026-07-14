# Sweep Aux Project Low Severity Findings

Parent: 454 (2026-07-06 full code review). Severity: LOW (batch).

## Description

Remaining low-severity aux-project findings (MCP cache filename collision → 454-024;
CompareVersions pre-release ordering → 454-022; FTS LIKE wildcards → 454-023):

This task is the umbrella for verifying those three cross-referenced LOW items landed
with their sibling tasks, plus any leftover aux cleanups discovered while fixing them.
If all three are covered by 454-022/023/024, close this as verification-only.

## Checklist

- [x] 454-022 covered CompareVersions pre-release ordering (verified: full SemVer 2.0 precedence in nuget-version-service.cs)
- [x] 454-023 covered LIKE wildcard escaping (verified: EscapeLikePattern + ESCAPE clause in search-index.cs)
- [x] 454-024 covered disk-cache filename collision (verified: GetSafeCacheFileName keys on full relative path)
- [x] No other orphaned aux findings remain

## Resolution (2026-07-14) — verification-only

All three cross-referenced LOW findings were verified fixed at the code level in their
sibling tasks; no leftover aux findings remain, so this umbrella closes as verification-only:
- CompareVersions pre-release ordering → 454-022 (nuget-version-service.cs: SemVer 2.0
  precedence, GetCoreVersion/GetPreRelease/ComparePreRelease; no-prerelease > prerelease).
- FTS/LIKE wildcard escaping → 454-023 (search-index.cs: EscapeLikePattern + `ESCAPE '\'`).
- MCP disk-cache filename collision → 454-024 (github-cache-service.cs: GetSafeCacheFileName
  keys on the full relative path).
