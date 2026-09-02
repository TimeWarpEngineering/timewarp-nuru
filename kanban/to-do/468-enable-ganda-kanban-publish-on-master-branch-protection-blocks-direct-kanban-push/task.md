# Enable ganda kanban publish on master (branch protection blocks direct kanban push)

## Description

`ganda kanban publish` pushes **kanban/** only straight to origin-home. It has **no PR fallback**.
On this repo that push is refused, so kitchens cannot land as to-do inbox without a product-style
PR. Task **467** had to ship as kanban-only PR [#230](https://github.com/TimeWarpEngineering/timewarp-nuru/pull/230) for that reason. This kitchen will too, until this task is done.

Ganda policy (tw-kanban / `KanbanPublishService.FormatRequirePullRequestMessage`):

> Rulesets cannot allowlist `kanban/` while requiring a PR for other paths.
> Per-repo policy: turn off "Require a pull request before merging" **or** add a bypass
> actor that this identity can use.

There are **no GitHub rulesets** on this repo (`GET /repos/.../rulesets` → `[]`). The block is
**classic branch protection** on `master`.

## Evidence (2026-09-02, `gh api .../branches/master/protection`)

| Repo | `enforce_admins` | Require PR reviews | Required checks | `kanban publish` |
|---|---|---|---|---|
| **timewarp-nuru** | **true** (Include administrators) | yes, 0 approvals | `ci` | **blocked** |
| timewarp-architecture | false | yes, 0 approvals | none | works |
| timewarp-state | false | yes, 0 approvals | none | works |
| timewarp-amuru | unprotected | — | — | works |

Nuru **was** the outlier: **Include administrators** was on, so even an org admin could not
push `kanban/**` to `master`. Architecture still "requires a PR" on paper but does not enforce
it for admins, which is how publish succeeds there.

**Changed 2026-09-02:** `DELETE .../protection/enforce_admins` → `enforce_admins: false`.
Required PR reviews (0) and `ci` unchanged. Force-push and deletions still off.

## Requirements

GitHub **Settings → Branches → master** (operator; not a code change):

- Align with architecture/state so **this identity** can `git push` a `kanban/**`-only commit to
  `master` (the publish path).
- Recommended: set **Do not include administrators** (`enforce_admins: false`). Keep required
  PR reviews and the `ci` check for non-admin / PR traffic.
- Alternative: a bypass actor the operator identity already is (classic protection has no
  path allowlist for `kanban/`).
- Do **not** drop `ci` on PRs. Do **not** enable force-push or branch deletion.
- Do **not** invent a ganda PR-fallback for publish (explicitly refused in ganda 221).

Prove:

```bash
# from a claimed nuru kitchen with only kanban/** vs origin/master
ganda kanban publish 468   # or a throwaway id after 468 is already on the board
# expect: Published task … to origin/master  (not GH013 / "must be made through a pull request")
```

If 468 itself cannot be published until the setting changes, land this kitchen via PR (this
file), change protection, then the **next** kitchen uses publish. Record which path was used.

Scan other public TimeWarp **package** repos for `enforce_admins: true` + require-PR and list
them in Notes (fix those in follow-up tasks; this id is nuru).

## Checklist

- [x] Master protection no longer blocks admin/operator `kanban/**` push (`enforce_admins` false or equivalent bypass)
- [x] `ci` still required on PRs
- [ ] `ganda kanban publish` succeeds on a kanban-only kitchen (paste CLI output in Notes)
- [x] Other-repo scan table in Notes
- [ ] 467/468 no longer need kanban-only PRs as the default inbox path

## Session

- Created: ganda session 788123 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)
- Trigger: architecture 209 spawned nuru 467; `kanban publish` refused; PR #230 used instead

## Notes

First landing was PR [#231](https://github.com/TimeWarpEngineering/timewarp-nuru/pull/231) because
publish was still blocked. Operator then `DELETE`d `enforce_admins` (2026-09-02). This commit is
the proof publish: `ganda kanban publish 468` from this kitchen.

### Other-repo scan (public TimeWarpEngineering, master protection, 2026-09-02)

`enforce_admins: true` + require PR (same trap, follow-up not this id):

- **timewarp-terminal**

Already `enforce_admins: false` (publish-capable if the operator is admin):

- timewarp-nuru (after this change)
- timewarp-architecture
- timewarp-state
- timewarp-mediator (`pr: false`)

Unprotected / no master protection among the 24 public repos listed: amuru, jaribu, netclaw,
fluentui-blazor, and the rest of the `gh repo list` public set.

## Results

### What was implemented

Classic branch protection on `TimeWarpEngineering/timewarp-nuru` `master`: turned off
**Include administrators** (`gh api -X DELETE .../protection/enforce_admins`). Left required
PR reviews (0 approvals) and required status check `ci`. Did not enable force-push or deletions.
No product code.

### Files changed

- `kanban/to-do/468-…/task.md` (this file) — Notes / Results
- GitHub branch protection (not in git)

### Key decisions

- Match architecture/state (`enforce_admins: false`) rather than dropping require-PR or `ci`.
- No ganda PR-fallback (ganda 221).
- **timewarp-terminal** has the same admin-enforced require-PR; out of scope here.

### Test outcomes

- After DELETE: `enforce_admins: false`, `status: ["ci"]`, `allow_force: false`, `allow_deletions: false`.
- `ganda kanban publish 468` output recorded below after this commit.

### How to validate

**Smoke**

```bash
gh api repos/TimeWarpEngineering/timewarp-nuru/branches/master/protection --jq '.enforce_admins.enabled, .required_status_checks.contexts'
# expect: false
# expect: ["ci"]

# from a claimed nuru kitchen with only kanban/** vs origin/master:
ganda kanban publish <id>
# expect: Published task … to origin/master
```

**Expect**

- Admin/operator can push `kanban/**` to `master`.
- PRs still run `ci`.
- Product branches still go through PRs for non-admins.

**Not in scope:** timewarp-terminal protection; inventing a publish PR fallback.
