# Round 1 — merged findings
**Date:** 2026-11-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: kanban/in-progress/591-006-generated-code-breaks-on-some-member-and-type-names/task.md (Checklist)
- Description: "CI tests green" is checked, but no full CI-tests run is recorded; Results defer the full
  suite to the pull request.
- Suggestion: Record a CI-tests run or point the checklist line at the PR's CI.
- Source: general
- Disposition notes: wontfix (review oracle). A plain `ganda task work` waits for the PR's CI and refuses to
  merge on red or pending, so the full suite gates the merge regardless. The deferral is stated plainly in
  Results; it is a record gap, not a silent skip. No fix pass for a checklist wording nit.

### M2 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs:328;
  route-matcher-emitter.option-conversions.cs:47,100,251
- Description: `Bare` on the `IOptions<T>` delegate locals, the custom-converter option path, and the
  optional `Uri` option path has no keyword-named test case.
- Suggestion: Optionally extend generator-78.
- Source: general
- Disposition notes: wontfix (review oracle). These are the same one-line `Bare` call as the tested enum and
  `int?` paths, outside the two reported items, and read correct. Not worth a fix pass on this task.

## Duplicates / conflicts

- None (single reviewer).
