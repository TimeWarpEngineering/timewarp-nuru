# Expose the generated capabilities catalog as a public runtime API

## Description

`--capabilities` is produced by `capabilities-emitter.cs` as a private static `PrintCapabilities(ITerminal, string? groupFilter)` that builds a `CapabilitiesResponse` literal and writes JSON. The document only exists on stdout. `TimeWarp.Nuru.Search` has to spawn the CLI and parse it (`source/timewarp-nuru-search/services/capabilities-client.cs`). An in-process host (MCP adapter, daemon, tests) has no way to enumerate routes, descriptions, option names, defaults, kinds or examples.

Emit the same literal as a public, lazily built static (working name `NuruApp.Capabilities`, or a generated `INuruCatalog` registered in DI) and make `--capabilities` print from it.

## Requirements

- Public, AOT-safe, reflection-free access to the full `CapabilitiesResponse` for the app, including the group filter overload.
- `--capabilities` output is byte-identical before and after (print from the shared instance).
- Add a return-type descriptor to `EndpointCapability` (name and whether it is `Unit`, primitive, or a complex type serialized as JSON), since hosts that map to another protocol need it and it is missing today.
- `TimeWarp.Nuru.Search` keeps working; optionally gets an in-process path when the CLI is the current app.
- Docs: `agent-invocation.md` and `built-in-routes.md` mention the runtime API.

## Checklist

- [ ] Emitter change and public surface
- [ ] Return-type descriptor in `EndpointCapability` and serializer context
- [ ] Tests: catalog equals printed JSON; group filter; return types
- [ ] Docs

## Notes

- Parent: 508. 508-004 depends on this.
- If 508-001 lands first, the DTOs live in the contracts package; the generated instance still lives in the app assembly.

## Session

- Created: 1479989 (2026-10-17)
