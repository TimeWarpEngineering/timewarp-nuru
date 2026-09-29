# Review framework — task 479

**Date:** 2026-09-30
**Host task:** kanban/to-do/479-advertise-json-args-invocation-on-capabilities-and-document-the-agent-contract/
**Diff scope:** branch `task/479-advertise-json-args-invocation-on-capabilities-and` vs `origin/master` (`4a047c4c..b1f17805`), product files only
**Plan / brief:** Follow-up 2 from task 457. Add optional nullable `invocation` on `CapabilitiesResponse`, emit `InvocationCapability.Standard` from `capabilities-emitter.cs`, leave `TimeWarp.Nuru.Search` indexing endpoints (object stays in `RawJson`), refresh capabilities tests, and document the agent contract in `documentation/user/features/agent-invocation.md` with the Nuru skill `--capabilities` section pointing at it.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
