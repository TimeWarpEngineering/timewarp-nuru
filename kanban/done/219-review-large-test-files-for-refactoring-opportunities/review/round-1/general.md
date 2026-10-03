# Round 1 — general
**Date:** 2026-10-03
**Scope reviewed:** git diff master...HEAD

## Summary
The split is clean. A multiset line comparison of the old routing-05-option-matching.cs against the three new files shows only the class-name and Register lines differ (11+7+13=31 tests, each class name unique in the shared namespace). The same comparison for generator-26 (fixtures + runfile vs original) shows no lost lines. The new generator/Directory.Build.props imports the parent via GetPathOfFileAbove, and the fixtures file is compiled once in CI multi-mode through the existing tests/**/*.cs glob. The only remaining check is empirical: the MSBuildProjectName condition value.

## Issues
### Issue 1 — Severity: nit
- File: tests/timewarp-nuru-tests/generator/Directory.Build.props:10
- Description: The condition relies on `MSBuildProjectName` being `generator-26-constructor-dependency-resolution.cs` (with .cs suffix) for file-based apps. This could not be verified statically; if the value has no suffix the fixtures are silently not compiled and the standalone runfile fails with unresolved types (CI multi-mode is unaffected).
- Suggestion: Confirm via the standalone run of generator-26 (already being run). Optionally make the condition tolerant, e.g. `'$(MSBuildProjectName)' == '...resolution' Or '$(MSBuildProjectName)' == '...resolution.cs'`.
- Status: open
