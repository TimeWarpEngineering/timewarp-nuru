# Review framework — task 470-004

**Date:** 2026-09-22
**Host task:** kanban/in-progress/470-004-stop-option-descriptions-at-endofoptions/
**Diff scope:** branch `task/470-004-stop-option-descriptions-at-endofoptions` vs `origin/master` (commits `31e44f0e`, `c79110dd`, `94f0f635`). Product files: parser description stop, parameter-name validation, `BuiltInTypeNames`, adjacent-parameter span, NURU_P004 message, parser tests, route-pattern anatomy and ubiquitous language.
**Plan / brief:** Parent 470 findings M6 (bug), M22 (suggestion), M38 and M39 (nits). Option descriptions must stop at `EndOfOptions`. Parameter names use `IsValidIdentifierFormat` (hyphens remain legal on option names). One built-in type list feeds the parser, `InvalidTypeConstraintError`, and `NURU_P004`. `AdjacentParametersError` spans the whole second parameter.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Grok review oracle `01a0c773-b874-7471-86fa-34ae5dbd670f` (2026-09-22). Implementer `01a0c764-7616-7f92-8acc-29005b4b41eb` (2026-09-22).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
