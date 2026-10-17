# Contracts-first hosting: public catalog, JSON binder and MCP adapter

## Description

Make a Nuru endpoint class the single contract for an operation, so one definition serves the CLI, an MCP server and any other host without a second schema. Today the generator emits the CLI, the `--capabilities` catalog and the `--json-args` binding, but each only exists inside the generated app: the catalog is a private printer, the binder is inline in the interceptor, and the attributes ship with the full runtime. A host that is not the CLI (an MCP server, a daemon, a test harness) cannot reach any of it.

This is the contracts-first direction: Nuru routes are the source of truth and MCP tools are derived from them. Task 507 is the opposite direction (MCP `[McpServerTool]` methods bridged into Nuru routes). Both read task 506's spike results; decide between them, or keep both, at 507's go/no-go gate.

Driving consumer: Roslynk (Peter Morris's Roslyn MCP server). Its planned shape is one contract class per operation in the timewarp-architecture style (outer static partial class, nested `Query`/`Command` with Nuru attributes, `Response`, `Validator`), a daemon that owns the Roslyn workspaces, a Nuru CLI whose handlers forward to the daemon, and an MCP server generated from the same contracts. Measured context cost of Roslynk's MCP surface today is about 25,000 characters per prompt (29 tool descriptions plus server instructions) in a harness that sends full schemas every turn; the CLI plus a one-line skill reduces the always-on cost to one line.

## Children

- 508-001 Attributes-only package `TimeWarp.Nuru.Contracts`
- 508-002 Public runtime capabilities catalog
- 508-003 Public `--json-args` binder and invoke
- 508-004 `TimeWarp.Nuru.McpHost` adapter (depends on 002 and 003)
- 508-005 Served endpoints: daemon host and forwarding client (backlog until 506 reports cold-vs-warm timing)

## Checklist

- [ ] 508-001 merged
- [ ] 508-002 merged
- [ ] 508-003 merged
- [ ] 508-004 merged
- [ ] 508-005 decided (go to to-do, or archive) from task 506 results
- [ ] 507 gate records the decision relative to this direction

## Notes

- Survey of timewarp-nuru master (2026-10-17): no MCP bridge in either direction; `source/timewarp-nuru-mcp` is the frozen dev-assist server (readme: frozen 2026-07-14) and holds the `TimeWarp.Nuru.Mcp` PackageId, so the adapter needs another name. `CapabilitiesResponse` and its serializer context are already public in core (`source/timewarp-nuru/capabilities/capabilities-response.cs`). The catalog is emitted by `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` as a private `PrintCapabilities` method. JSON binding is emitted by `json-args-emitter.cs` with no public entry point. Only consumer of the catalog is `TimeWarp.Nuru.Search`, which shells out and parses stdout.
- Nuru behaviors (`INuruBehavior`) wrap CLI invocation only; a non-CLI host that dispatches the same request types through TimeWarp.Mediator gets mediator behaviors instead. Validation for a contracts-first app therefore belongs on the mediator pipeline (as timewarp-architecture does with `FluentValidationBehavior`), with Nuru behaviors kept to CLI concerns such as telemetry.
- Related: timewarp-intel `evaluations/2026-10-15-opencode2-nuru.md` and `evaluations/2026-10-15-nuru-client-server.md`; SimCubeLtd/roslynk-cli as the daemon reference design.

## Session

- Created: 1479989 (2026-10-17)
