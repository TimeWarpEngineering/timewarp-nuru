# Review framework

## Budget (by-diff)

- Lines changed: 932
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 321

**Date:** 2026-10-03
**Host task:** kanban/to-do/321-optimize-generator-to-auto-detect-configuration-usage/
**Diff scope:** branch task/321-optimize-generator-to-auto-detect-configuration-us vs master (commit 4d304e07)
**Plan / brief:** HasConfiguration follows real use (AddConfiguration() call or handler IConfiguration / IConfigurationRoot / IOptions<T> parameter) instead of the hardcoded true.
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** claude review oracle (ganda task-work, 2026-10-03)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
