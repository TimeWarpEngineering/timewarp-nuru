# Round 1 — general
**Date:** 2026-09-22
**Scope reviewed:** branch `task/470-004-stop-option-descriptions-at-endofoptions` vs `origin/master` — option-description stop at `EndOfOptions`, parameter-name validation, `BuiltInTypeNames` / `NURU_P004`, adjacent-parameter span, and the new parser tests.

## Summary

Option descriptions now stop on a standalone `--`, so that token stays an end-of-options segment and is no longer skipped into the description. Parameter names go through `IsValidIdentifierFormat` while option names may still contain hyphens. Built-in type spellings live in one ordinal list used by the parser, `InvalidTypeConstraintError`, and `NURU_P004`. `AdjacentParametersError` covers the second parameter segment, falling back to the `{` token when that parse throws. Re-ran parser-09, parser-11, parser-13, parser-15, parser-18, and parser-19 (48 passed, 0 failed) and probed alias, repeated-option, typed-adjacent, and hyphenated catch-all patterns. No defect in this diff.

## Issues

None.
