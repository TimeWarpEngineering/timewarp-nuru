# Round 2 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-mcphost/nuru-mcp-tool-invoker.cs:133
- Description: Round 1: `CreateStructuredContent` parsed `capturedOut + terminal.Output`, so a handler that logged and returned a value dropped or corrupted `structuredContent`. The fix passes `TrimTrailingNewLine(rendered)`, where `rendered` is only the `RenderAsync` terminal text (line 95). Text content still concatenates captured console output with that rendered body (line 96). `WriteScalar` writes a `string` from the CLR value (line 202).
- Suggestion: Build structured content from the rendered terminal text, or from `result`, and keep captured console output in the text blocks only. For scalars, wrap the CLR value. Use the rendered text only when there is no CLR value.
- Source: general
- Disposition notes: Fixed in `a27fb9dc`. `Should_keep_console_output_out_of_structured_content` passed with the other tests in `mcphost-01-tools.cs` (14 passed, 0 failed). `mcp-hosting.md` now says console output stays in the text content. Char, Guid, and date scalars still use the rendered text; that text excludes captured console output, so the log-prefix failure is closed for them too.

## Resolved prior

- M1 fixed on the fix delta. No new findings.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
