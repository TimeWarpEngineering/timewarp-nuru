# dev release: add a local attestation guard on the release target before tagging

## Description

`dev release` evaluates its guards locally (gh auth, clean tree, on master, in sync, tag available, version not
published, green CI at the target) and then tags. The ganda-audit attestation is only verified later, inside the
`release:published` job (`tools/dev-cli/endpoints/workflow-command.attestation.cs`, `VerifyAttestationAsync`,
mode `require` on release). When the target commit's tree has no note, the job aborts after the tag and the
GitHub Release already exist. A tag cannot be retried, so recovery is manual: attest the tree from a scratch
branch tracking origin/master, push notes, `gh run rerun --failed`.

Hit three times:
- 4.0.0-beta.13 (task 582): release run 43492390251 aborted on merge-commit tree e479fb9.
- 4.0.0-beta.14 (task 589): caught before tagging only because the operator checked the note by hand.
- 4.0.0-beta.15 (2026-11-16): the target was merge commit 8a002f0 (PR #418) with no note; the operator attested it from a
  scratch worktree before tagging.

**How the tree came to be unattested (589, verified):**

| commit | tree | note at commit time |
|---|---|---|
| 2508adaa bump (branch) | 1f07bf7e | yes |
| e4a249cf hand `git mv` of the kitchen to in-progress (branch tip) | 977a8865 | no |
| af9674df merge commit on master (release target) | 977a8865 | no (same tree as the tip) |

The merge tree equals the branch-tip tree, and the tip was a hand kitchen commit whose post-commit attest did not
produce a note. The note for 977a8865 was only written at 03:11 by the manual scratch attest. Not established:
why that one post-commit attest produced no note (hook output was not kept; a plausible cause is the audit
refusing a kitchen in `in-progress` while ganda's phase was still `claimed`). The guard below is correct whatever
the cause: any unattested target must refuse before the tag.

## Requirements

- New guard in `dev release` (shared DevCli content, `source/timewarp-nuru-devcli/content/any/endpoints/release-command.guards.cs`
  and `source/timewarp-nuru-devcli/content/any/services/release-guard.cs`), evaluated after the release target is selected and in `--dry-run` too:
  fetch `refs/notes/ganda-audit`, read the note for `<target>^{tree}`, run `AttestationVerifier.Evaluate` and the
  signature check, and honour the repo's attestation mode through `AttestationConfig` (`off` skips, `warn` prints,
  `require` refuses). Use the same effective mode the release job will use, so local and CI agree.
- The verify orchestration currently lives only in the repo-local `tools/dev-cli/endpoints/workflow-command.attestation.cs`.
  Move it (or its process-running half) into DevCli package content so `dev release` and `dev workflow` share one
  implementation. The pure verifier and config are already in the package.
- Pass line: `✓ target tree <sha12> attested (key_id=…)`. Refusal names the target commit, its tree, the status
  (`NoNote`, `RefMissing`, bad signature, unknown key), and the remedy in one block:
  pull master and run `ganda repo attest` when HEAD is the target; otherwise a branch at the target sha tracking
  origin/master, `ganda repo attest`, delete the branch.
- Add the refusal and remedy to the guard table in `documentation/developer/guides/releasing.md`.
- Tests beside the existing release-guard tests: attested target passes; missing note refuses under `require`,
  warns under `warn`, is skipped under `off`; a note for HEAD's tree does not satisfy a walked-back target.

## Goal (implementer restatement)

`dev release` must refuse **before it tags** when the release target's tree has no valid ganda-audit
note, instead of letting the `release: published` job abort after the tag and GitHub Release exist.

Acceptance:
- One shared verify implementation (fetch `refs/notes/ganda-audit`, read the note for a given commit's
  tree, `AttestationVerifier.Evaluate`, openssl signature check) lives in DevCli package content; both
  `dev workflow` (repo-local) and `dev release` call it.
- `dev release` runs a new guard on the selected release target (after step 8, also in `--dry-run`),
  with the mode the release job uses: `.timewarp/dev.jsonc` `attestation.mode`, unset → `require`
  (`off` skips, `warn` prints an advisory and continues, `require` refuses; a typo is `require`).
- Pass prints `✓ target tree <sha12> attested (key_id=…)`. Refusal names the target commit, its tree,
  the status, and the remedy (HEAD is target → pull master + `ganda repo attest`; walked-back target →
  branch at target sha tracking origin/master, `ganda repo attest`, delete the branch).
- releasing.md guard list gains the new guard with its refusal and remedy.
- Tests: attested target passes; missing note refuses (require), warns (warn), skipped (off); a note on
  HEAD's tree does not satisfy a walked-back target.

## Checklist

- [x] Share the attestation verify orchestration between `dev workflow` and `dev release`
- [x] Guard evaluated on the selected target in `dev release` and `--dry-run`, honouring attestation mode
- [x] Refusal text with status and remedy; pass line
- [x] releasing.md guard table updated
- [x] Tests for pass, refuse, warn, off, and walked-back target
- [x] Results with a dry-run transcript against an unattested fixture tree

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-15)
- 2026-11-17 groom: `release-guard.cs` path corrected to `content/any/services/`; added the beta.15 occurrence.
- 2026-11-18 implement (ganda task work, implementer-claude): shared verify runner, release guard step 9, docs, tests.

## Notes

Ships in TimeWarp.Nuru.DevCli, so every TimeWarp repo that releases with `dev release` gets the guard on its next
DevCli bump. The tw-release skill's "Attestation failure" pipeline-refusal row can then point at the local guard.

## Results

### What changed

