# Round 2 — general
**Date:** 2026-10-19
**Scope reviewed:** fix delta since round 1, commit `a27fb9dc` (parent `578a009f`). `source/timewarp-nuru-mcphost/nuru-mcp-tool-invoker.cs`, `tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs`, `documentation/user/features/mcp-hosting.md`.

## Summary

Round 1 left M1 open: `NuruMcpContentFormat.Json` built `structuredContent` from captured console text plus the rendered body. The fix keeps that concatenation for the text block only. `CreateStructuredContent` now receives `TrimTrailingNewLine(rendered)`, and `rendered` is `RenderAsync`'s terminal text. `WriteScalar` writes a `string` result from the CLR value. Char, Guid, and date scalars still use the rendered text, and that text no longer includes captured console output.

A JSON object is parsed from the renderer terminal, so a console log no longer makes `JsonDocument.Parse` fail and drop `structuredContent`. A string scalar no longer picks up the log as a prefix. `mcp-hosting.md` says console output stays in the text content. `dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs` reported Total 14, Passed 14, including `Should_keep_console_output_out_of_structured_content`.

Compared `## Goal (implementer restatement)` with `## Requirements`. No misread.

## Issues

None.
