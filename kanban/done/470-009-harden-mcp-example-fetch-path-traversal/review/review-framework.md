# Review framework — task 470-009

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-009-harden-mcp-example-fetch-path-traversal/
**Diff scope:** local uncommitted — MCP GitHub fetch path allowlist, cache name sanitization, invariant meta parse; tests mcp-07/mcp-08
**Plan / brief:** Harden `FetchFromGitHubAsync` / cache against path traversal (parent 470 M13) and fix culture-sensitive meta timestamps (M40). No new MCP features.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task-work tw-implementation-review)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
