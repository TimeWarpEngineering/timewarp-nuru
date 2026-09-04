# Complete detailed code review of TimeWarp.Nuru

## Description

Whole-repo implementation review of **TimeWarp.Nuru as it exists on origin-home `master`**, not a PR delta.

This is the successor pass to kanban **454** (2026-07-06 full-repo review). 454 found ~51k lines of C# and drove child tasks 454-001…032; almost all of those remediations have landed. This task re-reviews the **current** tree (version `3.0.0-beta.77` at kitchen create; SHA pinned in `review/review-framework.md`) for new defects, regressions of 454 fixes, and gaps that 454 did not cover (DI/source-gen epic 391, capabilities/help/examples, AOT/incrementality, samples vs generator parity, CI inclusion drift).

Procedure: `tw-implementation-review` with **elevated effort** (area specialists, not default effort-1). Artifacts live under this folder task's `review/` subfolder. Same task through disposition — do **not** create a sibling “apply review findings” task.

## Requirements

### Scope (in)

Review product truth in this repo:

| Area | Paths |
|------|--------|
| Core runtime | `source/timewarp-nuru/` — app/builder, attributes, routing/binding, type conversion, help, capabilities, telemetry, DI seams, endpoints |
| REPL + completion | `source/timewarp-nuru/repl/`, `source/timewarp-nuru/completion/` |
| Analyzers / generators | `source/timewarp-nuru-analyzers/` — DSL interpreter, extractors, emitters, validators, incrementality |
| Parsing | `source/timewarp-nuru-parsing/` — lexer, parser, semantic validation, matchers |
| Aux | `source/timewarp-nuru-devcli/`, `source/timewarp-nuru-search/`, `source/timewarp-nuru-build/`, `source/timewarp-nuru-mcp/` (frozen product — defects only, no feature expansion) |
| Tests | `tests/` including CI inclusion (`tests/ci-tests/`) vs files on disk |
| Samples / tools | `samples/`, `tools/dev-cli/`, `runfiles/`, `benchmarks/` (correctness of harness, not score-chasing) |
| Infra | MSBuild (`Directory.Build.props`, packaging), `.github/workflows/workflow.yml`, `BannedSymbols.txt`, internals-visible-to generation |

Every finding **must** cite `path:line` evidence in the current tree. Zero issues in an area is a valid outcome. Do not invent findings.

### Scope (out)

- Re-opening 454 findings already marked **done** unless the defect is **still present** (prove it with current file:line).
- **454-019** (REPL redraw of lines longer than terminal width) — already tracked; mention if still open, do not duplicate as a new M#.
- **458** versioning/release *convention* (findings/convention/repo-matrix already exist). New **code** defects in `check-version`, `dev release`, or `workflow.yml` *are* in scope.
- MCP server **feature** work (frozen 2026-07-14). Security/correctness bugs still in scope.
- Strategic product forks (RFC/debate). This is implementation review, not “should we redesign the DSL”.
- Docs-only polish unless a doc **contradicts** code or ships a broken sample.

### Reviewer roster (effort)

Match 454’s area split, plus tests and security. Reviewers write independently under `review/round-1/`; orchestrator merges.

| File | Area |
|------|------|
| `core-runtime.md` | Routing, binding, type conversion, builders, capabilities, telemetry, DI |
| `repl-completion.md` | REPL input/key bindings, shell completion |
| `analyzers-generators.md` | Interpreter, extractors, emitters, validators, incrementality, AOT emit |
| `parsing.md` | Lexer, parser, semantic validation, matchers; optional malformed-pattern fuzz |
| `aux.md` | DevCli, search, build tasks, MCP (frozen) |
| `tests-infra.md` | CI inclusion drift, packaging, samples/generator parity, conventions |
| `security.md` | Injection, concurrency, untrusted input, analyzer-host crash (AD0001 / stack overflow), secrets |

Severity: `bug` · `suggestion` · `nit`. Status starts `open`. Prefer strongest severity when merging duplicates.

### Kitchen / procedure

