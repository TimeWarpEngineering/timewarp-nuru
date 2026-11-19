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
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:350
- Description: option-display, parameter-row, and example formatting is duplicated from
  `route-help-emitter.cs:215-305`; the root and per-route help pages can drift.
- Suggestion: share the formatting through `HelpPatternHelper`.
- Source: general
- Disposition notes: wontfix (review orchestrator). Both copies produce the same output today, and the
  two emitters write to different receivers (`terminal` vs `app.Terminal` with indentation), so the
  extraction is a refactor beyond this bug fix. Nit only; no behavior risk now.

### M2 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:274
- Description: several promoted root default routes share one `Parameters:` table and one `Options:`
  table with no per-route attribution; untested.
- Suggestion: group per route, or accept.
- Source: general
- Disposition notes: wontfix (review orchestrator). The target case is a single-command tool; each
  promoted route still gets its own usage line, which shows which parameters and options go together.
  Sized to what the org's tools hit (global prefs: "We are the primary consumer").

## Duplicates / conflicts

- None (single reviewer).
