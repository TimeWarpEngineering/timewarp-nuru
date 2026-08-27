# Disposition — task 395

**Date:** 2026-08-27
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Effort 1, roster `general`. Round 1 raised M1 (bug: command-handler `global::` fallback could `new` an internal impl after lowering) and M2 (suggestion: `lib/` TFM picked by path sort). Both were selected, fixed in `b034c788`, and re-verified in round 2. No new findings on the fix delta. No wontfix.

## Exception log

None.

## Escalations

None.

## Paths

- `review/review-framework.md`
- `review/round-1/general.md`, `review/round-1/merged.md`
- `review/round-2/general.md`, `review/round-2/merged.md`
