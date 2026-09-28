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

Kanban-only. Verified on this branch against commit fdad4d13. Nothing under `source/`, `tests/`, or `samples/`
changes. No build or test run.

- Archived with a `Closed 2026-09-28 (triage 477)` note: 434 (superseded by 470-001), 436 (superseded by 464),
  255 (delivered by 448 and the 458 series; Phase 3 workflow owned by ganda), 257 and 258 (unfilled placeholders
  whose scope shipped in `tools/dev-cli`, `source/timewarp-nuru-devcli`, 458-006, and ganda).
- 437 sits in `kanban/backlog/` and is gone from `kanban/to-do/`.
- 456 names the single NuGet strategy (`NuGetVersionService`, `PublishStateClassifier`); the git-tag strategy
  is gone from the requirements.
- 069 points at `source/timewarp-nuru/repl/key-bindings/` and `tests/timewarp-nuru-tests/repl/`, and at
  `AddRepl(...)` with `KeyBindingProfileName` defaulting to `"Default"`.
- 365 is the remaining work: DEBUG comments in `route-matcher-emitter.cs`, and `TreatWarningsAsErrors` still
  false under `tests/` and `samples/`.
- 219 lists the eleven `tests/**/*.cs` files over 500 lines; `wc -l` on 2026-09-28 matches that list.
- `kanban/to-do/task-work.progress.log` is absent.

### How to validate

Smoke:

```bash
git diff --stat fdad4d13^..HEAD -- source tests samples
test ! -e kanban/to-do/task-work.progress.log
test ! -e kanban/to-do/437-investigate-nuru-protocol-hyperlinks-and-terminal-based-navigation.md
test -f kanban/backlog/437-investigate-nuru-protocol-hyperlinks-and-terminal-based-navigation.md
grep -F 'Closed 2026-09-28 (triage 477)' \
  kanban/archived/255-epic-dev-cli-unified-developer-tool.md \
  kanban/archived/257-phase-2-dev-cli-core-command-migration.md \
  kanban/archived/258-phase-3-dev-cli-development-workflow-commands.md \
  kanban/archived/434-review-and-clean-up-help-modelcs.md \
  kanban/archived/436-add-examples-support-to-help-output.md
grep -F 'NuGetVersionService' kanban/to-do/456-check-version-warn-when-source-is-multiple-versions-ahead-of-last-release.md
grep -F 'KeyBindingProfileName' kanban/to-do/069-global-user-key-binding-profiles.md
grep -F 'route-matcher-emitter.cs' kanban/to-do/365-cleanup-debug-diagnostics.md
find tests -name '*.cs' -print0 | xargs -0 wc -l | awk '$1 > 500 && $2 != "total" { print }' | sort -nr
```

Expect:

- `git diff --stat fdad4d13^..HEAD -- source tests samples` prints nothing.
- `kanban/to-do/task-work.progress.log` is missing, and 437 exists only under `kanban/backlog/`.
- Each of the five archived files prints a `Closed 2026-09-28 (triage 477)` line.
- 456 mentions `NuGetVersionService`, 069 mentions `KeyBindingProfileName`, and 365 mentions `route-matcher-emitter.cs`.
- `wc -l` prints the same eleven paths and counts as the checklist in
  `kanban/to-do/219-review-large-test-files-for-refactoring-opportunities.md`, from 729 lines
  (`generator-26-constructor-dependency-resolution.cs`) down to 505 (`repl-29-word-operations.cs`).

## Notes

- Implementer: do not change anything outside `kanban/`. Report `ORACLE_RESULT: Done` once verified.
