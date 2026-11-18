# Review framework — task 590

**Date:** 2026-11-19
**Host task:** kanban/in-progress/590-telemetry-flush-drop-the-1-s-grace-delay-after-a-successful-force-flush-and-bound-flush-time-for-a-blackholed-endpoint/
**Diff scope:** branch `task/590-telemetry-flush-drop-the-1-s-grace-delay-after-a` vs merge-base `0e1bd801` (master). Four commits ahead. One commit behind: `8c4adc2a` lands the kanban file with no content change. Product diff is `nuru-app.cs`, telemetry-03, telemetry-07, the telemetry-overhead benchmark (script, readme, 2026-11-19 results), user telemetry docs, changelog, and the Jaribu pin.
**Plan / brief:** Drop the fixed 1 s grace delay in `NuruApp.FlushTelemetryAsync` and bound a blackholed endpoint to one short flush budget (default 2 s), while a set `OTEL_EXPORTER_OTLP_TIMEOUT` replaces that default. Re-run `benchmarks/telemetry-overhead/run-benchmark.sh` so case 3 sits near case 2.
**Effort:** 3 (by-diff, 5106 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** review Grok session `01a1efa6-70e4-7740-ac80-1004c8ea1c50` (2026-11-19). Implementer session `4ed09d35-117b-46d2-a11b-3ab17572538d` is on `task.md`.

## Budget (by-diff)

- Lines changed: 5106
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
