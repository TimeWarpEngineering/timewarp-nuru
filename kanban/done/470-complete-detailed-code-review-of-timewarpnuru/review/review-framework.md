# Review framework — task 470

**Date:** 2026-09-04
**Host task:** `kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/`
**Diff scope:** whole-repo review of origin-home `master` (not a PR delta, not the implement kitchen commit)
**Pinned SHA at kitchen create:** `648369f6a32737aa64037e4ed3ffb6c115764aa3` (`Merge pull request #232`)
**Pinned SHA at implement start (`git rev-parse origin/master`):** `38480f5721518b61ed33beef7bd9174225c0c7c8`
**Pinned origin/master at implement start:** `38480f57 publish kanban 470` (merge of `648369f6` + kitchen-only `f2fb2c9f`; product tree is still PR #232)
**Pinned version:** `3.0.0-beta.77` (`source/Directory.Build.props`)
**Plan / brief:** `task.md` — successor to 454 (2026-07-06); re-review current tree for new defects, 454 regressions, and uncovered areas
**Effort:** elevated — 7 area reviewers (not default effort-1). Host `review` oracle must keep this roster even though the default review body says effort 1.
**Reviewer roster:** core-runtime, repl-completion, analyzers-generators, parsing, aux, tests-infra, security
**Session IDs:** kitchen created Grok `01a06a77-1631-7543-b181-07ddc524f9fe` / ganda claim 3277544; implement Grok `01a06a90-5daf-7851-b2e5-6c9130f2b437` / ganda claim 3295430; review oracle Grok `01a06a9a-68a0-7f43-bf01-1e7391582be2` (2026-09-04)

**Re-pin at implement start:** `origin/master` moved from kitchen-create `648369f6` to `38480f57` (`publish kanban 470`). No product-code commits between those SHAs. Reviewers judge `38480f57` (equivalently product `648369f6`).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome for an area
- Address the current tree and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Do not re-open 454-done findings unless the defect is still present (cite current file:line)
- Do not duplicate **454-019** (long-line REPL redraw) as a new finding; note status only
- MCP (`source/timewarp-nuru-mcp/`) is frozen for features; correctness/security bugs still in scope
- 458 versioning *policy* is out of scope; new code defects in release/check-version/workflow are in

## Finding template

Each reviewer writes `review/round-N/<reviewer>.md` using the `tw-implementation-review` finding template (`bug` / `suggestion` / `nit`, `Status: open`, file:line, suggestion).

## Merge

After all seven reviewers finish, write `review/round-N/merged.md` with counts table, stable `M#` IDs, source attribution, and duplicate collapse notes.

## Disposition

Exit bar: 0 `open` findings on this task *or* remaining opens filed as `--parent 470` children with IDs listed in `review/disposition.md`. Outcome is `clean` or `accepted-exceptions`.
