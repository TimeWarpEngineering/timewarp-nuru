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

- [x] `invocation` on `CapabilitiesResponse` and emitted by `capabilities-emitter.cs`
- [x] Capabilities tests and full-document fixtures updated
- [x] TimeWarp.Nuru.Search decision recorded (surface or ignore)
- [x] Agent contract documented; interim fat-field pattern documented
- [x] Nuru skill `--capabilities` section points at the contract

## Notes

- Commit and push your changes before reporting done. Run the build and test gate in the foreground.

## Results

`--capabilities` emits an optional `invocation` object on every generated app. `Invocation` is nullable and omitted from JSON when null, so a document that lacks the property still deserializes. The emitted object is `InvocationCapability.Standard`: `jsonArgs` is `--json-args`, `stdin` is `-`, `filePrefix` is `@`, `merge` is `argvOverridesJson`, and `unknownKeys` is `error`.

`TimeWarp.Nuru.Search` does not surface the object. It indexes endpoints and leaves `invocation` in the stored capabilities document (`RawJson`). Unknown JSON stays safe for older readers because the property is not C# `required`.

The agent loop (capabilities, then `--json-args -` or `@path`), the exact-name rule, argv-overrides-JSON, and the app-level fat-field pattern are in `documentation/user/features/agent-invocation.md`. The Nuru skill `--capabilities` section points at that contract.

No rule from the recommendation was changed.

Verification:

- `dotnet tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode total 1793, passed 1787, skipped 6, failed 0. Standalone phase passed.
- `dotnet run tools/dev-cli/dev.cs -- verify-samples`: `64/64 samples built successfully`.
- `ganda repo audit`: "Repository passes all audit checks." Passed 29, Failed 0. (`bin/dev` is gitignored; `ganda repo audit --fix` ran `self-install`).

### How to validate

Smoke:

```bash
dotnet tests/ci-tests/run-ci-tests.cs
dotnet run tools/dev-cli/dev.cs -- verify-samples
ganda repo audit
dotnet tests/timewarp-nuru-tests/capabilities/capabilities-04-roundtrip.cs
```

Expect:

- The CI runner exits 0. Multi-mode total 1793, passed 1787, skipped 6, failed 0. Standalone tests pass.
- `verify-samples` prints `64/64 samples built successfully`.
- Audit prints "Repository passes all audit checks."
- `capabilities-04-roundtrip.cs` exits 0 with 8 passed. Emitted `--capabilities` includes `invocation` with `jsonArgs` `--json-args`, `stdin` `-`, `filePrefix` `@`, `merge` `argvOverridesJson`, and `unknownKeys` `error`. A document that omits `invocation` deserializes with `Invocation` null.
