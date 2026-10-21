# Round 2 — general
**Date:** 2026-10-21
**Scope reviewed:** fix delta `63545b11..c9dbc5c3` (product paths only). Re-checked M1 and M2 on that delta. Compared the current `## Goal (implementer restatement)` with the original Requirements.

## Summary

The fix pass sends both JSON-member argv errors through `NuruApp.WriteBindErrorAsync`: `InvalidValue` when the member is passed on argv, `MissingValue` when a required member is left out. The FileInfo and DirectoryInfo missing-option sites pass `MissingValue` into the new `WriteArgumentError` overload. `ExecuteRouteAsync` is already async, so the new `await` is valid, and the key is the option long form, the same key the `--json-args` binder uses for that member. I ran `routing-36-problem-errors.cs`: 8 passed, 0 failed, including the two new cases. The current restatement matches the original task. The opt-in sentence in `neutral-contracts.md` lost the word "and".

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1690
- Description: `EmitJsonMemberAfterMatch` now writes both errors through `WriteBindErrorAsync`. Without the opt-in, `WriteBindErrorAsync` still writes the same `Error:` line to stderr. `Should_render_argv_errors_for_a_json_member_when_opted_in` covers the opted-in argv flag and the omitted required member.
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru/nuru-app.cs:306
- Description: `WriteArgumentError(message, kind, key)` keeps the given kind. The two-argument form still means `InvalidValue`. The FileInfo and DirectoryInfo missing-option sites (`route-matcher-emitter.cs` lines 1444 and 1485) pass `MissingValue`. `Should_render_an_argument_error_with_its_kind` checks the helper.
- Status: fixed

## Issues

### Issue 1 — Severity: nit
- File: documentation/user/features/neutral-contracts.md:152
- Description: The edited opt-in sentence dropped "and" after the `--json-args` parenthesis. It now reads "a `--json-args` error (…) an argv value that does not convert". The three cases and the `MissingValue` sentence are still stated.
- Suggestion: Restore the conjunction: `(a bind failure, or a value that is not a JSON object) and an argv value that does not convert`.
- Status: open
