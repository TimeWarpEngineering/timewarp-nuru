# Optional McpBridge package mapping McpServerTool methods to Nuru routes

## Description

An optional package that turns an MCP server's `[McpServerTool]` / `[Description]` methods into Nuru routes, so an MCP server such as Roslynk gets a CLI with `--capabilities`, `--json-args`, help, completion and REPL without hand-written routes. It also emits filtered `--capabilities` so agents don't load every tool (the "minimal context bloat" lesson from OpenCode 2.0).

## Depends on

- 506

## Proposed package name: TimeWarp.Nuru.McpBridge

`TimeWarp.Nuru.Mcp` is already the PackageId of Nuru's dev-assist MCP server (`source/timewarp-nuru-mcp/timewarp-nuru-mcp.csproj`), which goes the other way (an MCP server that helps write Nuru apps). `McpBridge` names the direction (MCP tools into Nuru routes), keeps the two packages apart on NuGet, and keeps the bridge out of Nuru core. Busara rated this name GREEN.

## Go/no-go gate (after task 506)

Decide go or no-go from task 506's results before any implementation:

- `--json-args` vs MCP JSON Schema: do Roslynk's real tool inputs (nested objects, arrays, enums) map cleanly? If nested objects don't, decide between a JSON-string fallback, flattening, or no-go.
- `--capabilities` size: is the measured token size a problem at about 40 tools? Build group or prefix filtering only if it is.
- Runtime registration: Nuru 3.0 routes are compile-time (`RunAsync` throws unless intercepted, `source/timewarp-nuru/nuru-app.cs`). Decide between a runtime path (would need new non-generated dispatch, reflection, not AOT) and a build-time generator over `[McpServerTool]` methods (AOT-friendly). The intel eval prefers reflection first and a generator only for a real AOT consumer; confirm that is feasible.
- Record the decision and its evidence in this task.

**Decision (2026-10-21): no-go, archived (Steven).** The contracts-first direction (508) covers the need from the other side: one `[Operation]` contract serves CLI and MCP through `TimeWarp.Nuru.McpHost` (508-004, schema fidelity in 508-015), and Roslynk now hosts its MCP surface on McpHost with a Nuru CLI over the same contracts. Bridging hand-written `[McpServerTool]` methods into Nuru routes would add a second, reflection-based registration path for no current consumer.

## Requirements (if go)

- Separate opt-in package; nothing added to Nuru core.
- Match `[McpServerTool]` / `[Description]` by name and register routes; carry descriptions into help and `--capabilities`.
- `--capabilities` filter support suitable for the tool count.
- Tests against Roslynk's tool set.

## Context

- timewarp-intel `evaluations/2026-10-15-opencode2-nuru.md` (Busara: GREEN). Recommended order item 2: runtime reflection bridge in an optional package, size M; package name `TimeWarp.Nuru.McpBridge` GREEN.
- timewarp-intel `evaluations/2026-10-15-nuru-client-server.md` (Busara: daemon sequenced after the McpBridge go/no-go).

## Checklist

- [x] Go/no-go decision recorded: no-go, archived 2026-10-21 (see gate section)
- [ ] (go) Package scaffold `TimeWarp.Nuru.McpBridge`
- [ ] (go) Tool-to-route mapping and `--capabilities` filtering
- [ ] (go) Tests and docs

## Session

- Created: 4066933 (2026-10-15)
- Archived as no-go on the 508 close-out: 2026-10-21
