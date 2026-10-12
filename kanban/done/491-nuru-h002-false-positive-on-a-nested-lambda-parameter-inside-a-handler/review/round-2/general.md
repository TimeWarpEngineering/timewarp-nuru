# Round 2 — general
**Date:** 2026-10-12
**Scope reviewed:** post-fix `tests/ci-tests/run-ci-tests.cs` against round-1 M1. Re-checked `CiTestExcludes` for generator-54 and that the new array entry matches the excluded path.

## Summary

M1 is fixed. The second-phase comment says `generator-28..54`, and `standaloneTests` appends `generator-54-h002-nested-lambda-parameter.cs` in the same `Path.Combine` shape as generator-53. The exclude in `Directory.Build.props` still names that file. No new defect on the fix delta.

## Resolved prior

### M1 — Status: fixed
`run-ci-tests.cs` lists generator-54 after generator-53. The comment range is `generator-28..54`. `CiTestExcludes` still points at `../timewarp-nuru-tests/generator/generator-54-h002-nested-lambda-parameter.cs`.

## Issues

<!-- No new issues. -->
