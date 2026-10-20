# Review framework — task 508-009

**Date:** 2026-10-20
**Host task:** kanban/to-do/508-009-catalog-nested-enum-types-lose-allowedvalues/
**Diff scope:** branch `task/508-009-catalog-nested-enum-types-lose-allowedvalues` vs `master` (2 commits, 442 lines). Product diff is the generator enum lookup, capabilities fallback, nullable positional binder, and the nested-enum tests.
**Plan / brief:** Nested enum parameters and options must keep `allowedValues` in the catalog, `--capabilities`, shell completion, and the McpHost input schema, including nullable, doubly nested, and generic-outer enums. Resolution uses the compilation's symbols.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** grok `01a15785-3502-7ce1-a3e0-17c00c470564` (review oracle, 2026-10-20)

## Budget (by-diff)

- Lines changed: 442
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
