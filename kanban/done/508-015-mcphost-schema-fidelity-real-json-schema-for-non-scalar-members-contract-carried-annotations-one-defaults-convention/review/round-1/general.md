# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/508-015-mcphost-schema-fidelity-real-json-schema-for-non-s` vs `master` (schema export, catalog hints, McpHost schema writer, sample, tests, docs)

## Summary

McpHost now publishes the JSON Schema exported from `NuruJsonMemberContext` for a member that binds as one JSON value, and it maps `[DisplayName]`, `[Destructive]`, and `[OpenWorld]` onto tool title and hints, with `NuruMcpHostOptions.ToolAnnotations` winning per hint that is set. Defaults stay in the `default` keyword and are repeated as `Default: X.` because Codex's schema type drops `default` (confirmed at `openai/codex` `b2aa07db`, `codex-rs/tools/src/json_schema/types.rs`). The implementer restatement matches the rewritten task, including the Contracts fallback (BCL and Nuru attributes, architecture task 391).

Checked against the repo: `calculator-contracts --capabilities` emits `Notes` as an array of objects with `required: ["Label"]`, `Format` as `["object","null"]` with `Prefix` `["string","null"]` and the nested `[Description]` texts, plus `title` and `openWorld: false`. CLI probes accept a null `Format` and a null `Prefix`, and reject a string-encoded `Notes` with "wrong JSON type". `mcphost-04-schema-fidelity.cs` covers the object call, the hint override, both default placements, and nullable `int?`. No product defect in this diff.

## Issues

None.
