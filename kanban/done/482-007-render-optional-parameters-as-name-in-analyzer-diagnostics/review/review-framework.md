# Review framework — task 482-007

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-007-render-optional-parameters-as-name-in-analyzer-diagnostics/
**Diff scope:** commit 8950b15f on branch `task/482-007-render-optional-parameters-as-name-in-analyzer-dia` (vs its parent). Product files: `segment-definition.cs` (`PatternSyntax`), `route-location-lookup.cs`, overlap / REPL-default / reserved-json-args / service validators, `generator-30-nuru-r003-overlap.cs`.
**Plan / brief:** Finding A-4. Render optional parameters as `{name?}` / `{name:type?}` so diagnostic text is parser syntax, and anchor overlap, duplicate, unreachable, and REPL-default diagnostics on the source pattern.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** grok 01a10b26-755d-73c1-b43b-483e7d0b89da (2026-10-05)

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
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
