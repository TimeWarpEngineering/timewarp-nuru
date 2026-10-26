# Review framework — task 547

**Date:** 2026-10-26
**Host task:** kanban/in-progress/547-runasync-isnt-intercepted-when-the-awaited-call-is-parenthesized-in-an-expression/
**Diff scope:** branch `task/547-runasync-isnt-intercepted-when-the-awaited-call-is` vs `master` (merge-base `0d82883af65dbbd7fdae4ff28cb6b745e1268cd6`, head `a2272504fdbd9e9cf893ff0917c7b83b3e293ae1`). Product diff is the generator intercept pass, NURU_I001, generator-71, generator-72, changelog, and `skills/tw-nuru/SKILL.md`.
**Plan / brief:** `## Goal (implementer restatement)` in `task.md`. Intercept `NuruApp.RunAsync` / `RunReplAsync` by method symbol in any expression in the scope that builds the app, and warn with NURU_I001 when a `RunAsync` receiver cannot be traced to a `Build()`.
**Effort:** 2 (general only; by-diff 742 lines)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, model Grok 4.7). max-turns uncapped; budget turn cap 120.

## Budget (by-diff)

- Lines changed: 207
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

## Round 2

**Date:** 2026-10-26
**Diff scope:** fix delta `993f24bd..31353fd4` (head `31353fd4c2fff72486bd562cb6d53e94a7cd16b2`). Re-check of M1, M2, M3 and defects on that delta only.
**Effort:** 2 (general only; by-diff 424 lines)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, model Grok 4.7). max-turns uncapped; budget turn cap 120.

## Round 3

**Date:** 2026-10-26
**Diff scope:** fix delta `d627bb2e..20bd7c36` (head `20bd7c36c86f0202be47178d3cb14ef27866403a`). Re-check of M4 and defects on that delta only.
**Effort:** 2 (general only; by-diff 232 lines)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, model Grok 4.7). max-turns uncapped; budget turn cap 120.

## Round 4

**Date:** 2026-10-26
**Diff scope:** fix delta `2ea4f9ac..53b3cc91` (head `53b3cc91b82abeb842704dcb97fabe2fbecb480b`). Re-check of M5 and defects on that delta only.
**Effort:** 2 (general only; by-diff 207 lines)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, model Grok 4.7). max-turns uncapped; budget turn cap 120.
