# Review framework — task 478

**Date:** 2026-09-29
**Host task:** kanban/to-do/478-bind-json-args-in-the-nuru-source-generator/
**Diff scope:** branch `task/478-bind-json-args-in-the-nuru-source-generator` vs `origin/master` (commits `cc8fa535`, `37a4c57b`, `77bd449b`). Product files: `source/timewarp-nuru-analyzers/` emitters, validators, `BuiltInFlags`, NURU_R005; tests `routing-33-json-args.cs`, `generator-50-json-args-reserved.cs`, `generator-45` count, CI standalone list. Design of record: `kanban/done/457-research-json-based-cli-invocation-for-large-agent-payloads/research/recommendation.md` (follow-up 1 only; invocation object and docs are task 479).
**Plan / brief:** Reserve `--json-args`, peel it before config-arg filtering and user routes, bind JSON keys onto existing generated locals with no reflection, and cover the recommendation's error, tie, REPL, and help rules.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
