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

- [x] Upload only green master (not `always()`, not PRs)
- [x] `retention-days: 7`
- [x] `actions: write` + keep-last-two `Packages-*` prune
- [x] releasing.md retention note matches
- [x] Results + How to validate
- [x] Review disposition (`clean`, round 1 general)

## Session

- Created: 1965703 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
  (2026-09-09). Do not implement in cockpit.
- Implementer: grok `01a083df-bcf1-7411-803a-67a7151c1629` (2026-09-09)
- Review oracle: grok `01a083e8-1895-7002-bc2e-50cb324fad9d` (2026-09-09)
- Review (round 1 general): grok `01a083e9-91c1-7e52-81a1-58b2587a76bf`
  (2026-09-09)

## Notes

- Related: **458-002** (promote CI artifact), releasing.md “Artifact
  retention” appendix.
- Siblings: ganda **277**, amuru **113**.
- Implementation review: `review/` (effort 1, round 1 general, disposition
  `clean`).

## Results

Green-master-only `Packages-*` upload with a 7-day retention backstop and
a post-upload prune that keeps the two newest `Packages-*` artifacts.

### What was implemented

- Upload Artifacts is `success()` + `github.ref == 'refs/heads/master'`,
  skipping PR, failed runs, probe, release events, and release-mode
  dispatch (458-002 still must not re-upload the bytes it just
  downloaded).
- `if-no-files-found: error` (honest miss on master). Path stays
  `artifacts/packages/*.nupkg`.
- `retention-days: 7`.
- `permissions.actions: write` so prune can DELETE. After a successful
  upload, list all artifacts (paginated), keep the two newest names
  starting with `Packages-`, delete the rest. If the just-uploaded name
  is not listed yet, skip prune (do not fail).
- releasing.md overview + “Artifact retention” appendix match. DevCli
  readme / `ci-run-promotion.cs` Design comment no longer say
  always-run / `actions: read`. Promote code is unchanged.

### Files changed

- `.github/workflows/workflow.yml`
- `documentation/developer/guides/releasing.md`
- `source/timewarp-nuru-devcli/readme.md`
- `source/timewarp-nuru-devcli/content/any/services/ci-run-promotion.cs`
  (Design comment only)

### Key decisions / deviations

- Upload `if` matches ganda (task 226/277): `success()` + master ref +
  skip release/probe dispatch. Nuru path remains `*.nupkg` (not
  Skills-only).
- Prune keys off `steps.upload-packages.outcome == 'success'` so it
  never runs when upload was skipped or failed.
- No 458-002 promote-contract change. Missing/pruned blob recovery
  remains `gh run rerun <run-id>`.

### Test outcomes

- No `if: always()` left in `workflow.yml`.
- Exact prune `jq` filter against live
  `TimeWarpEngineering/timewarp-nuru` artifacts: one non-expired
  `Packages-110` (id `9929408893`). Repo `total_count` is 1 (operator
  prune already landed).
- Python fixture: keep `Packages-12` + `Packages-11`, delete
  `Packages-10`; ignore `github-pages`, `Executables-*`, and expired
  `Packages-9`. Unlisted expected name skips prune.
- PyYAML parse of `workflow.yml` succeeded.

### Review disposition

- **Outcome:** `clean`
- **Effort / roster:** 1, general only
- **Rounds:** 1 (`review/round-1/`)
- **Final counts:** 0 open / 0 fixed / 0 wontfix across bug, suggestion, nit
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`
- No sibling apply-review task; no fix loop (zero findings).

### How to validate

**Smoke**

```bash
# From this worktree
grep -n 'if: always()' .github/workflows/workflow.yml
grep -n 'success()' .github/workflows/workflow.yml
grep -n "github.ref == 'refs/heads/master'" .github/workflows/workflow.yml
grep -n 'retention-days: 7' .github/workflows/workflow.yml
grep -n 'if-no-files-found: error' .github/workflows/workflow.yml
grep -n 'actions: write' .github/workflows/workflow.yml
grep -n 'Keep last two Packages artifacts' .github/workflows/workflow.yml
grep -n 'path: artifacts/packages/\*.nupkg' .github/workflows/workflow.yml
grep -A8 'Artifact retention' documentation/developer/guides/releasing.md

# Read-only live list (does not delete)
gh api --paginate \
  -H "Accept: application/vnd.github+json" \
  "/repos/TimeWarpEngineering/timewarp-nuru/actions/artifacts?per_page=100" \
  --jq '.artifacts[] | select((.name | startswith("Packages-")) and (.expired == false)) | [.created_at, (.id | tostring), .name] | @tsv'
```

**Expect**

- `if: always()`: no matches.
- Upload `if` includes `success()` and `refs/heads/master`; probe and
  release-mode dispatch are skipped.
- `retention-days: 7`, `if-no-files-found: error`, `actions: write`,
  keep-last-two step present, path still `artifacts/packages/*.nupkg`.
- releasing.md appendix says 7-day retention + keep last two, not
  “no retention-days / 90 days”.
- Live list today: one row, `Packages-110`. After this branch merges,
  the next green **master** push uploads `Packages-{run_number}` and
  prune keeps at most two `Packages-*` names. PR / failed / probe /
  release-mode runs do not upload.

**Automated gate**

None for YAML. The greps above are the gate. Do not expect a `dotnet
test` surface.

**Not in scope:** org retention UI; ganda 277 / amuru 113 YAML; packing
at release time; deleting live artifacts from this implementer session.
