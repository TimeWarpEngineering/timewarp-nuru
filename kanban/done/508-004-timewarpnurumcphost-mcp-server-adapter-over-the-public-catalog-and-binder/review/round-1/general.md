# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch `task/508-004-timewarpnurumcphost-mcp-server-adapter-over-the-pu` vs merge-base `dba6245a`. Package `source/timewarp-nuru-mcphost/`, generator `AddMcpRoute` / `[Destructive]` / mixed-app catalog list, calculator sample, mcphost tests, and the MCP hosting docs.

## Summary

`TimeWarp.Nuru.McpHost` projects `NuruApp.Capabilities` into MCP tools and runs `tools/call` through `TryBind`, `InvokeAsync`, and `RenderAsync`. Names, annotations, the group filter, bind errors as tool errors, the stdio `mcp` route, and `WithNuruTools` match the task. The stdio transport writes with `Console.OpenStandardOutput()`, so pointing `Console.Out` at stderr does not take the protocol stream.

One contract break: `NuruMcpContentFormat.Json` builds `structuredContent` from captured console text concatenated with the rendered body. A handler that logs and returns a value drops or corrupts the structured payload while the call still succeeds. Tests cover console capture and JSON content separately, so they miss the combination.

Checked and not filed: flags are omitted from `required` because a bound flag binds `false` when absent (the `mh-run` test calls with `count` only). An unbound discriminator flag is different in the binder and is not distinguishable from the catalog, which is the limit already written in Results. Optional-value options stay `required` and are stricter than the binder; schema-valid calls still bind. The mixed-app list fix feeds help and `--capabilities` from `RoutesInIndexOrder` once. REPL and completion still concatenate `model.Endpoints`, and their `yielded` set keeps suggestions unique.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-mcphost/nuru-mcp-tool-invoker.cs:93
- Description: `InvokeAsync` sets `stdout` to `capturedOut + terminal.Output`. `CreateStructuredContent` (line 140) then parses that whole string when `ReturnTypeKind` is `Json` (lines 146-159). Console and default-terminal writes land in `capturedOut`. `RenderAsync` writes the CLI JSON to a separate `TestTerminal`. A handler that returns an object and also writes a log line produces text that is not one JSON value. `JsonDocument.Parse` throws, the catch returns null, and `StructuredContent` is omitted while `IsError` stays false. The text block still holds the log plus the JSON, so the call looks successful and the JSON option is empty. Scalar wrapping has the same split. `WriteScalar` (lines 180-197) writes numbers from the CLR `result`, but strings and other non-numeric results write `text`, so `{"result": ...}` includes the log prefix. `documentation/user/features/mcp-hosting.md` says a json route's object is passed through and any other value is wrapped as `{"result": value}`. `Should_return_structured_content_when_json_is_requested` uses `mh-report` with no console write. `Should_capture_console_output_into_the_result` does not set `ContentFormat.Json`.
- Suggestion: Build structured content from the rendered terminal text, or from `result`, and keep captured console output in the text blocks only. For scalars, wrap the CLR value. Use the rendered text only when there is no CLR value.
- Status: open
