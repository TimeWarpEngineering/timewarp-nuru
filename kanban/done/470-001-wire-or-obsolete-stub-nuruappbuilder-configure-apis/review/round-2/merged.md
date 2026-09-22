# Round 2 — merged findings
**Date:** 2026-09-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs
- Description: `IsPerCommandHelpRoute` used substring `Contains("--help")`, so `--helper` / `--help-all` were hidden from default CLI `--help`.
- Suggestion: Classify only via `OptionDefinition.LongForm is "help"` and `!ExpectsValue`. Add `--helper` regression.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. Substring `Contains` is gone. `--helper` lists; synthetic per-command `--help` rows stay hidden by default; `blog --help?` still classifies as help via LongForm.

## Duplicates / conflicts

- None. Round 2 raised no new findings. Prior M1 carried with updated status.
