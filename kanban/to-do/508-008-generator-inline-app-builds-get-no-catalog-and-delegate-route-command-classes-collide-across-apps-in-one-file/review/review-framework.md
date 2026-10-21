# Review framework — task 508-008

**Date:** 2026-10-21
**Host task:** kanban/to-do/508-008-generator-inline-app-builds-get-no-catalog-and-delegate-route-command-classes-collide-across-apps-in-one-file/
**Diff scope:** branch `task/508-008-generator-inline-app-builds-get-no-catalog-and-del` vs `master` (`4c68dda8`, 657 lines). Product diff is the generator interpreter, command-class emitters, and the new generator tests plus the daemon and routing-34 workaround edits.
**Plan / brief:** Every inline `Build()` gets its own capabilities catalog, and delegate-route command classes are named per app and per route. See `task.md` Goal and Requirements.
**Effort:** 2 (by-diff). Roster is general only.
**Reviewer roster:** general
**Session IDs:** grok `01a15c6c-1504-78a0-b012-51f7497ddb32` (2026-10-21)

## Budget (by-diff)

- Lines changed: 657
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
