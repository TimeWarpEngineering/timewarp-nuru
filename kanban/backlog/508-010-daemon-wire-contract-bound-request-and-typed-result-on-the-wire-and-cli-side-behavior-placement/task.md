# Daemon wire contract: bound request and typed result on the wire, and CLI-side behavior placement

## Description

508-005 shipped `TimeWarp.Nuru.Daemon` following the implementer's goal restatement, which the round-1 reviewer recorded as departing from the original 508-005 text in two places (508-005 task file, `## Learnings`). Both departures are documented in `documentation/user/features/served-apps.md` and work. This task decides whether either should change, and implements what is decided.

### 1. Wire payload and result type

Original 508-005: the CLI serialises the bound request (508-003's binder output) and renders the returned typed result exactly as in-process invocation would.

Shipped: the CLI parses argv locally, then sends argv (or the JSON object for MCP and `--json-args`) over the socket; the server matches and binds again and runs the handler; the client prints the server's stdout, stderr and exit code. The output matches an in-process run for the default terminal, but the wire payload is not the bound request and the CLI never receives a typed result. Consequences: a non-default `OutputStrategy` or renderer on the CLI side cannot see the value; a client cannot forward and then post-process a typed response; the route is matched and bound twice.

### 2. Behavior placement

Original 508-005: "define which chain runs where (CLI-side telemetry versus server-side validation)."

Shipped: the whole `INuruBehavior` pipeline (validation, telemetry behaviors, retry, authorization) runs on the server; the CLI keeps argv parsing, type conversion, built-ins and the process-level telemetry setup inside `RunAsync`. That is the opposite placement from the task's telemetry-on-the-CLI example. There is no way to mark a behavior as CLI-side.

## Decision gate

Backlog until Roslynk (the driving consumer) says whether it needs a typed result in the CLI process or a CLI-side behavior. If neither, archive with the shipped contract recorded as accepted. If either, move to to-do and implement the chosen part.

## Requirements (when promoted)

- Option 1: send the 508-003 bound request (JSON of the bound object plus route id and build id) and return a typed result envelope (`returnType` kind from the catalog: unit, primitive, json) that the CLI renders through the same `RenderAsync` path as in-process. Keep argv parse errors local. Keep the stdout/stderr/exit-code text as a fallback for handlers that only write to the console.
- Option 2: a placement attribute or builder call per behavior (`Local`, `Server`, `Both`), default `Server`; `[LocalOnly]` semantics extended to behaviors; docs table in `served-apps.md` updated.
- Either option: 508-004 McpHost over a served app keeps working; daemon tests extended; the 508-005 `## Learnings` misread is closed in Results.

## Checklist

- [ ] Ask Roslynk whether a typed result or CLI-side behavior is needed; record the answer here
- [ ] Promote or archive
- [ ] Implement the decided option(s)
- [ ] Tests and docs

## Notes

- Filed from the 508-005 review (grok, round 1, 2026-10-19). The implementation follows the restatement; this is a contract decision, not a defect.
- Related: 508 (parent), 508-003 (binder, `RenderAsync`), 508-004 (McpHost shares the server), 508-005 (daemon), Roslynk daemon.

## Session

- Created: 3918158 (2026-10-19)
