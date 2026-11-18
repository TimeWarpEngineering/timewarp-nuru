# Disposition — task 591-001

**Date:** 2026-11-19
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 found one bug (M1: a declared `--name:value` still reached `AddCommandLine`) and one suggestion (M2: the migration guide misstated slash forms and short-option `=` binding). The fix pass limits `AddCommandLine` to tokens the route filter drops, covers the colon form with configuration in routing-42, and corrects the guide and skill. Round 2 re-checked that delta and raised nothing new. Both findings are fixed.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.
