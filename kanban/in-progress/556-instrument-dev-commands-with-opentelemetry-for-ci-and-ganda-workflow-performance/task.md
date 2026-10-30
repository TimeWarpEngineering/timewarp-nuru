# Instrument dev commands with OpenTelemetry for CI and ganda workflow performance

## Description

`dev` does not export OpenTelemetry. `tools/dev-cli/dev.cs` builds with `NuruApp.CreateBuilder()` and never calls `UseTelemetry()`. The DevCli endpoints and the local commands start no activities. With `UseTelemetry()` absent, the source generator emits no tracer, meter, or OTLP exporter, so a CI run and a ganda run that shells out to `dev` leave no trace.

`dev workflow` already prints `ProcessorCount` and a wall-clock line per PR/merge step (`WriteStepDuration` in `tools/dev-cli/endpoints/workflow-command.cs`): attestation, clean, build, verify-samples, test. Release mode names its stages (tag-gate, check-version, locate-run, download-artifact, verify, push) and does not time them the same way. Those lines stay in the job log. They are not a trace we can query for where the pipeline spent time.

Calling `UseTelemetry()` alone is not enough. It emits one span per dispatched command (`TelemetryBehavior`, source `TimeWarp.Nuru.Behavior`, tags `command.name` and `command.type`, histogram `nuru.behavior.commands.duration`). `dev workflow` calls `CleanCommand.Handler`, `BuildCommand.Handler`, and the other handlers in-process, so those stages never pass through the route dispatcher and would not get their own command spans. The workflow span would be one bar for the whole pipeline.

This task turns telemetry on for this repo's `dev` CLI and adds a child span for each workflow stage, so a CI job or a ganda walk that runs `dev` shows where the time went. Ganda's own lifecycle spans stay in ganda (tasks 488 and 490 there). Nuru task 550 is the follow-up that lets a provider subscribe to extra sources such as `TimeWarp.Ganda`. This task uses a source name the generated provider already listens to.

## Requirements

- Call `UseTelemetry()` from `tools/dev-cli/dev.cs`. Set `ServiceName` to a string literal such as `timewarp-nuru-dev` (a non-literal `OtlpEndpoint` is ignored with no diagnostic; that is task 533). Leave the endpoint unset in code so export follows `OTEL_EXPORTER_OTLP_ENDPOINT`. A run with that variable unset exports nothing.
- Add a child span around each workflow stage, parented to the active `workflow` command span:
  - PR/merge: attestation, clean, build, verify-samples, test.
  - Release: tag-gate, check-version, locate-run, download-artifact, verify, push.
- Put `ci.mode`, `ci.stage`, and the step name on the stage span. Put `ProcessorCount` on the pipeline span. Set span status to Error when that step aborts the pipeline. Record `error.type` only. Leave API keys, tokens, argv, and exception messages off the span.
- Start those activities with `new ActivitySource("TimeWarp.Nuru")` or `new ActivitySource("TimeWarp.Nuru.Behavior")`. The generated provider subscribes only to those two names (`TelemetryEmitter.EmitTelemetrySetup`). A new source name creates spans that are never exported. Task 550 is the place to add further sources.
- Keep the existing terminal duration lines. The trace is a second copy of the same boundaries.
- Prove it. With `OTEL_EXPORTER_OTLP_ENDPOINT` pointed at a local sink, one short `dev` command exports a single command span, and a workflow run (a focused test of the stage helper is enough; a full `dev workflow` is not required) exports the workflow span plus one child per stage that ran. With the endpoint unset, the same command exports nothing.
- `dev` is `PublishAot`. `dev self-install` must still produce `bin/dev` after `UseTelemetry()` is compiled in. Fix new AOT warnings in this code. The OpenTelemetry packages already flow from `source/timewarp-nuru/timewarp-nuru.csproj`.
- Document the variables a GitHub Actions job or a ganda walk sets when it wants the trace: `OTEL_EXPORTER_OTLP_ENDPOINT`, optional `OTEL_SERVICE_NAME`, and the incoming W3C trace context when ganda is the parent process. `TelemetryBehavior` already parents its span to `Activity.Current`.
- Leave the GitHub Actions workflows without a collector and without an endpoint. Wiring an org collector is a follow-up once an endpoint exists.
- Leave `dotnet build` and `dotnet test` uninstrumented. This task times the stages inside `dev`, not the child SDK processes.
- Change this repo's `tools/dev-cli` only. The shared DevCli package does not own `workflow-command.cs`.

## Checklist

- [ ] `tools/dev-cli/dev.cs` calls `UseTelemetry()` with a literal service name and no hardcoded endpoint
- [ ] PR/merge and release stages each get a child span on `TimeWarp.Nuru` or `TimeWarp.Nuru.Behavior`
- [ ] Stage attributes and error status match the requirements above
- [ ] Terminal duration lines still print
- [ ] Proof: one command span with an endpoint set, no export with it unset, and stage children on a workflow run or an equivalent test
- [ ] AOT `dev self-install` still builds
- [ ] Env vars for CI and ganda are documented next to the dev CLI

## Notes

- Confirmed on master 2026-10-30: `dev.cs` has no `UseTelemetry()`, and neither `tools/dev-cli` nor `source/timewarp-nuru-devcli/content` references OpenTelemetry.
- Related, not blocking: 520 (AOT cost of `UseTelemetry()`; an unset endpoint stays on the cheap path, and a set endpoint is noise next to a multi-minute pipeline), 533 (non-literal `OtlpEndpoint`), 550 (extra `ActivitySource` names for in-process ganda spans).
- Ganda tasks 488 (`UseTelemetry` wiring) and 490 (task lifecycle spans) cover ganda's own process. This task covers the `dev` process those walks and `dev workflow` share.

## Session

- Created: 01a18ace-92d3-79d1-a591-3d9d0330fb9d (2026-10-30)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a18adc-ffe1-7d63-b6c0-ef595c7b56de, max-turns 500, Blocked — 2026-10-30T03:31:50Z

## Blocked

2026-10-30: placeholder — work starts now; this is not the final answer
