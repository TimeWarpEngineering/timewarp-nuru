# Disposition — task 219

**Date:** 2026-10-03
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general-reviewer round (effort 3 by-diff budget) found no bugs or suggestions. The 31 routing-05 tests and the generator-26 content survive the split. Only class names and `Register` lines changed. The new `generator/Directory.Build.props` imports its parent. The internals-visible-to refresh looks right. One nit about the `MSBuildProjectName` condition was closed wontfix because the standalone generator-26 run (10/10 passed after a cache clear) shows the condition matches.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | The condition was verified by a standalone run that passed 10/10, so a dual-name condition would be speculative | review orchestrator |

## Escalations

- None
