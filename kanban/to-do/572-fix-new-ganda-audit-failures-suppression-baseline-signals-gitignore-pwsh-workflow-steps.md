# Fix new ganda audit failures: suppression baseline, signals gitignore, pwsh workflow steps

## Description

`ganda repo audit` on master (2026-11-11) reports 6 failures after a ganda upgrade added checks.
Three are errors and block attestation and `kanban publish` signing:

- `analyzer-suppressions`: no `.timewarp/suppression-baseline.json`
- `timewarp-signals-gitignore`: `/.timewarp/signals/` not ignored
- `workflow-pwsh-shell`: `workflow.yml` has no `defaults.run.shell: pwsh` and 7 bash steps

Warnings: `memsearch-scaffold` (outdated `.githooks/*.cs`), `vscode-window-icon`, and
`ci-code-coverage`.

## Requirements

- `ganda repo audit --fix` output committed as-is (audit fixes are policy): baseline seeded,
  gitignore line, `defaults.run.shell: pwsh`, refreshed git hooks, `.vscode/settings.json`.
- The 7 bash `run:` steps in `.github/workflows/workflow.yml` converted to PowerShell by hand with
  the same behavior: Classify changed paths, Read OS codename, Install zsh, Require every stage,
  Keep last two Packages artifacts, Delete staged packages, Run release pipeline.
- `ganda repo audit` reports no Error. PR CI (which exercises `changes`, `stage`, `ci` jobs) green.
- `ci-code-coverage` stays a warning: tests run through `dev workflow --stage` (Jaribu runfiles),
  not `dotnet test`, so the check's MTP flags do not apply. Needs its own design if wanted.

## Checklist

- [x] `ganda repo audit --fix` applied
- [x] Workflow bash steps converted to pwsh
- [x] `ganda repo audit` has no Error
- [ ] PR open, CI green
- [ ] Release path (`Run release pipeline` step) re-checked on the next `dev release`

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-11)

## Notes

The pwsh default added by `--fix` makes the bash steps fail under PowerShell, so the hand
conversion must land in the same PR. Reference pwsh steps: timewarp-ganda `workflow.yml`
(Keep last two Packages artifacts) and timewarp-architecture (Classify changed paths).
