# Fix Undo Stack Trim Order Reversal

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru/repl/input/undo-stack.cs:170-189` — `TrimToCapacity()` copies the
stack to a list, reverses it, removes the oldest entries, then reverses AGAIN (line ~184)
before re-pushing. Traced with stack top→bottom `[A(newest),B,C,D(oldest)]`, max=3: the
re-pushed result is `[C,B,A]` instead of the correct `[A,B,C]`.

Impact: after more than MaxCapacity (default 100) edits on one input line, Ctrl+Z walks
from the OLDEST state forward — undo order is inverted/corrupt.

Fix is a one-liner: delete the second `items.Reverse()`.

## Checklist

- [ ] Remove the redundant second Reverse() in TrimToCapacity
- [ ] Unit test: push MaxCapacity+N states, verify pop order is newest-first
- [ ] Run REPL test suite

## Notes

Claude cannot run interactive REPL sessions; unit-test via the UndoStack class directly.
