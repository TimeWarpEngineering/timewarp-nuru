# McpHost schema fidelity: real JSON schema for non-scalar members, contract-carried annotations, one defaults convention

## Description

Rewritten 2026-10-20. The first version of this task asked McpHost to reproduce a hand-written MCP
server's `tools/list` byte for byte (options for `additionalProperties`, property order,
defaults-in-description, a deep-equal parity test), so Roslynk could keep its old schema. Steven decided
the fork does not keep that constraint: MCP clients are models that read `tools/list` on every connect, and
the goal is the cleanest Nuru architecture. Only the differences that are real defects stay.

Roslynk task 004 (PR #6) compared McpHost 4.0.0-beta.2's derived tools for its 29 `[Operation]` contracts
with its published ones. Of the differences found, these are defects for any Nuru app:

- **Non-scalar members publish as `string`.** `multi_query.operations` (an object array, bound as one JSON
  value via `--json-args`) becomes `"type": "string"`, so a model has to send JSON inside a string. A
  non-scalar member must publish its real JSON schema (array/object, item schema, required members, enum
  values), derived from the same type the binder deserializes into.
- **Annotations can only come from `EndpointKind`.** `readOnlyHint`/`idempotentHint` are derived; `title`,
  `destructiveHint` and `openWorldHint` cannot be set. Harnesses use these hints for permission prompts and
  auto-approval. Let a contract carry them (prefer `McpOperationBinding` in TimeWarp.Contracts; file the
  timewarp-architecture task if a property is missing) with a `NuruMcpHostOptions` override for apps that do
  not own their contracts.
- **Defaults convention.** McpHost puts defaults in the JSON `default` keyword; the ModelContextProtocol SDK
  convention some servers follow appends `Default: ...` to the description because not every harness shows
  `default` to the model. Check what Claude Code, Codex and Grok actually present to the model, pick one
  convention (or both), and document it. No per-app option unless the check says one is needed.
- **Nullable members** may keep `null` in their type if the binder accepts an explicit `null`; otherwise
  leave them as is and document that omitting means default.

Dropped from the first version: `additionalProperties`/ordering options and the deep-equal parity test
against a hand-written server.

## Requirements

- A sample with an object-array member and a nested object member publishes their real schemas; a
  `tools/call` with that JSON (not a string) binds and runs.
- A contract with `destructiveHint`, `openWorldHint` and `title` publishes them; the host override wins when
  set.
- The defaults decision is recorded with the harness evidence, implemented and tested.
- Docs: McpHost page (schema derivation, annotations, defaults); `neutral-contracts.md` (how a contract
  carries annotations).

## Checklist

- [ ] Non-scalar member schemas
- [ ] Annotations from the contract plus host override (architecture task filed if needed)
- [ ] Defaults convention decided from harness evidence and implemented
- [ ] Nullable handling decided
- [ ] Tests and docs

## Depends on

- 508-004

## Session

- Created: 2532391 (2026-10-19) by the Roslynk cockpit from the Roslynk 004 transport decision.
- Rewritten: 2532391 (2026-10-20) after Steven dropped byte-identical MCP parity for the Roslynk fork.

## Notes

- Related: 508-011 (verbatim text), 508-016 (concurrent calls), Roslynk task "move the daemon, stdio and CLI
  onto the Nuru served app".
