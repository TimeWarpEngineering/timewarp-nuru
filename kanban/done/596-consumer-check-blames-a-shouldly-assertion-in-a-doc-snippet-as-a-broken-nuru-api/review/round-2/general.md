# Round 2 — general
**Date:** 2026-11-17
**Scope reviewed:** delta `2b5b5e208841b9adef65fae8aa1779737b44c6d2..HEAD` (`ce03e1f5`). Product change is `skills/tw-nuru-verify/SKILL.md` only (the docs-step `broken` bullet). Re-checked M1 against `nuru_errors` in `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh`. Compared with `## Goal (implementer restatement)` and the original task. No misread.

## Summary

The fix updates the `broken` bullet so it states the same rule as the script. Host extension points still cover `CS1061` and `CS0117`. A `CS1061` whose receiver is one of the sixteen keyword types, optionally with `?` or `[]`, is not blamed. `CS0117` on those types stays blamed. `object`, `dynamic`, and a missing member on a package type stay blamed. The keyword list matches the `grep -vE` at `nuru-consumer-check.sh:325`. The recorded round-2 fix pass covers build, test, and audit for this markdown edit. No new issues.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: skills/tw-nuru-verify/SKILL.md:105
- Description: Round 1 asked the skill's `broken` bullet to match `nuru_errors`. The bullet now does. Built-in keyword receivers drop `CS1061` only, with the same type list and optional `?`/`[]`. `object`, `dynamic`, and package types (`TestTerminal`, `ITerminal`, `NuruAppBuilder`) stay blamed.
- Status: fixed

## Issues

None.
