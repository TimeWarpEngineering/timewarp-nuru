# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** product commit `b3b7702c` on `task/526-json-member-schema-publish-typed-ids-and-other`. Generator detection, schema transform, `--json-args` kind gate, mcphost-08, docs, changelog.

## Summary

A JSON member whose type carries its own `[JsonConverter]` and implements `IParsable<TSelf>` was exported as an untyped node. The generator now records those types while it walks the member (type arguments, array elements, public instance properties, base types), skips the string-enum converter, and sets `IsUuid` from a public instance `Guid Value`. `NuruJsonMemberSchema.Create` rewrites only an untyped node to `{"type":"string"}`, adds `"format":"uuid"` for the Guid case, fills missing `items` or `additionalProperties`, and still applies `[Description]` and `["string","null"]`. A top-level typed-ID option was untyped and the kind gate accepted only an object or array; it now publishes the uuid string schema and accepts a JSON string. `mcphost-08-typed-id-schema.cs` covers root, nested, array, dictionary values, nullable struct and nullable reference, a non-Guid parsable id, an unchanged string enum, and `--json-args` / MCP binding, with a test-local typed ID and no timewarp-architecture reference. Docs and the Unreleased changelog state the same rule. Recorded `./bin/dev build`, `./bin/dev test`, and `ganda repo audit` name commands and outcomes, and `### How to validate` names the three mcphost-08 checks. The restatement matches the task. The prerelease stays unchecked because the task defers the release to a later explicit ask.

## Issues
