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
