# Review framework — task 471

**Date:** 2026-09-09
**Host task:** kanban/in-progress/471-keep-last-two-packages-artifacts-in-ci/
**Diff scope:** branch `task/471-keep-last-two-packages-artifacts-in-ci` vs `origin/master` (product commits `dcb94d4f` plus kanban `70795ae4`)
**Plan / brief:** Green-master-only `Packages-*` upload, `retention-days: 7`, keep-last-two prune of older `Packages-*` artifacts; releasing.md + DevCli comments aligned. Do not change 458-002 promote contract.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle grok `01a083e8-1895-7002-bc2e-50cb324fad9d` (2026-09-09)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Product files in scope

- `.github/workflows/workflow.yml`
- `documentation/developer/guides/releasing.md`
- `source/timewarp-nuru-devcli/readme.md`
- `source/timewarp-nuru-devcli/content/any/services/ci-run-promotion.cs` (Design comment only)

Kanban kitchen (`task.md`) is out of product review except as the brief.

## Requirements to check

- Upload `if`: `success()` + `github.ref == 'refs/heads/master'`; skip PR, release, probe, release-mode dispatch
- Path stays `artifacts/packages/*.nupkg`; `if-no-files-found: error`; `retention-days: 7`
- After successful upload, paginated list + delete older names starting with `Packages-`; keep two newest
- `permissions.actions: write` with comment; `${{ github.token }}` / `GH_TOKEN`
- If just-uploaded name is not listed yet, do not fail the job
- Do not delete other artifact names; do not change 458-002 promote code
- releasing.md no longer claims “no retention-days / 90 days”
