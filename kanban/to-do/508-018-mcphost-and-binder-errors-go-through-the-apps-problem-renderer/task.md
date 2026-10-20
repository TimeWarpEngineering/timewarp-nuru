# McpHost and binder errors go through the app's problem renderer

## Description

Found by the independent review of Roslynk PR #9 (task 005) on TimeWarp.Nuru 4.0.0-beta.4.

McpHost's `tools/call` binding failure bypasses the app's problem renderer.
`source/timewarp-nuru-mcphost/nuru-mcp-tool-invoker.cs` (~line 72-74): when `App.TryBind` fails it returns
`ErrorResult(error.Message)`, plain text such as `Error: Missing required value 'symbolName' for 'get_symbol'.`

An app that registered `IResponseRenderer<SharedProblemDetails>` (508-011) renders every other failure through
it. Roslynk's renderer emits a header outline (`error=Invalid` plus `errorMessage=...`), and its MCP server
instructions tell models failures look like that. Before PR #9, Roslynk mapped binding errors to `error=Invalid`.
Now a model sees two error formats depending on where the failure happened.

## Requirements

- When a problem renderer is registered, a bind failure becomes a `SharedProblemDetails` (Type from
  `NuruBindError.Kind`, or a single documented type such as `Invalid`; Detail = the message; Status 400 or the
  documented usage code) and is rendered through it, on both the in-process and forwarded paths.
- Same for the CLI `--json-args` and argv binding errors when the app opts in (keep today's default when no
  problem renderer is registered).
- Exception escapes (`Error: {exception.Message}`) get the same treatment (Type `Faulted` or documented).
- Tests for both paths; docs on the McpHost page and `neutral-contracts.md`.

## Checklist

- [ ] Bind failure through the problem renderer (in-process and forwarded)
- [ ] CLI binding errors opt-in
- [ ] Escaped exceptions
- [ ] Tests and docs

## Session

- Created: 2532391 (2026-10-21) by the Roslynk cockpit from the PR #9 review.

## Notes

- Related: 508-003 (binder), 508-004 (McpHost), 508-011 (problem renderer), Roslynk 005.
