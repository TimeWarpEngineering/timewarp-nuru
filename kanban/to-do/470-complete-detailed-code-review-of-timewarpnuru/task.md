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
- [ ] Worker re-pins SHA at review start if `origin/master` moved

### Round 1

- [ ] Area reviewers write `review/round-1/<area>.md` (7 files)
- [ ] Merge → `review/round-1/merged.md` (counts + stable `M#`)
- [ ] Parsing crash-safety: either fuzz malformed patterns or document why not (454 ran 200k with zero uncaught exceptions)

### Disposition / follow-through

- [ ] Child tasks for independent product fixes (`--parent 470`), or same-task nits committed here
- [ ] `review/disposition.md`
- [ ] `## Results` + `### How to validate`
- [ ] Do not `kanban done` from the implementer; host lifecycle / human gate

## Notes

### Prior art (do not duplicate blindly)

- **454** — 2026-07-06 six-agent review. Children 454-001…018, 020…032 are **done**; **454-019** remains in `to-do` (long-line REPL redraw). Parent 454 is still in-progress for that leftover + a batched human REPL pass. 454-033 (MCP examples) was archived won't-do when MCP was frozen.
- **455** — design issue from 454-012 (`IParameterSymbol` availability) — done.
- **458** — versioning/release convention. Implementation children through 458-009 done; **458-010** (attestation) still in-progress. Do not re-litigate F1–F9.

### Snapshot at kitchen create (2026-09-04)

- Origin-home SHA: `648369f6` (`Merge pull request #232` — Aspire 13.5 `WithTerminal` spike, task 467)
- Package version: `3.0.0-beta.77` (`source/Directory.Build.props`)
- Other open work that may overlap: **391** (epic full DI support, in-progress), **454-019**, **458-010**

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
