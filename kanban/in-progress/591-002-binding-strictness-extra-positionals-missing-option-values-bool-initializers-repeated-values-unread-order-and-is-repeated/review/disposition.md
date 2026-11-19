# Disposition — task 591-002

**Date:** 2026-11-20
**Outcome:** reopened. Round 5 has open findings (M9 bug, M10 suggestion). The final disposition comes after
the next review round.
**Rounds:** 5
**Final open count:** 2 (pending fix pass)

## Summary

Rounds 1–3 closed clean on 2026-11-19 with M1–M6 fixed. Round 4 reviewed the restart merge and opened M7 (a
greedy repeated option swallowed a required positional) and M8 (duplicate routing-43). The fix pass closed both.
Round 5 found one shape the fix missed. M9: a string repeated option before `{required} {*catchall}` now binds
the last token to the required positional, leaves the catch-all empty, and exits 0 (the `fluent-docker-run`
sample). M10: the fix-pass record has no cache clear and no CI-suite run after the generator change. See
`review/round-5/merged.md`.

## Exception log (if accepted-exceptions)

None yet.

## Escalations

- None. If the operator keeps the current greedy rule for a catch-all, M9 becomes a documented contract change
  (see the round-5 suggestion).
