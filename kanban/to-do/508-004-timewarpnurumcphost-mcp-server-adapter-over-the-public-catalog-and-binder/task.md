# TimeWarp.Nuru.McpHost: MCP server adapter over the public catalog and binder

## Description

An opt-in package that gives any Nuru app an MCP server with no per-app code: one MCP tool per route, built from the public capabilities catalog (508-002) and dispatched through the public binder and invoke (508-003). Adds a built-in `mcp` route that serves over stdio, and a hosting extension for apps that already run an HTTP host (streamable HTTP transport).

Direction: Nuru routes are the contract, MCP tools are derived. Task 507 (`TimeWarp.Nuru.McpBridge`) is the reverse mapping and stays a separate package; the two can coexist, and 507's go/no-go gate records which direction Roslynk takes.

## Depends on

- 508-002
- 508-003

## Requirements

- Tool name = route literal (group path joined by a documented separator, or the route's declared name); description = route description; input schema = JSON Schema built from `parameters[]` and `options[]` (name, type, required, default, enum allowed values, arrays for repeated and catch-all), so the schema the agent sees is the same contract `--json-args` accepts.
- Tool annotations from `EndpointKind`: Query is readOnly and idempotent; IdempotentCommand is idempotent; Command is neither. `destructiveHint` left to an optional per-route attribute, not inferred.
- Server instructions from the app description plus an optional app-provided text.
- `tools/call` binds the arguments with the public binder (unknown keys are an error reported as an MCP tool error, not a protocol error), invokes, and returns the typed result rendered as the CLI would render it (text), with JSON content as an option.
- Filtering: expose the same group filter the catalog has, so an app can publish a subset of routes as tools.
- Package name: `TimeWarp.Nuru.Mcp` is taken by the frozen dev-assist server. Working name `TimeWarp.Nuru.McpHost`; confirm against 507's `McpBridge` so the pair reads as two directions.
- Tests: schema parity between `--capabilities` and `tools/list` for the calculator sample; a `tools/call` round trip; annotation mapping; filter.
- Docs: a page beside `agent-invocation.md`; note in the Roslynk-facing guidance that MCP and CLI share names and arguments exactly.

## Checklist

- [ ] Package scaffold, `ModelContextProtocol` reference (already pinned in `Directory.Packages.props`)
- [ ] Catalog to tool definitions, including annotations and schema
- [ ] `tools/call` through binder and invoke, text and JSON content
- [ ] `mcp` built-in route (stdio) and HTTP hosting extension
- [ ] Group filter
- [ ] Tests and docs

## Notes

- Parent: 508.
- Consumer: Roslynk's daemon would host this in-process so its MCP surface is derived from the same contracts its Nuru CLI is generated from; until then Roslynk reflects over its contracts assembly, and deletes that code when this lands.

## Session

- Created: 1479989 (2026-10-17)
