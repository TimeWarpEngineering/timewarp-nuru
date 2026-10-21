# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** branch `task/508-018-mcphost-and-binder-errors-go-through-the-apps-prob` vs `master` (product sources, docs, and the new tests). Compared with `task.md` Requirements and `## Goal (implementer restatement)`.

## Summary

Bind failures and escaped handler exceptions on McpHost `tools/call`, and the same two failures on the daemon invoke path, go through `NuruProblems` and `NuruApp.TryRenderProblem`. The CLI opt-in covers `--json-args` errors and the argv conversion sites that now call `WriteArgumentError`. The mapping (Type `Invalid` / `Faulted`, status, detail, kind and key) matches the restatement, and the in-process versus forwarded text comparison is implemented and tested for those two failures. Two argv binding errors for JSON members still write plain `Error:` text after the opt-in, and argv "Missing value" errors are labeled kind `InvalidValue`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1690
- Description: `EmitJsonMemberAfterMatch` still writes argv binding errors with `Terminal.WriteErrorLine` and returns 1. The two messages are `Error: --{name} is JSON. Pass it with --json-args.` (line 1690) and `Error: pass --{name} via --json-args.` (line 1696). `UseProblemRendererForErrors()` does not affect them. The original requirement says CLI `--json-args` and argv binding errors go through the problem renderer when the app opts in. The binder already maps the first of those messages to a `NuruBindError` (`json-args-emitter.cs` around the `argvJson` fail, kind `InvalidValue`) and the CLI surfaces that failure through `WriteBindErrorAsync`. The pure argv match path never reaches that helper. An opted-in app therefore still shows the old `Error:` line for a JSON member passed on argv, and for a required JSON member that was omitted.
- Suggestion: Route both writes through `WriteArgumentError` or `WriteBindErrorAsync` (kind `InvalidValue` for the rejected flag, kind `MissingValue` for the omitted required member) so the opt-in renders them as `Invalid` on stderr and the no-opt-in text stays the same. Add a routing test that opts in, passes a JSON option on argv, and expects `error=Invalid` on stderr.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-nuru/nuru-app.cs:297
- Description: `WriteArgumentError` always passes `NuruBindErrorKind.InvalidValue`. The FileInfo and DirectoryInfo "Missing value for option" sites (`route-matcher-emitter.cs` lines 1444 and 1485) are missing values, and `NuruBindErrorKind` already has `MissingValue`. With the opt-in, a renderer that prints the `kind` extension reports `InvalidValue` for a missing option value. Type, status, and detail stay correct.
- Suggestion: Let `WriteArgumentError` take the kind, and pass `MissingValue` from those two sites.
- Status: open
