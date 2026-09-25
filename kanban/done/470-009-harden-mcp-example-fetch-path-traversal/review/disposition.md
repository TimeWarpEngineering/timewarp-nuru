# Disposition — task 470-009

**Date:** 2026-09-23
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 raised one bug (M1: percent-encoded `..` leaving the allowlist while staying under the org/repo AbsolutePath check). Fixed on this task id with fail-closed `%` rejection, post-resolve allowlist AbsolutePath assertion, and an mcp-08 regression. Round 2 re-verified M1 fixed with no new findings. Disposition is clean.

## Exception log (if accepted-exceptions)

_None._

## Escalations

- None.
