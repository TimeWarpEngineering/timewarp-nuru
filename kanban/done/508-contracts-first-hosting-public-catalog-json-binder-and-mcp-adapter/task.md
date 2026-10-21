# Contracts-first hosting: public catalog, JSON binder and MCP adapter

## Description

Make one neutral operation contract serve the CLI, HTTP and MCP, with Nuru as the CLI and MCP binding. The neutral layer is a shared package (working name `TimeWarp.Contracts`, defined by timewarp-architecture task 384) that both Nuru and timewarp-architecture depend on. It stands on TimeWarp.Mediator, which both already use:

- The request class is the operation. Its kind comes from the mediator interface (`IQuery<T>`, `ICommand<T>`, `IIdempotentCommand<T>`), which Nuru already maps to `EndpointKind`.
- `[Operation("name", Description = "...")]` gives the canonical name and description. Members derive from the C# shape: name from the property, required from nullability and `required`, default from the initializer, allowed values from enums, description from `System.ComponentModel.Description`.
- Outcome is `OneOf<Response, SharedProblemDetails>` (the RFC 7807 type architecture already shares with its WASM client). CLI renders it to stderr and a non-zero exit code; MCP as a tool error; HTTP as problem details.
- Shared `I*Details` interfaces with composed FluentValidation validators stay as architecture does them.
- A catalog document generalizes Nuru's `CapabilitiesResponse`: operations, kind, members, return type, bindings. `--capabilities`, `tools/list` and OpenAPI are projections of it.

Bindings are optional attributes in their own packages: HTTP (`[ApiRoute]`, `[ApiEndpoint]`, `[EndpointAuthorize]`, unchanged), CLI (`[NuruRoute]`, `[Parameter]`, `[Option]` become overrides; with only `[Operation]` present Nuru synthesizes the route from the operation name and every member as an option named after the property), MCP (`[McpTool]` for annotation overrides; defaults from kind).

Nuru's part of this is below. Task 507 is the opposite direction (MCP `[McpServerTool]` methods bridged into Nuru routes); its go/no-go gate decides between the directions, or keeps both, using task 506's spike results.

Driving consumer: Roslynk (Peter Morris's Roslyn MCP server): one contract class per operation, a daemon owning the Roslyn workspaces, a Nuru CLI whose handlers forward to the daemon, and an MCP server derived from the same contracts. Measured context cost of Roslynk's MCP surface today is about 25,000 characters per prompt in a harness that sends full schemas every turn; the CLI plus a one-line skill reduces the always-on cost to one line.

## Children

- 508-001 Reference the shared contracts package; derive endpoints from `[Operation]`; handler placement and mediator dispatch
- 508-002 Public runtime catalog (a projection of the neutral catalog)
- 508-003 Public JSON binder and invoke (the neutral binder, exposed)
- 508-004 MCP binding `TimeWarp.Nuru.McpHost` over catalog and binder (depends on 002 and 003)
- 508-005 Served endpoints: daemon host and forwarding client (backlog until 506 reports cold-vs-warm timing)
- Follow-ups filed during the work, all merged: 508-006, 508-007, 508-008, 508-009, 508-011, 508-013, 508-014, 508-015, 508-016, 508-017, 508-018

## Depends on

- (cross-repo) timewarp-architecture 384 defines the shared package; 508-001 waits for its first published version. Not a board edge because depends-on is same-repo.

## Implementation order (serial, decided 2026-10-17)

One item at a time across the three repos:

1. timewarp-nuru 506 spike (decision data for 507 and 508-005)
2. timewarp-architecture 384: `TimeWarp.Contracts` 1.0.0-beta.1 published
3. Roslynk: typed request/response/outcome refactor, no surface change
4. timewarp-nuru 508-002 public catalog
5. timewarp-nuru 508-003 public binder and invoke
6. timewarp-nuru 508-001 consume the package, derive endpoints, handler placement, mediator dispatch
7. Roslynk: CLI written directly with `[Operation]`, forwarding handlers, daemon over the existing loopback `tools/call`; CLI-first skill
8. timewarp-nuru 508-004 McpHost
9. Roslynk: host McpHost in the daemon, delete reflection registration and the `multi_query` binder
10. timewarp-nuru 507 gate: archive or narrow
11. timewarp-architecture: roles slice, info analyzer, skill and docs; promote `TimeWarp.Contracts` to 1.0.0
12. timewarp-nuru 508-005 decided from step 1 timing

## Checklist

- [x] 508-001 merged
- [x] 508-002 merged
- [x] 508-003 merged
- [x] 508-004 merged
- [x] 508-005 decided (to-do or archive) from task 506 results: built and merged
- [x] 507 gate records the decision relative to this direction: no-go, 507 archived 2026-10-21

## Notes

- Three Nuru conflicts the neutral model resolves, all owned by 508-001: handler placement (Nuru requires a nested `Handler` and invokes it directly; architecture keeps handlers in the application layer and dispatches through the mediator; the neutral model allows any registered handler, which is also how a CLI edition registers a forwarding handler and a daemon the real one for the same request type); pipeline (dispatch endpoints through the mediator so FluentValidation runs identically in every host, Nuru behaviors stay CLI middleware); rendering (an optional `IResponseRenderer<TResponse>` the CLI and MCP hosts consult before falling back to JSON, so Roslynk keeps its outline format).
- Survey of timewarp-nuru master (2026-10-17): no MCP bridge in either direction; `source/timewarp-nuru-mcp` is the frozen dev-assist server (frozen 2026-07-14) and holds the `TimeWarp.Nuru.Mcp` PackageId. `CapabilitiesResponse` is public in core (`source/timewarp-nuru/capabilities/capabilities-response.cs`); the catalog is emitted by `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` as a private printer; JSON binding is emitted by `json-args-emitter.cs` with no public entry point; only `TimeWarp.Nuru.Search` consumes the catalog, by shelling out.
- Related: timewarp-intel `evaluations/2026-10-15-opencode2-nuru.md` and `evaluations/2026-10-15-nuru-client-server.md`; SimCubeLtd/roslynk-cli as the daemon reference design.

## Session

- Created: 1479989 (2026-10-17)
- Revised to the shared neutral-contract model: 1479989 (2026-10-17)
- Serial implementation order recorded: 1479989 (2026-10-17)
- 2026-10-21: every to-do child merged; shipped through 4.0.0-beta.5 (508-008 lands in the next beta). 507 archived as no-go. Done is blocked on backlog children 508-010 and 508-012.
- 2026-10-21: 508-010 moved out to top-level 516 (decision still open); 508-012 closed as superseded by 508-014 (shelved 2026-10-19; done, not implemented); 508 done (Steven).
