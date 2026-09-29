# Round 1 — general
**Date:** 2026-09-30
**Scope reviewed:** branch `task/479-advertise-json-args-invocation-on-capabilities-and` vs `origin/master` — `invocation` on `--capabilities`, search decision, capabilities tests, and the agent-invocation contract.

## Summary

`CapabilitiesResponse.Invocation` is nullable and not C# `required`. `CapabilitiesJsonSerializerContext` already uses camelCase and `WhenWritingNull`, so a null `Invocation` is omitted and a document that lacks `invocation` still deserializes. `JsonSerializable(typeof(InvocationCapability))` is registered. `EmitResponseConstruction` is the only response builder and always assigns `InvocationCapability.Standard` (`--json-args`, `-`, `@`, `argvOverridesJson`, `error`), including when a group filter narrows `endpoints`. `CapabilitiesClient` still copies name, version, description, and endpoints; the comment and `agent-invocation.md` record that `invocation` stays in `RawJson`. Round-trip and live `--capabilities` tests assert the five fields, and a fixture without the property loads with `Invocation` null. The new doc matches the shipped binder: exact catalog `name` (ordinal), argv overrides JSON, repeated options replace rather than merge, JSON `null` is a type error on non-nullable values and clears a nullable reference, attached forms and any other value exit 1, REPL rejects `-`, and `~/` expands on `@path`. Root help lists `--json-args`; per-route help says values may come from it. The skill section links to `documentation/user/features/agent-invocation.md`. No bugs, suggestions, or nits.

## Issues

<!-- none -->
