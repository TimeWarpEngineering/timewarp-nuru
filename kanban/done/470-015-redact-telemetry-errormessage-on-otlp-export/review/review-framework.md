# Review framework — task 470-015

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-015-redact-telemetry-errormessage-on-otlp-export/
**Diff scope:** local uncommitted — telemetry redaction on failure paths (behavior, emitter, options remarks, docs, samples, new telemetry-02 test)
**Plan / brief:** Parent 470 M33 — stop exporting `Exception.Message` via Activity status/`error.message` when OTLP can leave the process; prefer `error.type`; document OTLP sink trust; keep generator/runtime parity
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task-work tw-implementation-review)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
