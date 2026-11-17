# Review framework — task 596

**Date:** 2026-11-17
**Host task:** kanban/in-progress/596-consumer-check-blames-a-shouldly-assertion-in-a-doc-snippet-as-a-broken-nuru-api/
**Diff scope:** branch `task/596-consumer-check-blames-a-shouldly-assertion-in-a` vs merge-base `26c48c11540c1e463e67be68cc47c7a5d0e39ceb` (`master...HEAD`). Product paths: `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh`, `documentation/user/guides/migrating-from-system-commandline.md`. Kitchen `task.md` move is out of product scope.
**Plan / brief:** Stop `nuru_errors` from blaming `CS1061` on a C# built-in receiver, keep blaming a missing member on a package type, and name Shouldly in the migration guide's `## Tests` section.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a1e91d-eb5a-7e52-b7ff-20fe8aafdeb8` (2026-11-17). Implementer session `8b6163b4-ed04-4f23-9e84-d0676118d037`. Review-2 oracle grok `01a1e92e-f35d-7720-a305-ede15ff180ce` (2026-11-17).

## Budget (by-diff)

- Lines changed: 97
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 2

**Date:** 2026-11-17
**Diff scope:** `2b5b5e208841b9adef65fae8aa1779737b44c6d2..ce03e1f57c68bbd2ca6215ee23a5474f1414a0cc`. Product path: `skills/tw-nuru-verify/SKILL.md` (re-check of M1). Prior round files stay frozen.
**Effort:** 1 (general only). The budget block above is this round.
**Session ID:** review-2 oracle grok `01a1e92e-f35d-7720-a305-ede15ff180ce`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
