# Disposition — task 547

**Date:** 2026-10-26
**Outcome:** clean
**Rounds:** 5
**Final open count:** 0

## Summary

Round 5 (general, effort 1) re-reviewed `80af7f31..930090e9`. M6 is fixed: a `null` or `default` write wrapped in parentheses, `!`, or a cast counts as null, so a closure `RunAsync` after that write and one `Build()` is intercepted. generator-72 covers those spellings and expects no `NURU_I001`. No new findings. M1–M5 stay fixed. The implementer restatement matches the task checklist.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.
