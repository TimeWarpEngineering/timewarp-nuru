# CsCheck pilot: fuzz route parsing with random argv

## Description

Add a CsCheck property test that feeds random argv into Nuru route parsing/dispatch and asserts it never throws an unhandled exception and always yields a meaningful exit code (success, or a defined usage/error code with a message).

Sibling pilot: Amuru task 128 (quote-then-parse round-trip), https://github.com/TimeWarpEngineering/timewarp-amuru/blob/master/kanban/to-do/128-cscheck-pilot-quote-then-parse-round-trip-property-for-argument-escaping/task.md

## Context

- Busara (Wisdom) verdict 2026-10-13: GREEN for CsCheck as a **test-only** dependency, pilots first. We have no property-based testing today.
- Akili (Intelligence) evaluation: `/workspace/ideas/2026-10-13-cscheck-eval.md` (box). Verified CsCheck works in a Jaribu runfile.
- CsCheck: https://github.com/AnthonyLloyd/CsCheck (Apache-2.0, forkable if needed).

## Rules

- Pin **CsCheck 4.9.1** (no need to wait for 5.0; tests do not need AOT).
- Test-only. CsCheck must never be referenced by product/package code.
- Pin `CsCheck_Seed` in CI; print the seed on every failure so it can be replayed.
- Do not use `Faster` in build-blocking tests (CI timing is flaky).

## Kill criterion

If both pilots (this one and the sibling) find nothing and the generators are costly to maintain, stop at these two pilots.

## Deferred (until pilots show value)

- State reducer model-based tests
- Parse/format round-trip
- Terminal / Flexbox invariants

## Checklist

- [ ] Add CsCheck 4.9.1 to the test project/runfile only
- [ ] Generators for random argv (options, flags, values, unknown tokens, empty, unicode)
- [ ] Property: no unhandled exceptions; exit code is in the defined set
- [ ] Pin CsCheck_Seed in CI; print seed on failure
- [ ] Record findings for the kill-criterion review

## Notes

Not launched. Filed 2026-10-13 at Steven's request ("sure lets try").
