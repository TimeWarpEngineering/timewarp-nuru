# Triage to-do 2026-09-28: close superseded tasks, rewrite outdated specs

## Description

Review of all 18 nuru to-do tasks against master on 2026-09-28. This task records the kanban-only changes;
**all of the work is already committed on this branch** (commit fdad4d13). Nothing in `source/`, `tests/`, or
`samples/` changes.

## Checklist

- [x] Archive 434 (superseded by 470-001), 436 (superseded by 464), 255 / 257 / 258 (delivered by 448, the 458
      series, and ganda), each with a closing note
- [x] Move 437 (speculative hyperlink research) to backlog
- [x] Rewrite 456 for the single NuGet check-version strategy (git-tag strategy removed in 3.0.0-beta.72)
- [x] Rewrite 069 with current paths (`source/timewarp-nuru/repl/key-bindings/`, `tests/timewarp-nuru-tests/repl/`)
      and the current REPL API (`AddRepl(...)`, `KeyBindingProfileName` defaults to "Default")
- [x] Rewrite 365 to the remaining work (route-matcher-emitter DEBUG comments; warnings-as-errors in tests and samples)
- [x] Regenerate 219's list of test files over 500 lines
- [x] Delete the stray gitignored `kanban/to-do/task-work.progress.log`

## Results

Kanban-only. The implement and review oracles should verify the checklist against the branch (the moves are
recorded as renames in fdad4d13) and report Done without further edits. No build or test changes.

## Notes

- Implementer: do not change anything outside `kanban/`. Report `ORACLE_RESULT: Done` once verified.
