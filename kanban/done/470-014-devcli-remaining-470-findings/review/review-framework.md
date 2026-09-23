# Review framework — task 470-014

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-014-devcli-remaining-470-findings/
**Diff scope:** local uncommitted — packable-project-service, GenerateNuruJsonContextTask, self-install + WindowsExeReplace, packable/self-install tests, Directory.Build.props Compile includes
**Plan / brief:** Parent 470 findings M23 (MSBuild exit 0 unparseable Properties → fail-loud), M24 (unexpected JSON-context exceptions → fail build), M41 (best-effort delete `dev.exe.old` after Windows self-install)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task-work review oracle (implementer-cursor / tw-implementation-review)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
