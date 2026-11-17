# Perf PR measurement bar before speedup claims

## Description

Add a skill/review line that requires interleaved or paired runs (N runs + median, or a stated confidence interval) before a Nuru performance PR claims a speedup. The same wording applies to Amuru and Terminal via the shared review/skill text — start as a skill or review checklist line, not a new BenchmarkDotNet framework.

## Requirements

- Add a short, enforceable bar: interleaved/paired runs; report N + median or a stated interval; no speedup claim without that evidence.
- Place it where Nuru perf PR reviewers and agents will see it (skill and/or AGENTS / review checklist).
- Wording should also cover Amuru and Terminal (shared "Cult of Performance" bar), even though this kitchen lives in Nuru.

## Checklist

- [ ] Pick the home for the bar (Nuru AGENTS, a shared skill, or both)
- [ ] Write the measurement requirement (paired/interleaved, N, median or interval)
- [ ] Point Amuru/Terminal reviewers at the same wording
- [ ] Confirm no new BenchmarkDotNet framework is introduced by this task

## Session

- Created: 874147 (2026-11-05)

## Notes

- Source: X web-rewrite agents eval option I (GREEN). https://github.com/TimeWarpEngineering/timewarp-intel/blob/master/evaluations/2026-11-05-x-web-rewrite-agents.md (PR https://github.com/TimeWarpEngineering/timewarp-intel/pull/103). Busara: start as skill/review line, not a new framework. Confirmed by Steven 2026-11-05.
- Filed only; not launched. Do not start until Steven says so.
