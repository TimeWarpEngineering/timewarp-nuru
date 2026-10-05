# Round 2 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: changelog.md:88
- Description: beta.70’s contents are the tree tagged `v3.0.0-beta.69`, not an unknown package whose changes belong under beta.71.
- Suggestion: Move those bullets onto beta.70 and say beta.69 was not published.
- Source: general
- Disposition notes: Fixed in round 2. Bullets sit on beta.70. beta.69 is a one-line pointer. beta.71 is unchanged.

### M2 — Severity: bug — Status: fixed
- File: changelog.md:338
- Description: Grouping beta.26 with beta.27 attributed MCP changes to a tag that is the same commit as beta.25.
- Suggestion: Separate headings.
- Source: general
- Disposition notes: Fixed in round 2. beta.27 has the MCP bullets. beta.26 says same commit as beta.25, not published.

### M3 — Severity: bug — Status: fixed
- File: changelog.md:384
- Description: `--capabilities` was introduced in beta.22 and listed as added in beta.24.
- Suggestion: Move the flag to beta.22. Describe the beta.24 source-generator emission separately.
- Source: general
- Disposition notes: Fixed in round 2.

### M4 — Severity: bug — Status: fixed
- File: changelog.md:390
- Description: beta.22 dropped public `MapMultiple` without a breaking bullet.
- Suggestion: Add the breaking bullet.
- Source: general
- Disposition notes: Fixed in round 2.

### M5 — Severity: bug — Status: fixed
- File: changelog.md:80
- Description: beta.72 omitted multi-character single-dash options and help-text escaping.
- Suggestion: Add both under Fixed.
- Source: general
- Disposition notes: Fixed in round 2.

### M6 — Severity: suggestion — Status: fixed
- File: changelog.md:168
- Description: Kanban task ids were written as `#NNN` GitHub references (404 on this repo), including `#442`.
- Suggestion: Cite `task NNN`. Cite PR `#191` for the Unit fix.
- Source: general
- Disposition notes: Fixed in round 2 for 349, 351, 356, 357, 360, 372, 381, 382, 385, 387, 389, 396, 403, and 442.

### M7 — Severity: suggestion — Status: fixed
- File: changelog.md:116
- Description: beta.66 release notes’ workflow-command fix was omitted.
- Suggestion: Add the bullet.
- Source: orchestrator
- Disposition notes: Fixed in round 2.

## Duplicates / conflicts

- Round 1 ids kept. No new findings.
