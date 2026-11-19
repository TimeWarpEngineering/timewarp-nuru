# Review framework

## Budget (by-diff)

- Lines changed: 565
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 591-003

**Date:** 2026-11-19
**Host task:** kanban/in-progress/591-003-root-help-hides-the-default-routes-parameters-and-options/
**Diff scope:** branch `task/591-003-root-help-hides-the-default-routes-parameters-and` (HEAD 2c2dc94a) vs `master` merge base; product commit ae50eac4
**Plan / brief:** root `--help` prints a root default route's usage pattern, description, parameters, options, and examples instead of a blank `Commands:` row; description-only default routes stay a `Commands:` row; named commands unchanged. Docs and migrate skill updated.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5, ganda task work review node); implementer session 01a1f377-ba63-7e51-926e-0243f2c72b0a

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