1. Re-pin `review/review-framework.md` to the SHA actually reviewed (`git rev-parse origin/master`).
2. Round 1: spawn area reviewers (read-only on product code; write only under `review/round-1/`).
3. Merge → `review/round-1/merged.md` with stable `M#` IDs and counts table.
4. Evaluate:
   - Independent product fixes → **child tasks** (`ganda kanban create … --parent 470`), one coherent batch per child (454’s HIGH 1:1 / MEDIUM grouped model).
   - Tiny nits that belong on this branch → fix here, then `round-2/` re-review of the fix delta.
   - `wontfix` only with rationale + decider on the live `merged.md`.
5. Write `review/disposition.md` (`clean` or `accepted-exceptions`) when open count is 0 **or** remaining opens are filed as children with IDs recorded in disposition (parent stays in-progress until those children land, same as 454).
6. `## Results` **must** include rounds, roster, counts by severity/status, disposition, `review/` paths, and `### How to validate`.

**Forbidden:** process files next to `task.md`; a sibling “apply 470 findings” task; clobbering prior `round-N/`.

## Checklist

### Kitchen

- [x] Folder task created (`ganda kanban reserve` + `claim --repo timewarp-nuru`)
- [x] `review/review-framework.md` scaffolded with scope, roster, prior-art notes
- [x] Worker re-pins SHA at implement start (`origin/master` `38480f57`; product still `648369f6`)

### Round 1

- [x] Area reviewers write `review/round-1/<area>.md` (7 files)
- [x] Merge → `review/round-1/merged.md` (counts + stable `M#`)
- [x] Parsing crash-safety: 50k malformed patterns, seed `470001`, 0 uncaught exceptions

### Disposition / follow-through

- [x] Child tasks for independent product fixes (`--parent 470` 470-001…015); nits folded into those children
- [x] `review/disposition.md` (`accepted-exceptions`; M20 → epic 391)
- [x] `## Results` + `### How to validate`
- [ ] Do not `kanban done` from the implementer; host lifecycle / human gate (parent stays in-progress until children land)

## Notes

### Prior art (do not duplicate blindly)

- **454** — 2026-07-06 six-agent review. Children 454-001…018, 020…032 are **done**; **454-019** remains in `to-do` (long-line REPL redraw). Parent 454 is still in-progress for that leftover + a batched human REPL pass. 454-033 (MCP examples) was archived won't-do when MCP was frozen.
- **455** — design issue from 454-012 (`IParameterSymbol` availability) — done.
- **458** — versioning/release convention. Implementation children through 458-009 done; **458-010** (attestation) still in-progress. Do not re-litigate F1–F9.

### Snapshot at kitchen create (2026-09-04)

- Origin-home SHA: `648369f6` (`Merge pull request #232` — Aspire 13.5 `WithTerminal` spike, task 467)
- Package version: `3.0.0-beta.77` (`source/Directory.Build.props`)
- Other open work that may overlap: **391** (epic full DI support, in-progress), **454-019**, **458-010**

### Snapshot at implement start (2026-09-04)

