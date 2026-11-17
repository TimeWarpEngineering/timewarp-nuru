# Review framework — task 597

**Date:** 2026-11-18
**Host task:** kanban/in-progress/597-update-outdated-package-pins-contracts-beta-34-source-generators-beta-17/
**Diff scope:** branch `task/597-update-outdated-package-pins-contracts-beta-34` vs merge-base `940642415b3fd3cbcafcb76358a155587609fbf2` (`master...HEAD`). Product change is the two `PackageVersion` lines in `Directory.Packages.props`. The `task.md` move is board bookkeeping.
**Plan / brief:** Bump `TimeWarp.Contracts` 2.0.0-beta.33 → 2.0.0-beta.34 and `TimeWarp.SourceGenerators` 1.0.0-beta.16 → 1.0.0-beta.17. Leave Nuru's own version unchanged. No release. Fix any new diagnostic in code.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer-grok `01a1eab7-4b0b-75e1-bf1f-67ff1bf1c92b`; implementer-claude `f302caf4-ac96-4a87-991e-9e7bcf00fadb`

## Budget (by-diff)

- Lines changed: 98
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
