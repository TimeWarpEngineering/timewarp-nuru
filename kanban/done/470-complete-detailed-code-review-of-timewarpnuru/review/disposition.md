# Disposition — task 470

**Date:** 2026-09-04
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count on this review ledger:** 0 remaining unfiled (opens are `--parent 470` children or `wontfix`)

## Summary

Round 1 used the elevated 7-area roster (not default effort-1) against origin-home `38480f57` / product `648369f6` (`3.0.0-beta.77`). 454-done remediations generally hold. New defects are independent product work, so they are filed as children on this id (454 HIGH 1:1 / MEDIUM grouped model) rather than fixed on the review branch. Parent 470 stays in-progress until those children land. **454-019** remains on its existing to-do kitchen (not duplicated as an M#). One suggestion (**M20**, source-gen `IEnumerable<T>` DI) is `wontfix` here because epic **391** already owns it.

Parsing crash-safety: 50,000 malformed patterns, seed `470001`, **0** uncaught exceptions via `PatternParser.TryParse` + `Parse`.

## Child split (filed `--parent 470`)

| Child | Merged IDs | Title |
|-------|------------|-------|
| 470-001 | M1 M2 M3 M34 M35 | Wire or obsolete stub NuruAppBuilder configure APIs |
| 470-002 | M4 | Fix REPL HandleCharacter unclamped selection crash |
| 470-003 | M5 M36 | Normalize Windows CRLF clipboard paste in REPL |
| 470-004 | M6 M22 M38 M39 | Stop option descriptions at EndOfOptions |
| 470-005 | M7 | Use GetSymbolInfo for Map Implements AddBehavior type args |
| 470-006 | M8 M21 | Escape service constructor defaults in source-gen DI |
| 470-007 | M9 M10 M31 | Fail-closed DevCli NuGet version lookup |
| 470-008 | M11 M12 M30 | Fix search version flag and FTS NUL crash |
| 470-009 | M13 M40 | Harden MCP example fetch path traversal |
| 470-010 | M14 M15 M29 | Run generator-17 and check-version-04 in CI |
| 470-011 | M17 M32 | Restrict REPL history file mode |
| 470-012 | M16 M25–M28 M42 M43 | Sweep tests-infra leftovers from 470 review |
| 470-013 | M18 M19 M37 M44 | REPL completion remaining 470 findings |
| 470-014 | M23 M24 M41 | DevCli remaining 470 findings |
| 470-015 | M33 | Redact telemetry error.message on OTLP export |

Tiny nits were folded into the child that already touches the same files (not committed on this branch). No sibling “apply 470 findings” task.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M20 | suggestion | Source-gen DI cannot resolve `IEnumerable<T>` multi-impl constructor deps. Runtime DI works; `generator-27` is runtime-DI-only. Owned by in-progress epic 391. | review oracle 2026-09-04 |

## Escalations

- None. 454-019 stays on its existing task; do not re-file.
- MCP remains frozen for features; 470-009 is correctness/security only.
- 458 versioning *policy* was not re-litigated; 470-007/014 are code defects in DevCli/workflow services.

## Review artifacts

- `review/review-framework.md`
- `review/round-1/{core-runtime,repl-completion,analyzers-generators,parsing,aux,tests-infra,security}.md`
- `review/round-1/merged.md`
- this file
