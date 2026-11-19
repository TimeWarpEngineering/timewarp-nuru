# Round 2 — general
**Date:** 2026-11-19
**Scope reviewed:** `a90c3da2..038fa1e3` (commit 038fa1e3): `operation-endpoint-extractor.constructors.cs`
`IsScalar`, `routing-44-endpoint-file-directory-ip.cs`, and the updated `task.md` Goal / Results.

## Summary

The fix pass resolves round-1 M1 as suggested. `IsScalar` no longer matches three nullable display names
by hand; for any reference type with `NullableAnnotation.Annotated` it compares the display of
`WithNullableAnnotation(NotAnnotated)`, so `Uri?` and `Version?` join `FileInfo?`, `DirectoryInfo?`, and
`IPAddress?` as scalars. `routing-44` adds `t591007-optional` with nullable `FileInfo?`, `DirectoryInfo?`,
`IPAddress?`, and `Uri?` properties, covering both bound and omitted cases. Risk is low.

## Issues

None.

## Checked and clean

- Prior M1: fixed. The per-type `display[..^1]` special case is gone; the reference-type path is general.
- Nullable value types still go through the `Nullable<T>` recursion at the top of `IsScalar`, before the
  reference-type branch; `string?` is caught earlier by `SpecialType.System_String`. Oblivious
  (`NullableAnnotation.None`) types already display without `?`, so they are unaffected.
- `IsJsonMember` element path (`List<Uri?>`, `FileInfo?[]`): the element now classifies as scalar, and
  `RepeatedElementConstraint` passes the `FullyQualifiedFormat` name to `GetTypeConstraintFromClrType`,
  whose existing `?` branch (checked in round 1) maps it to the same constraint.
- Behavior change for `Uri?` / `Version?` (JSON member → argv scalar): no other `[Operation]` in the repo
  declares `Uri?` or `Version?` (`rg 'public (Uri|Version|System\.Uri|System\.Version)\? '` outside
  `kanban/` hits only a test fake in `check-version-05` and the new routing-44 property). The prior JSON
  classification of the nullable form was the inconsistency M1 named, not a contract.
- Test: `T591007OptionalQuery` uses `.Map<T>()`, the `t591007-` route prefix, and the PascalCase option
  names the existing `t591007-place` case already uses. `Uri.ToString()` of `https://example.com/a` is
  `https://example.com/a`, matching the asserted output. The omitted case asserts all four stay `none`.
- `## Goal (implementer restatement)` was extended with the M1 acceptance; it still matches the original
  task (bind the three types; the nullable widening is review-driven scope). No misread.
- Recorded gates in `## Results` cover the fix pass: `./bin/dev build` 0 warnings, cache clear, routing-44
  9 passed (7 + 2 new), `ganda repo audit` exit 0 with one advisory. Full suite is CI. `Version?` has no
  dedicated case; the Results say so, and it shares the `Uri?` code path.

No targeted command was run.
