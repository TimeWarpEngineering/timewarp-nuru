# Review framework — task 591-001

**Date:** 2026-11-19
**Host task:** kanban/in-progress/591-001-option-value-and-value-tokens-are-swallowed-as-configuration-overrides-also-after/
**Diff scope:** Round 1 reviewed the branch vs `master`. Round 2 reviews the delta since round 1 base `16de6357` (commit `587436ba` only): `configuration-emitter.cs`, `interceptor-emitter.method-body.cs`, `json-args-emitter.cs`, `routing-42-option-equals-and-end-of-options.cs`, `documentation/user/guides/migrating-from-system-commandline.md`, and `skills/tw-nuru-migrate-system-commandline/SKILL.md`.
**Plan / brief:** Re-verify round 1 M1 and M2. M1: `AddCommandLine` receives only tokens the route filter drops. M2: the guide and skill state slash forms and `-o=value` exactly.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** round 1 reviewer grok `01a1f055-b7fd-7891-94b6-6728fcfd0c3a` (2026-11-19). Round 2 reviewer grok `01a1f064-c47a-7e62-a02d-05db885de292` (2026-11-19). Implementer session was not reported (claude-opus-5-5, 2026-11-18).

## Budget (by-diff)

- Lines changed: 220
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
