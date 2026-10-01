# Refresh githooks pre-push to the ganda baseline (task branch to home guard)

## Description

Refresh this repo's `.githooks/pre-push.cs` (and the `.githooks/pre-push` shim if the baseline
changed it) to the current ganda repo baseline.

Ganda task 323 (timewarp-ganda PR #197, merged 2026-10-01) added a guard to the baseline
pre-push hook. It refuses a push whose local ref is `refs/heads/task/*` and whose destination
is the home branch (`master` / `main`), and the message names the task branch. Raw-sha pushes,
such as the `kanban publish` merge commit, stay allowed. Until each repo picks up the new hook,
`ganda repo audit` warns `memsearch-scaffold: .githooks/pre-push.cs (outdated)`, and the repo
lacks the guard.

## Requirements

1. Apply the baseline with ganda itself, not by hand:
   `ganda repo audit --fix --checks memsearch-scaffold`. Confirm that `.githooks/pre-push.cs`
   now matches the baseline and the warning is gone.
2. Keep any repo-specific hook content the baseline intends to preserve. If `--fix` would drop a
   local customization, stop and record it in Notes rather than overwrite it.
3. Re-run `ganda repo audit` and fix anything else it reports (boyscout welcome), then commit.
4. Smoke-test the hook without touching home:
   - Pipe a fake pre-push line into the hook,
     `refs/heads/task/x <sha> refs/heads/master <sha>`, and confirm it is refused.
   - Pipe `<sha> <sha> refs/heads/master <sha>` and confirm it is allowed.

   Do not push to master to test.

## Checklist

- [x] `.githooks/pre-push.cs` refreshed via `ganda repo audit --fix --checks memsearch-scaffold`
- [x] Audit clean (no `memsearch-scaffold` warning)
- [x] Hook smoke test: task→home refused, raw sha→home allowed (stdin simulation only)
- [x] Gates per this repo's `tw-pr` (a hook-only change needs no full build unless the skill's
      scope table says otherwise)
- [ ] Implementation review; host `open-pr`

## Notes

- One of a set of identical tasks filed in each repo that carries `.githooks/pre-push.cs`:
  amuru, architecture, bayline, ganda, kiini, mediator, nuru, state, taratibu.
- Do not start any app host. Run builds serially and call `dotnet build-server shutdown` before
  finishing.

## Results

- `ganda repo audit --fix --checks memsearch-scaffold` refreshed `.githooks/pre-push.cs`: a pure
  addition (+19 lines) of the task/* → home guard. No local customization was dropped. The
  `.githooks/pre-push` shim was unchanged.
- Boyscout: the audit also failed `bin-dev` / `dev-cli-capabilities` (bin/dev missing in this
  worktree). `ganda repo audit --fix --checks bin-dev` built it. `bin/` is gitignored, so there
  is no committed change.
- `ganda repo audit` result: "Repository passes all audit checks." No `memsearch-scaffold` warning.
- Gates: this is a hook-only change, so no full build was needed. `dotnet build-server shutdown` was run.

### How to validate

Smoke (from the task worktree, with S=$(git rev-parse HEAD)):

```bash
ganda repo audit
echo "refs/heads/task/x $S refs/heads/master $S" | ./.githooks/pre-push origin url; echo "exit=$?"
echo "$S $S refs/heads/master $S" | ./.githooks/pre-push origin url; echo "exit=$?"
```

Expect:

- The audit reports "Repository passes all audit checks." and no `memsearch-scaffold` warning.
- task→home is refused with exit=1 and prints
  `Refusing push of task branch to home: task/x -> master.` (recorded 2026-10-01).
- raw sha→home is allowed with exit=0 and no output (HEAD is a task branch).

## Session

- Created: 2026-10-01
- 2026-10-01: implement oracle (ganda task work) refreshed the hook, made the audit clean, and ran the smoke test.
