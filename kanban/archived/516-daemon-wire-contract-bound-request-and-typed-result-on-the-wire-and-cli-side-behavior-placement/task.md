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

- [x] Ask Roslynk whether a typed result or CLI-side behavior is needed; record the answer here (answered from Roslynk's code, see Results)
- [x] Promote or archive (archived)
- [ ] Implement the decided option(s) (not needed)
- [ ] Tests and docs (not needed)

## Notes

- Filed from the 508-005 review (grok, round 1, 2026-10-19). The implementation follows the restatement; this is a contract decision, not a defect.
- Related: 508 (former parent; moved out as 508-010 so 508 could close), 508-003 (binder, `RenderAsync`), 508-004 (McpHost shares the server), 508-005 (daemon), Roslynk daemon.

## Session

- Created as 508-010: 3918158 (2026-10-19)
- 2026-10-21: moved out from under 508 to top-level 516 (Steven) so 508 can close; decision still open.
- 2026-11-28: backlog review, archived (Steven: "archive 516"). Claude session 06092692.

## Results

Archived 2026-11-28. The shipped 508-005 contract is accepted: argv (or the JSON object) on the wire, text and exit code back, and the whole `INuruBehavior` pipeline on the server.

The decision gate asked whether Roslynk needs a typed result in the CLI process or a CLI-side behavior. Nobody on the Roslynk side answered in words. The answer below is read from Roslynk's master on 2026-11-28:

- Roslynk is on `TimeWarp.Nuru.Daemon` and `TimeWarp.Nuru.McpHost` 4.0.0-beta.12 and uses the contract as shipped.
- Its CLI prints server-rendered text through `UseVerbatimRendererOutput()` and `UseProblemRendererForErrors()` (508-011 and 508-018, both landed after this card was filed). The `IResponseRenderer<T>` classes run in the engine, on the server.
- `RoslynkForwarder` reads only the exit code and the output text of a forwarded call.
- Roslynk registers no `INuruBehavior`, so there is nothing to place on the CLI side.
- Roslynk's board has no open task, and none asks for either option.

Nothing in the daemon changed for this card. `documentation/user/features/served-apps.md` already describes the accepted contract. The 508-005 `## Learnings` departure is closed as accepted, not a defect.

If a consumer later needs the value in the CLI process (a non-default `OutputStrategy`, post-processing a typed response) or a CLI-side behavior, file a new task; the Requirements above are a starting spec.
