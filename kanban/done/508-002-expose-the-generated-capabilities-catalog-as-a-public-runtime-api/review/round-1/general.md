# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch `task/508-002-expose-the-generated-capabilities-catalog-as-a-pub` vs `master` (`a5bf6cfd`)

## Summary

`NuruApp.Capabilities` returns a per-app, lazily built `CapabilitiesResponse`, `FilterByGroup` reproduces the old `--group-filter` rule, and `--capabilities` serializes that same instance. `returnType` is the last, nullable endpoint member, and the new tests cover build-time availability, caching, printed JSON, group filters, and ordinary delegate and endpoint kinds. The gap is problem-pair endpoints: the descriptor classifies the awaited `OneOf<TSuccess, SharedProblemDetails>` while the invoker classifies and writes `TSuccess`.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs:395
- Description: `DescribeReturnType` always classifies `handler.ReturnType` (the awaited type). For an endpoint whose response is `OneOf<TSuccess, SharedProblemDetails>`, that awaited type is the `OneOf`, so `GetOutputStrategy` returns `Json` and the published name is the `OneOf` type. `EmitCommandResult` does not write that value. When `handler.ReturnsProblemPair` is set it unwraps to `handler.SuccessTypeName` and runs `GetOutputStrategy` on the success type (raw text for `string`, JSON for a record, nothing for `Unit`); a problem goes to stderr and sets the exit code. `T508ProblemQuery` in `tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs` is `IQuery<OneOf<string, SharedProblemDetails>>` and would be published as a JSON `OneOf` even though success output is the raw string `problem:ok`. `capabilities-08-runtime-catalog.cs` never builds a problem-pair route, so the new tests do not catch it. 508-004 maps this catalog onto MCP tools, and the parent contract is that the outcome is that `OneOf`.
- Suggestion: When `handler.ReturnsProblemPair` and `handler.SuccessTypeName` is set, pass that success type through the same `GetOutputStrategy` switch and strip `global::` for `name`. Leave a non-problem `OneOf` classified as the awaited type, because the invoker serializes that value as JSON. Add a catalog assertion on a problem-pair route (string success and a complex success) that the name and kind match the success type.
- Status: open
