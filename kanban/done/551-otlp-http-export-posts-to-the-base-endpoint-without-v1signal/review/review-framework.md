# Review framework — task 551

**Date:** 2026-10-28
**Host task:** kanban/in-progress/551-otlp-http-export-posts-to-the-base-endpoint-without-v1signal/
**Diff scope:** branch `task/551-otlp-http-export-posts-to-the-base-endpoint-withou` vs `master` (merge-base `8cd9d502`), product commits `a156ea47` and `34cca17b`. 12 files, 735 lines changed.
**Plan / brief:** `UseTelemetry()` HTTP OTLP export must post to `{base}/v1/{signal}`. Env-only endpoints must not assign `o.Endpoint` (that disables the SDK append). A code-set `OtlpEndpoint` is a base URL: append `/v1/{signal}` for `http/protobuf` unless the path already ends with it; gRPC passes through. Per-signal endpoint overrides are used as-is. Code beats env. Task 517 early return stays.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** cursor review oracle (Grok 4.7); max-turns uncapped; review budget turn cap 120

## Budget (by-diff)

- Lines changed: 735
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
