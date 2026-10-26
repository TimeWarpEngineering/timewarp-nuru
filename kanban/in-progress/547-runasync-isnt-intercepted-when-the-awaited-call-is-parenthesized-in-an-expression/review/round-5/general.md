# Round 5 — general
**Date:** 2026-10-26
**Scope reviewed:** fix delta `80af7f31..930090e9` (`fix(generator): count a wrapped null write as null in HasUnrecordedWrite`). Prior open finding M6, plus new defects on that delta only. The whole branch was not re-reviewed.

## Summary

M6 is fixed. `IsNullLiteral` now treats `null` and `default` as null when they are wrapped in parentheses, the null-forgiving operator, or a cast (`null!`, `(null)`, `default!`, `(NuruApp?)null`, `(NuruApp?)(default)`, `default(NuruApp)`). A cast of any other expression still counts as an unrecorded write.

`RecordAssignment` still drops a null while the variable has no recorded app. `HasUnrecordedWrite` skips these wrapped forms, so `NuruApp app = null!;` followed by one `Build()` leaves that `Build()` as the only recorded app. `AppAt` then binds a closure `RunAsync` to it. `ReportRunAsyncNotIntercepted` skips a call whose site is already on an app, so generator-72's empty `NURU_I001` check is the intercept check. The new test covers the six spellings above, including a second wrapped null before `Build()`, and expects no compile error and no `NURU_I001`. A wrapped null after a recorded app is still stored as a null app, so the every-assignment rule still leaves that closure for `NURU_I001`.

The implementer restatement matches the task checklist. This delta does not edit the restatement.

## Issues

None.

## Resolved prior

- M6 (bug): fixed by `930090e9`. `IsNullLiteral` at `dsl-interpreter.cs:418`.
