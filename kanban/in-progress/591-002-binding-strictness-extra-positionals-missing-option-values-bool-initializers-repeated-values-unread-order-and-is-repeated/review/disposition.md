# Disposition — task 591-002

**Date:** 2026-11-20
**Outcome:** reopened. Round 4 has open findings (M7 bug, M8 nit). A final disposition follows the next
review round.
**Rounds:** 4
**Final open count:** 2 (pending fix pass)

## Summary

Rounds 1–3 closed with a clean disposition on 2026-11-19: M1–M6 fixed. The restart then merged the 591 feature
branch, and round 4 reviewed that merge. The merge resolution is correct. It also changed the input of the
existing routing-09 test, which exposed M7: a greedy repeated option swallows a *required* positional, so
`{args}* {path}` routes that the design docs and a sample show no longer match. M8 is the duplicate routing-43
file number. See `review/round-4/merged.md`.

## Exception log (if accepted-exceptions)

None yet.

## Escalations

- None. If the operator would rather keep pure greedy consumption, M7 becomes a documented contract change
  (see the round-4 suggestion).
