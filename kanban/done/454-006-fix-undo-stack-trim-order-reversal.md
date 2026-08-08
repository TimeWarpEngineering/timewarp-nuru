# Fix Undo Stack Trim Order Reversal

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru/repl/input/undo-stack.cs` — `TrimToCapacity()` copied the stack
to a list (newest-first per Stack enumeration), reversed to oldest-first, removed the
oldest entries, then reversed AGAIN before re-pushing. Pushing newest-first puts the
newest at the BOTTOM: after exceeding MaxCapacity (default 100) edits on one input
line, Ctrl+Z walked history from the OLDEST state forward — undo order inverted.

## Checklist

- [x] Remove the redundant second Reverse() in TrimToCapacity (one-line fix, plus a
      comment stating the ordering invariant)
- [x] Add Purpose/Design context regions to undo-stack.cs (records the trim-order
      invariant and the PSReadLine grouping design)
- [x] Unit test: push past MaxCapacity, verify pop order is newest-first and oldest
      entries were the ones trimmed
- [x] Run REPL test suite

## Results

- Test added to `tests/timewarp-nuru-tests/repl/repl-27-undo-redo.cs`
  (`UndoStack_should_preserve_order_when_trimmed_past_capacity`, capacity 3, 5 saves,
  asserts undo yields state-5 → state-4 → state-3 and that 1/2 were trimmed).
- Bug-first verification: with the fix stashed, exactly this test fails (14/15);
  with the fix, 15/15.
- Full CI: 1281 multi-mode tests, 1274 passed, 0 failed, 7 pre-existing skips + both
  standalone Roslyn phases 4/4, exit 0.

## Session

- Created: 2026-07-06 (full-repo review session)
- Implementation: 2026-07-06 (same session)
