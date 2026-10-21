# Round 1 — merged findings
**Date:** 2026-10-19
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-mcphost/nuru-mcp-tool-invoker.cs:93
- Description: `InvokeAsync` sets `stdout` to `capturedOut + terminal.Output`. `CreateStructuredContent` (line 140) parses that whole string when `ReturnTypeKind` is `Json` (lines 146-159). Console and default-terminal writes land in `capturedOut`. `RenderAsync` writes the CLI JSON to a separate `TestTerminal`. A handler that returns an object and also writes a log line produces text that is not one JSON value. `JsonDocument.Parse` throws, the catch returns null, and `StructuredContent` is omitted while `IsError` stays false. The text block still holds the log plus the JSON, so the call looks successful and the JSON option is empty. Scalar wrapping has the same split. `WriteScalar` (lines 180-197) writes numbers from the CLR `result`, but strings and other non-numeric results write `text`, so `{"result": ...}` includes the log prefix. `documentation/user/features/mcp-hosting.md` says a json route's object is passed through and any other value is wrapped as `{"result": value}`. `Should_return_structured_content_when_json_is_requested` uses `mh-report` with no console write. `Should_capture_console_output_into_the_result` does not set `ContentFormat.Json`.
- Suggestion: Build structured content from the rendered terminal text, or from `result`, and keep captured console output in the text blocks only. For scalars, wrap the CLR value. Use the rendered text only when there is no CLR value.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
