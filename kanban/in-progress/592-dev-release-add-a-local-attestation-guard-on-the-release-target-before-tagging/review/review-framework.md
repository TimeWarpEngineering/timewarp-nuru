# Review framework — task 592

**Date:** 2026-11-18
**Host task:** kanban/in-progress/592-dev-release-add-a-local-attestation-guard-on-the-release-target-before-tagging/
**Diff scope:** branch `task/592-dev-release-add-a-local-attestation-guard-on-the` vs `master` (`master...HEAD`, feature commit `997fd666`). Product paths: DevCli release guard and shared attestation runner, `dev workflow` call site, `documentation/developer/guides/releasing.md`, `tests/timewarp-nuru-tests/devcli/release-03-target-attestation.cs`, compile includes, and the audit `--fix` Amuru pin bump.
**Plan / brief:** `dev release` must refuse before tagging when the selected release target's tree has no valid ganda-audit note, using one shared verify implementation with `dev workflow` and the release job's attestation mode.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review oracle grok 4.7, session `01a1ee71-b7d5-7e70-9179-27b35acf8281` (2026-11-18). Implementer session was not reported.

## Budget (by-diff)

- Lines changed: 1250
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Restatement check

`## Goal (implementer restatement)` matches the original requirements: shared verify in DevCli content, guard after the release target is selected (step 9, including `--dry-run`), release-job mode (`off` / `warn` / `require`, unset or typo → require), pass line, refusal with status and remedy, `releasing.md`, and the five test cases. No misread recorded.
