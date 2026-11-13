# JSON member binding: a JsonException is a bind error (Invalid), never Faulted or a crash

## Description

Found in Roslynk task 009 (fork PR TimeWarpEngineering/Roslynk#15), 2026-10-23. Steven confirmed that this is
ours and not Peter Morris's.

The generated `--json-args` / McpHost binder (`__JsonBind_N`, emitted by
`source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs`) deserializes a JSON member with
`JsonSerializer.Deserialize(JsonElement, Type, JsonSerializerContext)` and does not catch `JsonException`.
Scalar members already get a `NuruBindError` (for example `startItem: "first"` gives `error=Invalid`, exit 1).
A value that fails **inside** an object or array member does not. Examples are an unknown string-enum value,
a typed ID that does not parse, or a nested type mismatch. The exception escapes:

- **In process** (`--no-daemon`, or any non-daemon Nuru app): an unhandled `JsonException` crashes the
  process, exit 134.
- **Through the daemon / McpHost:** the call becomes `error=Faulted` (handler-exception path), not a binding
  problem.

Reproduction against Roslynk at 009's head (an enum value outside `MultiQueryOp`):

```bash
roslynk multi_query --no-daemon --json-args '{"solutionId":"<sln>","operations":[{"tool":"rename_symbol"}]}'
# Unhandled exception. System.Text.Json.JsonException: The JSON value could not be converted to
# Morris.Roslynk.Features.MultiQuery.MultiQueryOperation. Path: $[0].tool ...  -> exit 134
```

Every TimeWarp contract with an enum, a typed ID or a nested object in a JSON member hits this. That is why it
is our need.

## Requirements

- The JSON-member binder catches `JsonException` (and the `NotSupportedException`/`InvalidOperationException`
  that the serializer throws for unbindable shapes, if any reach it) and turns it into a `NuruBindError` for
  that member. It then follows the same path as scalar bind failures: usage text, or with
  `UseProblemRendererForErrors()` an `error=Invalid` problem, exit 1. In process and forwarded, CLI and
  McpHost (`isError` with the `error=Invalid` outline), the result is the same as the 508-018 behavior.
- The message names the member and the JSON path from the exception (`operations: $[0].tool ...`). Do not
  leak a stack trace.
- A handler exception still becomes Faulted. Only binding changes.
- Tests: an unknown enum value in an array member, a type mismatch in a nested object, and an unparsable
  nested value. Cover in process, through the daemon and through McpHost. No crash, and exit 1 / `isError`.
- Ship in a Nuru prerelease. Follow-up for Roslynk, when bumping: tighten
  `McpSurfaceTests.WhenAMultiQueryOperationNamesAToolOutsideTheEnum_ThenTheWholeCallFails` to
  `error=Invalid`, and add a `--no-daemon` CLI test.

## Checklist

- [ ] JSON-member deserialization failures become `NuruBindError`
- [ ] `error=Invalid` / exit 1 in process, forwarded and over MCP; no crash
- [ ] Tests
- [ ] Released in a Nuru beta
- [ ] Roslynk follow-up noted

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)

## Notes

- Related: 526 (schema for converter-bound types). This task is binding only, not schema.
