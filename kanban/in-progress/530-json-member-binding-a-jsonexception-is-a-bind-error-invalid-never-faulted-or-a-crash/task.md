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

## Goal (implementer restatement)

The generated JSON-member binder (`__JsonBind_N`, from `json-args-emitter`) calls
`JsonSerializer.Deserialize` for object/array members without a guard, so a value that fails *inside* the
member (unknown enum name in an array item, nested type mismatch, nested value that does not parse) escapes
as an exception: a crash (exit 134) in process, `error=Faulted` through the daemon/McpHost.

Acceptance:

- A serializer failure on a JSON member (`JsonException`, plus `NotSupportedException`,
  `InvalidOperationException`, `FormatException` from the serializer or a converter) becomes a
  `NuruBindError` (`InvalidValue`) for that member, so it flows like scalar bind failures: usage text, or
  `error=Invalid` with `UseProblemRendererForErrors()`, exit 1.
- Same result in process (`--json-args`, `--no-daemon`), forwarded through the daemon, and over McpHost
  (`isError` with `error=Invalid`).
- The message names the member and the JSON path inside it (`Steps: $[0].Tool: ...`), with no stack trace.
- A handler exception is still `Faulted`.
- Tests cover an unknown enum in an array member, a nested-object type mismatch and an unparsable nested
  value, in process, through the daemon and over McpHost.
- Release in a Nuru beta and the Roslynk follow-up are later steps (release waits for an explicit ask).

## Checklist

- [x] JSON-member deserialization failures become `NuruBindError`
- [x] `error=Invalid` / exit 1 in process, forwarded and over MCP; no crash
- [x] Tests
- [ ] Released in a Nuru beta
- [x] Roslynk follow-up noted (see ## Results)

## Session

- Created: Roslynk cockpit 1287ec9b-6d3e-4805-9dd9-5ef6634981c8 (2026-10-23)
- 2026-11-14 implementer-claude (ganda task work, implement oracle): merged origin/master, guarded the
  JSON-member deserialize, added runtime helpers, tests and a doc line. Audit boy-scout: `ganda repo audit
  --fix` created the gitignored `.vscode/settings.json` for the advisory `vscode-window-icon` warning (no
  tracked change). One audit run showed a transient `kanban-landed` failure (another live claim kitchen
  out of sync with origin-home); the rerun passed with no change on this branch.
- The post-commit attest then failed `runfile-executable`: the new test runfile (untracked when the audit
  ran) lacked the executable bit. Fixed with `chmod +x` + `git update-index --chmod=+x`; the audit rerun
  passed (44 passed, 0 failed).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-13T20:20:29Z
- Review oracle: reviewer-cursor (cursor, vendor xai, model grok-4.7), body tw-implementation-review, effort 2, roster general, turn cap 120 recorded by the host budget, this cursor session uncapped (no --max-turns flag), session id not exposed, round 1, disposition clean — 2026-11-14
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-13T20:28:33Z

## Review

- Round 1: Done

## Notes

- Related: 526 (schema for converter-bound types). This task is binding only, not schema.

## Results

### What changed

- `source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.assign.cs`: the `ValueShape.Json`
  assign wraps `JsonSerializer.Deserialize` in `try`/`catch (Exception) when
  (NuruBindError.IsJsonMemberFailure(...))`, sets `__ok = false` and records
  `NuruBindError.ForJsonMember(key, pattern, exception)`. The route pattern is threaded in from `EmitFill`
  (`json-args-emitter.validation.cs`). The bind method then returns the failure like any scalar bind error,
  so `--json-args`, `TryBind`, the daemon and McpHost all report it as a bind error.
- `source/timewarp-nuru/invocation/nuru-bind-error.cs`: two public static helpers called by generated code.
  `IsJsonMemberFailure` is true for `JsonException`, `NotSupportedException`, `InvalidOperationException`
  and `FormatException` (a typed-ID converter that parses text). `ForJsonMember` builds an `InvalidValue`
  error: `Error: Invalid value for '<key>' on '<pattern>'. <key>: <json path>: <serializer message without
  the " Path: … | LineNumber …" suffix>`. No stack trace.
- `documentation/user/features/agent-invocation.md`: one bullet on JSON-member failures being
  `InvalidValue`.
- Tests: `tests/timewarp-nuru-mcphost-tests/mcphost-06-json-member-bind-errors.cs` (also in the CI
  multi-mode assembly). An `[Operation]` with `IReadOnlyList<JbStep> Steps` and `JbOptions? Options` covers
  an unknown enum name in an array item (`Steps: $[0].Tool`), a nested-object type mismatch
  (`Options: $.Limits.Max`) and an unparsable nested Guid (`Steps: $[0].Target`). It checks `TryBind`
  (kind `InvalidValue`, key, message, no stack trace), in-process `--json-args` with plain errors (exit 1),
  with `UseProblemRendererForErrors()` and with `--no-daemon` (exit 1, `error=Invalid`), McpHost in process
  (`isError`, `error=Invalid`), and forwarded through a real `NuruDaemonServer` for McpHost and for the CLI
  via `AddServeRoute()` (same text as in process). A handler exception stays `error=Faulted` over McpHost
  (local and forwarded) and still escapes the in-process CLI as itself.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | Pass, 0 warnings, 0 errors |
| Test | `./bin/dev test` | Pass: CI multi-mode 2190 total, 2184 passed, 6 skipped, 0 failed; standalone phase 0 failed |
| Audit | `ganda repo audit` | Pass: 44 passed, 0 failed, 1 skipped (after `--fix` for the advisory `vscode-window-icon`; see ## Session) |

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-06-json-member-bind-errors.cs
```

Expect: 5 tests pass. Before this change the in-process `--json-args` cases crash with an unhandled
`System.Text.Json.JsonException`, and McpHost reports `error=Faulted` instead of `error=Invalid`.

### Follow-ups

- Release: ships in the next Nuru 4.0.0 beta (cutting a release waits for an explicit ask; the "Released
  in a Nuru beta" box stays open until then).
- Roslynk, when bumping to that beta: tighten
  `McpSurfaceTests.WhenAMultiQueryOperationNamesAToolOutsideTheEnum_ThenTheWholeCallFails` to
  `error=Invalid`, and add a `--no-daemon` CLI test for the same input (exit 1, no crash).

### Review

Round 1, roster general, effort 2, turn cap 120. Session id not exposed. Counts: bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix). Disposition `clean`. No wontfix and no escalation. The restatement matches the task: `FormatException` is an extra filter for a typed-ID converter, and the beta cut stays a later step.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
