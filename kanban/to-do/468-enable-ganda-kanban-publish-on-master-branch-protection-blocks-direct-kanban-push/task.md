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

Nuru is the outlier among the repos we compared: **Include administrators** is on, so even an
org admin cannot push `kanban/**` to `master`. Architecture still "requires a PR" on paper but
does not enforce it for admins, which is how publish succeeds there.

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

- [ ] Master protection no longer blocks admin/operator `kanban/**` push (`enforce_admins` false or equivalent bypass)
- [ ] `ci` still required on PRs
- [ ] `ganda kanban publish` succeeds on a kanban-only kitchen (paste CLI output in Notes)
- [ ] Other-repo scan table in Notes
- [ ] 467/468 no longer need kanban-only PRs as the default inbox path

## Session

- Created: ganda session 788123 (2026-09-02)
- Cockpit: grok `01a03d38-9611-7620-aae5-848e15dafa94` (timewarp-flow)
- Trigger: architecture 209 spawned nuru 467; `kanban publish` refused; PR #230 used instead

## Notes

This task’s own first landing may be a PR because publish is still blocked — that is expected.
The **result** is that later nuru to-dos use `ganda kanban publish`, not `gh pr create`.
