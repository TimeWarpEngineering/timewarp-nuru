# Redesign capabilities contracts for agent-first CLI automation

## Description

Redesign Nuru `--capabilities` to be contract-first for AI agent consumption, with DTOs as the single source of truth and serialization/deserialization guaranteed to round-trip.

This is a forward-looking beta redesign. Backward compatibility is not required. The goal is to define the right long-term contract, then make emission and tests enforce it.

## Checklist

- [ ] Define and document vNext contract model (no separate JSON schema; C# contracts are the schema)
- [ ] Rename command concept in contract from `CommandCapability` to `EndpointCapability` to align with Nuru terminology
- [ ] Use strongly typed `EndpointKind` enum (`Query`, `Command`, `IdempotentCommand`) instead of free-form message type string
- [ ] Change top-level response to a flat canonical `Endpoints` list for agent discoverability
- [ ] Include `GroupPath` on each endpoint so grouping context remains available without tree traversal
- [ ] Add `Aliases` to endpoint contract
- [ ] Add `DefaultValue` to parameter and option contracts
- [ ] Add `AllowedValues` to parameter and option contracts (populate for enum-backed bindings)
- [ ] Ensure option contract exposes both `IsFlag` and `IsRepeated`
- [ ] Keep `IsCatchAll` on parameter contract and ensure emitted property naming matches contract exactly
- [ ] Remove non-contract output fields from capabilities payload unless explicitly modeled in DTOs
- [ ] Replace string-built capabilities JSON emission with DTO instance construction + serializer context output
- [ ] Update serializer context registrations for all new/renamed contract types
- [ ] Add tests: DTO serialization shape tests for new contracts
- [ ] Add tests: DTO deserialization tests for new contracts
- [ ] Add tests: end-to-end `--capabilities` output parses as valid JSON
- [ ] Add tests: end-to-end `--capabilities` output deserializes to `CapabilitiesResponse`
- [ ] Add tests: enum `AllowedValues` are emitted and deserialized correctly (parameters + options)
- [ ] Add tests: options with no values still emit required arrays/collections as empty, never omitted
- [ ] Add tests: endpoints with no params/options/aliases still produce valid JSON (no trailing commas)
- [ ] Add tests: `IsFlag`, `IsRepeated`, `IsCatchAll`, `DefaultValue` round-trip correctly
- [ ] Run full capabilities test suite and CI tests
- [ ] Update samples/docs to reflect the new agent-first capabilities contract

## Notes

### Product Direction

- `--help` remains optimized for humans.
- `--capabilities` is optimized for AI agents.
- Capabilities contracts are versioned with Nuru package versions.

### Design Decisions Captured

- No backward compatibility guarantees needed in beta for this redesign.
- No standalone JSON schema file is required.
- The C# DTO contracts define the wire contract.
- Emission must be generated from DTO instances and serializer context to eliminate contract drift.

### Motivation / Prior Gaps

Recent analysis found emitter-model drift and test blind spots (field name mismatches, missing required collections, invalid JSON scenarios, unmodeled fields). This task resolves those design flaws by moving to a strict contract-first pipeline with roundtrip validation.

## Results

### What was implemented
- Rewrote `capabilities-response.cs` with new agent-first contract: `CapabilitiesResponse` → flat `Endpoints` list, `CommandCapability` → `EndpointCapability`, `MessageType string` → `Kind EndpointKind` enum, added `GroupPath string[]`, `Aliases string[]`, `DefaultValue`, `AllowedValues`, `IsFlag` fields
- Deleted `GroupCapability` class entirely (hierarchical groups model removed)
- Updated `capabilities-json-serializer-context.cs`: removed old type registrations, added `EndpointCapability`, `EndpointKind`, added `UseStringEnumConverter = true`
- Rewrote `capabilities-emitter.cs`: replaced hand-built JSON string approach with C# code that builds typed `CapabilitiesResponse` DTO instances and serializes via `JsonSerializer.Serialize()` + `CapabilitiesJsonSerializerContext`
- `AllowedValues` implemented: `Compilation` threaded into `CapabilitiesEmitter.Emit()` from `interceptor-emitter.cs`; `ExtractEnumValues()` helper uses Roslyn `GetTypeByMetadataName()` to populate enum member names for enum-typed parameters and options

### Files changed
- `source/timewarp-nuru/capabilities/capabilities-response.cs` — full rewrite
- `source/timewarp-nuru/capabilities/capabilities-json-serializer-context.cs` — full rewrite
- `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` — full rewrite + AllowedValues via Compilation
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` — pass `compilation` to `CapabilitiesEmitter.Emit()`
- `tests/timewarp-nuru-tests/capabilities/capabilities-01-basic.cs` — rewritten for new DTOs
- `tests/timewarp-nuru-tests/capabilities/capabilities-02-integration.cs` — updated to JSON parse + deserialize assertions
- `tests/timewarp-nuru-tests/capabilities/capabilities-03-groups.cs` — updated for flat model + GroupPath
- `tests/timewarp-nuru-tests/capabilities/capabilities-04-roundtrip.cs` — NEW file with roundtrip + AllowedValues tests

### Key decisions
- `EndpointKind` enum serializes via `UseStringEnumConverter` with camelCase policy: `Query` → `"query"`, `IdempotentCommand` → `"idempotentCommand"`
- `GroupPath` computed by splitting `route.GroupPrefix` on `' '`; empty array for top-level routes
- `AllowedValues` populated via Roslyn `GetTypeByMetadataName()` — `Compilation` was always available at the call site, just not threaded through
- `GroupHierarchyBuilder` retained (used by other emitters); only the capabilities emitter dependency removed

### Test outcomes
- All 31 new capabilities tests pass
- Full CI: **1093 passed, 7 skipped (pre-existing), 0 failed**
