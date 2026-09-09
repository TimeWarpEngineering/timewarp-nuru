# Keep last two Packages artifacts in CI

## Description

Org GitHub Actions artifact quota filled (ganda master upload after #155
failed while tests were green). Nuru had **550** artifacts / ~5.5 GB —
mostly `Packages-*` from **PR and `always()` uploads** with **no
`retention-days`** (GitHub default ~90 days). Operator pruned to one
newest pack. Stop the refill.

Same policy is landing on **ganda 277** and **amuru 113**.

Release **458-002** promotes `Packages-*` from a CI push run (no
rebuild). If the blob for a SHA is gone: `gh run rerun <run-id>`
(releasing.md). Do not pack at release time.

## Requirements

File: `.github/workflows/workflow.yml`. Update releasing.md artifact
retention note if it still says “no retention-days / 90 days”.

### Upload gates (match ganda)

Today:

```yaml
if: always() && github.event_name != 'release' && !(github.event_name == 'workflow_dispatch' && inputs.mode == 'release' && inputs.confirm == 'release')
# no retention-days
# if-no-files-found: ignore
# path: artifacts/packages/*.nupkg
```

Change to **success() + `refs/heads/master`**, skip PR, skip release,
skip probe. Keep skipping release-mode re-upload (458-002). Keep
`path: artifacts/packages/*.nupkg` (nuru still promotes that set).

`if-no-files-found`: **error** on master upload (honest miss); do not
upload empty PR/fail runs.

### Retention backstop

`retention-days: 7`.

### Keep last two

After a successful Upload Artifacts step, delete older artifacts whose
**name starts with `Packages-`**. Keep the **two newest**. Do not delete
other artifact names.

- Job `permissions.actions`: **`write`** (today `read`). Comment why.
- `${{ github.token }}` / `GH_TOKEN`. Paginate.
- If the new artifact is not listed yet, do not fail the job.

### Not in scope

- Org retention UI
- Ganda/amuru YAML (those are 277 / 113)
- Changing 458-002 promote contract

## Checklist

- [ ] Upload only green master (not `always()`, not PRs)
- [ ] `retention-days: 7`
- [ ] `actions: write` + keep-last-two `Packages-*` prune
- [ ] releasing.md retention note matches
- [ ] Results + How to validate

## Session

- Created: 1965703 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
  (2026-09-09). Do not implement in cockpit.

## Notes

- Related: **458-002** (promote CI artifact), releasing.md “Artifact
  retention” appendix.
- Siblings: ganda **277**, amuru **113**.
