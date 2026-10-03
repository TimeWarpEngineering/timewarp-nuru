# Round 2 — general
**Date:** 2026-10-03
**Scope reviewed:** Send-back delta `223cfa14..56c86972` (generator-26 revert, internals-visible-to regeneration, task.md), checked against the full branch diff `origin/master...HEAD`.

## Summary

The send-back on PR #280 asked for the generator-26 fixtures extraction to be reverted. That is done. `generator/Directory.Build.props` and `generator-26-constructor-dependency-fixtures.cs` are gone. `generator-26-constructor-dependency-resolution.cs` is byte-identical to `origin/master`, with subjects inline above the existing `// JARIBU TESTS` banner. The three `internals-visible-to.g.cs` files no longer list the fixtures stem or the `generator` entry. The routing-05 stems and the pre-existing missing stems are still listed. Rerunning `runfiles/generate-internals-visible-to.cs` leaves the tree unchanged. Round 1 already covered the routing-05 split, and this delta does not touch it.

Verification after `ganda runfile cache --clear`:

- generator-26: 10/10
- routing-05-option-modifier-matrix: 11/11
- routing-05-boolean-mixed-typed-options: 7/7
- routing-05-option-aliases: 13/13
- `dotnet build tests/ci-tests/run-ci-tests.cs`: exit 0

## Issues

None.
