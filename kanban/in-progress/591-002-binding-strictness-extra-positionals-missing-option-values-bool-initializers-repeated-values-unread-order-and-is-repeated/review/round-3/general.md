# Round 3 — general
**Date:** 2026-11-19
**Scope reviewed:** fix delta `6b29609c..HEAD` (`be4635e5` docs, `1180b084` kanban land). Carries M6 from round 2.

## Summary

The fix pass is documentation only. `GetCommand` and `RunCommand` in the System.CommandLine migration guide set
`Order = 0` on the positional and `Order = 1` on the catch-all, so a copied example no longer reports `NURU_A002`.
The same omission is fixed in the getting-started add command, the readme's second add command, and the endpoint
guide's required/optional pair. The catch-all keeps the highest order, so it stays last. No product code changed.

A scan of every `[Parameter]` in `*.md` outside `kanban/` found no other class with two or more parameters that
omits `Order`. The rest (endpoint guide, generator design doc, `tw-nuru` skill, subset-publishing, samples) declare
one parameter per class, which may omit `Order`. The scan was read-only `rg`; no gate was run.

The recorded gates cover the change: `./bin/dev build` (0 warnings, 0 errors) and `ganda repo audit` (45 passed,
1 skipped) for implement-fix-3, with no test run because no test or production type changed. The restatement
item 5 in `## Results` matches the diff.

## Issues

### M6 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:349
- Description: verified. `GetCommand` (:349, :352) and `RunCommand` (:396, :399) set `Order`.
- Status: fixed

No new findings.