- Origin-home SHA: `38480f5721518b61ed33beef7bd9174225c0c7c8` (`publish kanban 470`)
- Product tree: still `648369f6` (only kanban 470 files landed after PR #232)
- Package version: `3.0.0-beta.77`
- **454-019** still `to-do` (long-line REPL redraw) — reviewers note status only, do not duplicate as a new M#
- Host `review` node owns round 1 (7 area files), merge, parsing crash-safety note, and disposition. Implement oracle did not spawn reviewers.

### Related skills

- `tw-implementation-review` — procedure, templates, severity, disposition
- `tw-agent-collaboration` — QA workspace `review/`, same-task disposition, Results shape
- `tw-csharp` / Nuru `documentation/developer/standards/` — conventions to judge against
- `tw-nuru` — intended public surface (false positives vs real bugs)

### Dispatch (cockpit — not this session)

```bash
ganda task work 470 --repo timewarp-nuru --host herdr
```

## Session

- Created: Grok cockpit `01a06a77-1631-7543-b181-07ddc524f9fe` (2026-09-04) — reserved/claimed 470, wrote inbound brief
- Ganda claim: cramer@TWE-001 session 3277544 (2026-09-04)
- Implementer: Grok `01a06a90-5daf-7851-b2e5-6c9130f2b437` (2026-09-04) — re-pinned SHA, moved in-progress; did not run `tw-implementation-review`
- Ganda claim (implement): cramer@TWE-001 session 3295430 (2026-09-04)
- Review oracle: Grok `01a06a9a-68a0-7f43-bf01-1e7391582be2` (2026-09-04) — host `review` node, elevated 7-area roster, round 1

## Results

Review oracle (`tw-implementation-review`) completed round 1, merge, child split, and disposition on this id. Parent stays in-progress until children 470-001…015 land. No `kanban done` / `gh pr create` from this node.

### What was implemented

- Round 1: seven area reviewers wrote `review/round-1/{core-runtime,repl-completion,analyzers-generators,parsing,aux,tests-infra,security}.md`.
- Merge: `review/round-1/merged.md` with stable M1–M44.
- Parsing crash-safety: 50k malformed patterns, seed `470001`, 0 uncaught exceptions.
- Independent product fixes filed as `--parent 470` children 470-001…015 (nits folded into the child that already touches the same files).
- `review/disposition.md`: **accepted-exceptions** (M20 wontfix → epic 391; remaining opens filed as children).
- **454-019** noted only (still present at `repl-console-reader.cs:279`, `:310`); not duplicated as an M#.

### Files changed

- `kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/task.md`
- `kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/review/review-framework.md`
- `kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/review/round-1/*.md` (7 area + merged)
- `kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/review/disposition.md`
- Child kitchens 470-001…015 on origin-home `kanban/to-do/` (published separately)

### Key decisions

- Host default effort-1 overridden by this kitchen’s elevated 7-area roster (task brief + framework).
- Whole-repo review (not the implement kitchen delta). Product tree judged at `648369f6` / origin-home `38480f57`.
- Same-task disposition: no sibling “apply 470 findings” task. Children are 454-style product batches.
- M20 (source-gen `IEnumerable<T>` DI) wontfix on 470 — owned by epic 391.

### Review trail

| Field | Value |
|-------|--------|
| Rounds | 1 |
| Roster / effort | elevated: core-runtime, repl-completion, analyzers-generators, parsing, aux, tests-infra, security |
| Counts (final ledger) | bug 17 open, 0 fixed, 0 wontfix; suggestion 15 open, 0 fixed, 1 wontfix (M20); nit 11 open, 0 fixed, 0 wontfix |
| Disposition | **accepted-exceptions** — M20 → 391; all other opens filed as 470-001…015 |
| Paths | `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md` |

### Test outcomes

No product-code change on the 470 branch. Parsing fuzz (50k, seed 470001) ran against current sources: 0 uncaught exceptions. Area reviewers re-verified 454-done items against current file:line.

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-470-complete-detailed-code-review-of-timewarpnuru
git fetch origin
ganda kanban show 470
ls kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/review/round-1/
test -f kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/review/disposition.md && grep -E 'Outcome:|accepted-exceptions' kanban/in-progress/470-complete-detailed-code-review-of-timewarpnuru/review/disposition.md
ganda kanban show 470-001
ganda kanban show 470-015
```

**Expect**

- 470 column is `in-progress`
- `review/round-1/` contains the seven area files plus `merged.md`
- `review/disposition.md` **Outcome:** `accepted-exceptions`
- `merged.md` counts table: 17/15/11 open by severity plus M20 wontfix
- 470-001 and 470-015 exist under `kanban/to-do/` on origin-home (inbox)
- `review/round-1/parsing.md` crash-safety: 50k, seed 470001, 0 uncaught

**Automated gate**

None on this review branch (no product compile/test surface). Children own their test gates.

**Not in scope**

Product fixes (children), `kanban done` on 470, `gh pr create` (later host nodes).
