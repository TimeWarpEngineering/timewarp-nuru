# dev release: add a local attestation guard on the release target before tagging

## Description

`dev release` evaluates its guards locally (gh auth, clean tree, on master, in sync, tag available, version not
published, green CI at the target) and then tags. The ganda-audit attestation is only verified later, inside the
`release:published` job (`tools/dev-cli/endpoints/workflow-command.attestation.cs`, `VerifyAttestationAsync`,
mode `require` on release). When the target commit's tree has no note, the job aborts after the tag and the
GitHub Release already exist. A tag cannot be retried, so recovery is manual: attest the tree from a scratch
branch tracking origin/master, push notes, `gh run rerun --failed`.

Hit twice:
- 4.0.0-beta.13 (task 582): release run 43492390251 aborted on merge-commit tree e479fb9.
- 4.0.0-beta.14 (task 589): caught before tagging only because the operator checked the note by hand.

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
  and `release-guard.cs`), evaluated after the release target is selected and in `--dry-run` too:
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

## Checklist

- [ ] Share the attestation verify orchestration between `dev workflow` and `dev release`
- [ ] Guard evaluated on the selected target in `dev release` and `--dry-run`, honouring attestation mode
- [ ] Refusal text with status and remedy; pass line
- [ ] releasing.md guard table updated
- [ ] Tests for pass, refuse, warn, off, and walked-back target
- [ ] Results with a dry-run transcript against an unattested fixture tree

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-15)

## Notes

Ships in TimeWarp.Nuru.DevCli, so every TimeWarp repo that releases with `dev release` gets the guard on its next
DevCli bump. The tw-release skill's "Attestation failure" pipeline-refusal row can then point at the local guard.
