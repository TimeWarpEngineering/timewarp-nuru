# Ignore routine journals so worktree gc is not dirty

## Description

`ganda task work` writes `task-work.journal.json` beside the kitchen. Unless
root `.gitignore` lists that basename, `git status --porcelain` shows `??`
and `ganda pr merge` / `worktree gc` **refuses** a dirty worktree.

This is a **consumer sweep**. Ganda **262** added audit check
`routine-journals-gitignore` and `--fix`, then left “sweep every org repo”
out of scope. That was wrong: we have hit this on merge at least six times
(Taratibu 252/253/254, mediator 004-001/004-002, architecture 207/208,
timewarp-software **033**). Each origin that never ran `--fix` is another
dirty-gc.

This origin (`timewarp-nuru`) is missing the ignore. Org SSOT: `ganda repo audit`
check `routine-journals-gitignore`. `--fix` appends the missing basename
lines. Tracked journals are **Failed / not fixable** — `git rm --cached`
is required (gitignore does not hide tracked files).

Do **not** commit journal contents.

## Requirements

Root `.gitignore` must contain this glob (comments/blanks ok):

```
*.journal.json
```

One line covers every routine journal (`task-work`, stacked-task-set, planning,
rfc, debate, advisor, and the next one). Ganda **268** updates the audit check
to PASS on this glob; do not add the six 262 exact names.

Prefer `ganda repo audit --fix --checks routine-journals-gitignore` (this
CLI requires `--fix` when `--checks` is set) so the commented block matches
other origins:

```gitignore
# Routine journals beside kitchens (local; not product)
*.journal.json
```

Then:

- `git rm --cached` any `*.journal.json` that `git ls-files` still lists.
  Delete empty leftover dirs if they exist only because of the journal.
- Do **not** `git rm` product `task.md` files.
- `git ls-files '*.journal.json'` must be empty.
- Audit check `routine-journals-gitignore` PASSes.
- `git check-ignore -v` on a journal basename path hits the new line.

## Checklist

- [x] Root `.gitignore` has `*.journal.json`
- [x] `git ls-files '*.journal.json'` is empty
- [x] Audit `routine-journals-gitignore` PASSes
- [x] `git check-ignore -v` confirms ignore; porcelain does not list journals
- [x] Do not implement on `master`

## Notes

- Predecessor: ganda `kanban/done/262-audit-gitignore-for-task-work-journal-so-worktree-gc-is-not-dirty/`
- Consumer precedent: architecture **208**, timewarp-software **034**
- Host hole (ganda kitchen, separate): unstage **any** `kanban/**/*.journal.json`
  on kitchen commits; consider a hook that runs `repo audit --fix`.
- 262 out-of-scope (“do not sweep every org repo”) is why this kitchen exists.

### How to validate

**Automated**
```bash
git check-ignore -v kanban/to-do/task-work.journal.json || true
# expect: .gitignore:…:*.journal.json (path may be untracked)

git ls-files '*.journal.json'
# expect: empty

ganda repo audit --fix --checks routine-journals-gitignore
# expect: routine-journals-gitignore PASS (fix is a no-op once present)
```

**Not in scope:** changing `WorktreeGcService` to treat untracked journals as
clean; host unstage-all (ganda).

## Session

- Created: grok `01a06304-cbf6-7d83-b5a2-4a99e9d09d40` (2026-09-03) cockpit timewarp-flow
- Trigger: `/tw-merge` software 033 — GC refused, then leftover journal
  committed; 262 left consumer sweep out of scope
- Pattern: `*.journal.json` (cockpit, 2026-09-03) — one glob, not six names
- Implementer: grok (2026-09-04) `ganda task work` implement oracle on
  `task/469-ignore-routine-journals-so-worktree-gc-is-not-dirt`

## Results

Root `.gitignore` now ignores every routine journal with one glob. The leftover
tracked `kanban/to-do/task-work.journal.json` (committed from task 467’s done
move) was removed from the index with `git rm --cached`; the file remains on
disk as local host state.

**Files changed**
- `.gitignore` — appended the 268 block:
  `# Routine journals beside kitchens (local; not product)` + `*.journal.json`
- `kanban/to-do/task-work.journal.json` — untracked (`git rm --cached` only;
  contents not committed)

**Decisions / deviations**
- Used `ganda repo audit --fix --checks routine-journals-gitignore` so the
  comment matches other origins. Did **not** add the six 262 exact names.
- Did **not** add `.memsearch/memory/` (separate audit check, out of scope).
- Did **not** change `WorktreeGcService` or ganda host unstage-all.

**Test outcomes**
- `git ls-files '*.journal.json'` — empty
- `git check-ignore -v kanban/to-do/task-work.journal.json` —
  `.gitignore:451:*.journal.json`
- `ganda repo audit --fix --checks routine-journals-gitignore` —
  `routine-journals-gitignore` **PASS** (fix is a no-op once present)
- `git status --porcelain` does not list journals (`??` gone; staged `D` is
  the untrack). Other audit FAILs (`bin-dev`, `memsearch-*`) are pre-existing
  and out of scope.

### How to validate

**Smoke**
```bash
git check-ignore -v kanban/to-do/task-work.journal.json || true
git ls-files '*.journal.json'
git status --porcelain | grep -E 'journal\.json' || true
ganda repo audit --fix --checks routine-journals-gitignore
```

**Expect**
- `git check-ignore -v` prints `.gitignore:<line>:*.journal.json` for that path
  (file may be untracked / local-only).
- `git ls-files '*.journal.json'` prints nothing.
- Porcelain does not list `*.journal.json` (`??` or otherwise).
- Audit table: `routine-journals-gitignore` **PASS**. Fix result is a no-op
  (“already ignores”). Other checks (e.g. `bin-dev`, `memsearch-*`) may still
  fail; ignore those for this task.

**Automated gate**
Same four commands as Smoke. No product test suite for a gitignore line.

**Not in scope:** changing `WorktreeGcService` to treat untracked journals as
clean; host unstage-all (ganda); memsearch gitignore / `bin/dev` audit FAILs.
