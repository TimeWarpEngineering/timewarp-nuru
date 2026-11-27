# Disposition — task 602

**Date:** 2026-11-27
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 3) raised M1 (bug: group help swallowed `<group> --help` on a grouped raw route), M2 (suggestion: endpoint and alias runtime coverage), and M3 (nit: configuration-override docs). Fix commit 1fa4a72e closed all three, and round 2 (general, effort 2) re-verified them against the diff and the recorded Results (routing-43 at 18 passed, `./bin/dev build` and `ganda repo audit` passing). Round 2 raised one nit, M4: the new public fluent `.WithAlias` is missing from the changelog. It was accepted as wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit | Additive stub on a beta package (4.0.0-beta.18), already handled by the generator, covered by routing-43 and recorded in task.md Results. The changelog line goes with the next fluent-alias change. | review orchestrator (round 2) |

## Escalations

- None.
