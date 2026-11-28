# Review framework

## Budget (by-diff)

- Lines changed: 334
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 1 scope

**Date:** 2026-11-28
**Diff scope:** branch `task/540-enable-the-green-key-ledger-skip-on-nuru-after` vs `master` (44290254..85363b84)
**Plan / brief:** `## Goal (implementer restatement)` in task.md: port the green-key-v1 ledger in shadow mode (log hit/miss and the would-line, skip nothing)
**Effort:** 3, roster axes general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)

## Round 2 scope

**Date:** 2026-11-28
**Diff scope:** fix-pass delta `85363b84..ac2b28c6`, re-verifying round-1 M1–M5 and scanning the delta for new defects
**Effort:** 2, roster axes general, turn cap 120
**Reviewer roster:** general (review oracle, Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code and write only under `review/round-N/`
- Severity is bug, suggestion, or nit. Status starts as open
- Zero issues is a valid outcome
- Do not re-run a full build, a full test run, an audit, or the hooks. One targeted command may check one suspected bug
- Prior rounds are immutable. New work goes in `round-(N+1)/`
