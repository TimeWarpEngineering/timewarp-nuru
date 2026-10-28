# Release 4.0.0-beta.7

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.6 to 4.0.0-beta.7, merge,
start a CI run on master (the newest master commit is usually a kanban-only land commit with no
run until 543 lands), then cut the release with `dev release` from the master worktree
(tw-release convention).

Ships everything merged since v4.0.0-beta.6: 537 (#365), 538 (#366), 551 (#367), 552 (#368).

Release-note callouts:
- OTLP `http/protobuf` exports now post to `{base}/v1/{traces,metrics,logs}`; per-signal `OTEL_EXPORTER_OTLP_{SIGNAL}_ENDPOINT`/`_PROTOCOL` are honored; a code-set `OtlpEndpoint` is a base URL (551).
- `UseTelemetry()` alone now emits one span per command (it registers `TelemetryBehavior` as the outermost behavior unless the app already added it); spans nest under the ambient `Activity` (552).
- Method-group handlers (`.WithHandler(Type.Method)`) under any behavior now compile (552).
- CI: non-code changes skip the build; the build runs as parallel jobs (537, 538). No package impact.

Unblocks ganda 488 (ganda task spans to SigNoz).

## Checklist

- [x] Bump version to 4.0.0-beta.7
- [ ] Bump PR merged
- [ ] Master CI green on the commit `dev release` tags
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages
- [ ] Consumer check (`skills/tw-nuru-verify`) against the published beta.7
- [ ] Tell ganda (488) beta.7 is out

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-28)

## Notes
