# Round 3 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `46138c91..b71d5d68`. Product path is `documentation/user/features/neutral-contracts.md` only. Re-checked M3 on that sentence. Compared the current `## Goal (implementer restatement)` with the original Requirements.

## Summary

The fix pass adds the list comma after the `--json-args` parenthesis. The opt-in sentence is now three cases joined by a comma and a final "and". M1 and M2 stay fixed; this delta does not touch their code. The restatement still matches the original task: one documented `Invalid` type, opt-in for CLI `--json-args` and argv binding errors (including a JSON member and `MissingValue`), and `Faulted` for an escaped exception. No new findings.

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1690
- Description: Unchanged since round 2. This delta does not touch the emitter.
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru/nuru-app.cs:306
- Description: Unchanged since round 2. This delta does not touch `WriteArgumentError`.
- Status: fixed

### M3 — Severity: nit — Status: fixed
- File: documentation/user/features/neutral-contracts.md:152
- Description: The sentence lists three opted-in cases: a `--json-args` error (a bind failure, or a value that is not a JSON object), an argv value that does not convert, and a JSON member passed on argv or left out. The comma after the parenthesis joins the first case to the second. "and" still introduces the third. The `MissingValue` sentence is unchanged.
- Status: fixed

## Issues

None.
