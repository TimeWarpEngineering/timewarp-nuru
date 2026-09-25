# Review framework — task 472

**Date:** 2026-09-24
**Host task:** kanban/to-do/472-fix-repo-audit-failures-on-master-bin-dev-capabilities-global-usings-analyzer-runfile-exec-bit-and-shebang/
**Diff scope:** branch `task/472-fix-repo-audit-failures-on-master-bin-dev-capabili` vs `origin/master` (commit 909da85e)
**Plan / brief:** Clear five `ganda repo audit` failures on master — bin-dev, global-usings-analyzer (TW0007 + TimeWarp.SourceGenerators), runfile exec bits, runfile shebang, then verify dev-cli-capabilities; build+test green.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
