# Review framework

## Budget (by-diff)

- Lines changed: 272
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Rounds

- **Host task:** `kanban/in-progress/591-007-endpoint-file-info-directory-info-and-ip-address-properties-do-not-bind/`
- **Round 1** (2026-11-19): branch `task/591-007-…` `90af1d6a..a90c3da2`, kanban kitchen excluded. Roster general,
  effort 2. Review oracle claude (claude-opus-5-5), session 60ea781d-3829-4221-9b51-afd0d8e39f1c.
- **Round 2** (2026-11-19): fix delta `a90c3da2..038fa1e3` (commit 038fa1e3, M1 fix), kanban kitchen excluded
  from product scope; `task.md` `## Results` re-read. Roster general, effort 2. Review oracle claude
  (claude-opus-5-5).
