# Disposition — task 454-019

**Date:** 2026-09-14
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (effort 1, general) found one bug (M1: wrap fields not re-anchored after completion dumps, so Alt+= / first multi-candidate Tab left later cursor motion on the pre-dump row) and one suggestion (M2: reset wrap state on search exit). M1 was fixed on this task id: candidate display is dump-only; the reader adopt-anchors from the physical cursor then `RedrawLine`. Round 2 re-verified M1 as fixed, M2 as wontfix, and raised no new issues. `repl-43` is 15/15.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | suggestion | Search-line redraw is an explicit task deferral. For a non-wrapping search UI, live currentTop plus pre-search LastCursorVisualIndex reconstructs InputStartRow and correctly clears original wrap rows. Adopting at the search-prompt row would leave remnants on earlier wrap rows. When search redraw is wrap-fixed later, it should maintain the same wrap fields as single-line redraw. | review orchestrator |

## Escalations

None.
