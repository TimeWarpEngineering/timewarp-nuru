# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 2 |

## Resolved prior

- Round 1 raised no findings. Nothing to carry.

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tests/ci-tests/run-ci-tests.cs:32
- Description: The standalone-list comment says `generator-28..63`; the list now ends at generator-64.
- Suggestion: Change the range to `generator-28..64`.
- Source: general
- Disposition notes: wontfix on this branch (review orchestrator, 2026-10-23). A comment-only nit. Fixing it here would cost a full implementer pass and a round 3 on a PR whose purpose is to cut CI cost. The follow-up task that converts the 49 call sites edits these same CI files and can correct the range.

### M2 — Severity: nit — Status: wontfix
- File: tests/timewarp-nuru-tests/generator/generator-64-discover-endpoints-across-apps.cs:14
- Description: The namespace `Gen63DiscoverEndpointsAcrossApps` and `assemblyName: "Gen63DiscoverEndpoints"` (line 93) keep the pre-renumber number. They do not collide with 521's names, and the file is excluded from the CI assembly.
- Suggestion: Rename to `Gen64…`.
- Source: general
- Disposition notes: wontfix on this branch (review orchestrator, 2026-10-23). The identifiers are internal to a standalone Roslyn-hosted test and do not change behavior. Same rationale as M1: hand it to the follow-up conversion task.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