- `source/timewarp-nuru-devcli/content/any/services/attestation-verify-runner.cs` (new):
  `AttestationVerifyRunner.VerifyAsync(repoRoot, commitish, keyOverride?, ct)` and `Describe(outcome)`,
  moved out of `tools/dev-cli/endpoints/workflow-command.attestation.cs`. It resolves `<commitish>^{tree}`
  instead of hard-wiring `HEAD`. `AttestationVerifyOutcome` moved with it and now carries `KeyId`.
  `dev workflow` (`workflow-command.verification.cs`) calls it with `"HEAD"`; behaviour and messages unchanged.
- `source/timewarp-nuru-devcli/content/any/services/release-attestation-guard.cs` (new): pure
  `ReleaseAttestationGuard.ResolveReleaseMode` (config `attestation.mode`, unset/typo → require, same as the
  release job, which passes no `--attestation`) and `Check(mode, targetSha, headSha, outcome)` → Skipped /
  Pass / Warn / Refuse with the pass line or the refusal block (commit, tree, status, detail, remedy).
- `release-command.cs` / `release-command.guards.cs`: new step 9 `CheckTargetAttestedAsync` after the
  release-target walk and before the `--dry-run` stop; dry-run and execute renumbered 10 and 11.
- `documentation/developer/guides/releasing.md`: nine guards; guard 9 with pass line, refusal, status list
  and remedy table; Step 1/6 attestation bullet points at guard 9. DevCli `readme.md` services table updated.
- Tests: new `tests/timewarp-nuru-tests/devcli/release-03-target-attestation.cs` (scratch git repo, note
  signed by a throwaway openssl key through `keyOverride`); compile includes added to
  `tests/timewarp-nuru-tests/devcli/Directory.Build.props` and `tests/ci-tests/Directory.Build.props`.
  `attestation-02-openssl-verify.cs` comment references updated.

### Gates

- `./bin/dev build` — passed (0 warnings, 0 errors). `dev self-install` run once after the `tools/dev-cli/`
  change; it compiles all DevCli content and succeeded.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/release-03-target-attestation.cs` — 7/7 passed.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/attestation-02-openssl-verify.cs` — 1/1 passed (comment-only edit).
- `ganda repo audit` — failed first on `package-pin-currency` (pre-existing: TimeWarp.Amuru 3.0.0-beta.9 behind
  3.0.0-beta.10) and `vscode-window-icon`. `ganda repo audit --fix` bumped TimeWarp.Amuru and TimeWarp.Amuru.Tools
  to 3.0.0-beta.10 in `Directory.Packages.props` (boy-scout) and set the gitignored `.vscode` title; the rerun inside
  `--fix` passed (45 passed, 0 failed, dev-cli self-install compiled). After the bump, `./bin/dev build` and
  `release-03-target-attestation.cs` (7/7) passed again.

### Dry-run transcripts

Fixture: scratch bare `origin` holding only `origin/master` (c1980c15, no tags, no notes ref), cloned to a
work dir; `GH_REPO=TimeWarpEngineering/timewarp-nuru` so gh reads real CI runs; `.timewarp/dev.jsonc` marked
skip-worktree and pointed at an unpublished package id so guard 7 passes (4.0.0-beta.16 is already on NuGet).

Unattested target (no notes ref on the fixture origin):

```text
✓ gh authenticated
✓ working tree clean
✓ on master
✓ in sync with origin/master
✓ tag v4.0.0-beta.16 available
✓ version 4.0.0-beta.16 not yet published
✓ HEAD is 2 kanban-only commit(s) past 34e82fa (CI run 44252432388)
Error: release target 34e82fa7e0d5 is not attested (RefMissing) — refusing before the tag.
  commit: 34e82fa7e0d5c9c0c7141d0cf99c0003fbc8697a
  tree:   b8a1894ff6952072af3e328329fb832ba95f4437
  status: RefMissing
  The release: published job verifies this tree with attestation.mode=require and would abort after the tag and GitHub Release exist.
  Remedy: the target is behind HEAD, so HEAD's tree is not the target's tree. Attest the target from a scratch branch:
    git switch -c attest-34e82fa7e0d5 34e82fa7e0d5c9c0c7141d0cf99c0003fbc8697a
    git branch --set-upstream-to=origin/master
    ganda repo attest
    git switch master && git branch -D attest-34e82fa7e0d5
    dev release
exit=1
```

Same fixture after copying GitHub's `refs/notes/ganda-audit` into the fixture origin (production key):

```text
✓ HEAD is 2 kanban-only commit(s) past 34e82fa (CI run 44252432388)
✓ target tree b8a1894ff695 attested (key_id=tw-audit-1)

Dry run — no changes made.
...
All guards passed.
exit=0
```

### How to validate

Smoke:

```bash
./bin/dev build
dotnet run --file tools/dev-cli/dev.cs -- self-install
dotnet run --file tests/timewarp-nuru-tests/devcli/release-03-target-attestation.cs
```

Expect: build succeeds; self-install compiles the DevCli content with the new runner and guard; 7/7 tests
pass (attested target passes with `key_id`; missing note refuses under require with the HEAD remedy; warns
under warn; off is skipped; a note on HEAD's tree leaves a walked-back target `NoNote` with the scratch-branch
remedy; unknown key names the DevCli update; release-mode resolution defaults to require).

Manual: on a clean, synced master, `./bin/dev release --dry-run` prints
`✓ target tree <sha12> attested (key_id=tw-audit-1)` before `Dry run — no changes made.`; against a clone whose
origin has no `refs/notes/ganda-audit`, it exits 1 with the refusal block above.
