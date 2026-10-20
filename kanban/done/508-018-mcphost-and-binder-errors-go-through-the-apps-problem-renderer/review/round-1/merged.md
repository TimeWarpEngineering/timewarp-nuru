# Round 1 — merged findings
**Date:** 2026-10-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1690
- Description: `EmitJsonMemberAfterMatch` still writes two argv binding errors with `Terminal.WriteErrorLine`: `Error: --{name} is JSON. Pass it with --json-args.` (line 1690) and `Error: pass --{name} via --json-args.` (line 1696). `UseProblemRendererForErrors()` does not change them. The original task requires argv binding errors to go through the problem renderer when the app opts in. The `--json-args` binder already turns the first message into a `NuruBindError` and the CLI prints that through `WriteBindErrorAsync`. The argv match path returns before that helper.
- Suggestion: Send both writes through `WriteArgumentError` or `WriteBindErrorAsync` (kind `InvalidValue` for the rejected flag, kind `MissingValue` for the omitted required member). Without the opt-in, keep today's plain text. Cover the opted-in argv case in `routing-36-problem-errors.cs`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/timewarp-nuru/nuru-app.cs:297
- Description: `WriteArgumentError` always uses `NuruBindErrorKind.InvalidValue`. The FileInfo and DirectoryInfo missing-option sites (`route-matcher-emitter.cs` lines 1444 and 1485) are missing values. An opted-in renderer that prints `kind` shows `InvalidValue` for those messages. Type `Invalid`, status 400, and the detail text are correct.
- Suggestion: Pass the kind into `WriteArgumentError`, and use `MissingValue` at those two sites.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- One reviewer. No overlaps.
