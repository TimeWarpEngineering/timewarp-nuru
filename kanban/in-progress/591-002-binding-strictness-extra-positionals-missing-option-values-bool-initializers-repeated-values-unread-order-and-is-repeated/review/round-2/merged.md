# Round 2 — merged findings
**Date:** 2026-11-19
**Sources:** general
**Scope:** fix delta `14104e13..HEAD`; carries round-1 IDs M1–M5.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 3 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- Plain flags no longer consume a following boolean token (`route-matcher-emitter.option-parsing.cs:41-54`).
  Tests: routing-43 optional and required positional after a flag.

### M2 — Severity: bug — Status: fixed
- `NURU_A005` for a non-identifier `[Parameter(Name)]`; property-name fallback in both segment and binding paths.
  Test: generator-76 `Should_report_nuru_a005_when_parameter_name_is_not_an_identifier`.

### M3 — Severity: bug — Status: fixed
- `endpoints.md` and `endpoint-generator.md` set `Order` and say it is required. Remaining instances elsewhere are M6.

### M4 — Severity: suggestion — Status: fixed
- Bare default-true `--create` rejection documented in the migration guide (concept map, difference 10) and the
  migrate skill; pinned by routing-43 `Should_reject_a_bare_default_true_bool`.

### M5 — Severity: nit — Status: fixed
- `ExtractSegmentFromProperty` deleted.

## Issues

### M6 — Severity: bug — Status: open
- File: documentation/user/guides/migrating-from-system-commandline.md:349
- Description: `GetCommand` (`Url` :349, catch-all `Items` :352) and `RunCommand` (`Script` :396, catch-all `Args`
  :399) declare two `[Parameter]` properties without `Order`. A catch-all counts toward `NURU_A002`
  (`endpoint-extractor.properties.cs:178-195`), so each copied example fails the build with two `NURU_A002` errors.
  Contradicts the guide's own concept map (:48) and restatement item 4.
- Suggestion: Add `Order = 0` / `Order = 1` to both examples.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer; none. M6 is the remainder of M3's class (multi-parameter doc examples without `Order`), kept
  separate because M3's cited files are fixed.
