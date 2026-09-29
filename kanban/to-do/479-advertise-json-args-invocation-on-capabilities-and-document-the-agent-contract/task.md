# Advertise --json-args invocation on --capabilities and document the agent contract

## Description

Follow-up 2 from research task **457** (merged 2026-09-29, PR #271). Design of record:
`kanban/done/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md` (sections "Capabilities surface" and "What agents should do today").

## Depends on

- **478** (the `--json-args` binding) merged first, so the advertised contract matches shipped behavior.

## Scope (verbatim from the recommendation)


Add the optional `invocation` object to `CapabilitiesResponse` and emit it from `capabilities-emitter.cs`. Keep the property nullable so existing fixtures and older deserializers still load. Update `TimeWarp.Nuru.Search` only if it should surface the object; ignoring unknown JSON is already safe. Refresh capabilities round-trip tests and any full-document assertions. Document the agent loop (capabilities, then `--json-args -` / `@path`), the exact-name rule, argv-overrides-JSON, and the app-level fat-field interim for apps that cannot wait. Point the Nuru skill's `--capabilities` section at that contract.

## Requirements

- The `invocation` object is optional and nullable (not C# `required`); older readers keep working.
- Build, full CI test gate, samples, and `ganda repo audit` pass.

## Checklist

- [ ] `invocation` on `CapabilitiesResponse` and emitted by `capabilities-emitter.cs`
- [ ] Capabilities tests and full-document fixtures updated
- [ ] TimeWarp.Nuru.Search decision recorded (surface or ignore)
- [ ] Agent contract documented; interim fat-field pattern documented
- [ ] Nuru skill `--capabilities` section points at the contract

## Notes

- Commit and push your changes before reporting done. Run the build and test gate in the foreground.
