# TimeWarp versioning + release convention

Status: **canonical** (458 review 2026-08-06; Layer 3 restated as attestation by
458-010). Other TimeWarp package-publishing repos copy this verbatim; deviations
require a written reason in that repo.

## The convention

1. **Version SSOT.** The repo's package version lives in exactly one place:
   `<Version>` in `source/Directory.Build.props`. No MinVer, no CI-injected
   versions, no per-project overrides.

2. **One version per repo.** All packable projects in a repo share the SSOT version
   and are released together (lockstep). Burned numbers on unchanged packages are
   accepted and harmless.

3. **A version bump is a normal PR.** Bumping `<Version>` declares "the next release
   will be X." Bumps may ride feature PRs or stand alone; no enforcement either way.
   `dev check-version` warns when source drifts more than one increment ahead of the
   last release (task 456) — a bump merged is not a release cut.

4. **Publishing happens only from a published GitHub Release.** The
   `release: published` event is the sole routine publish trigger.
   `workflow_dispatch` builds and tests; it publishes only via an explicit
   break-glass input (`mode: release` plus typed confirmation), and CI supplies
   NuGet credentials (OIDC trusted publishing) under exactly the same condition
   that enables release mode — never otherwise.

5. **The tag is derived, not typed.** Releases are cut with `dev release`, which
   reads the SSOT version, runs the release gate, and creates tag `v{Version}` and
   the GitHub Release on the master head commit. Humans type a version exactly
   once: in the props bump PR.

6. **Release gate (all hard-fail unless stated):**
   - tag == `v{Version}` from props (defense-in-depth even with derived tags);
   - tag commit is reachable from master;
   - version not already fully published — none published → proceed; all published
     → abort; partially published → proceed with a loud resume warning
     (`--skip-duplicate` makes the re-push idempotent);
   - distance warning (advisory, task 456) when source is >1 increment ahead.

7. **Published artifacts are tested artifacts — by identity, not by convention.**
   Preferred: **build-once / promote** — master CI builds, tests, and uploads the
   `.nupkg` set; the release job downloads the artifacts of the tag commit's green
   CI run and pushes those exact bytes (no rebuild). Requires required status
   checks on master. Fallback where promotion plumbing isn't warranted:
   rebuild in the release run but insert `verify/test` before pack → push.
   Never rebuild-and-push without testing that rebuild.

8. **No hand-maintained package lists.** Pack, push, and check-version derive the
   package set from MSBuild `IsPackable` (or one generated manifest). Adding a
   packable project automatically adds it to pack, push, and the gate.

9. **Prerelease exit is the maintainer's call; the machinery is indifferent.**
   `-beta.N` means one thing: the API may still break without a major bump. A line
   exits beta when the maintainer commits to its API — i.e., would pay for the
   next breaking change with a new major version. No count, age, or usage metric
   forces the transition, and no exit-criteria documentation is owed. The release
   pipeline and gates treat stable and prerelease versions identically. (The task
   456 distance warning covers the separate mechanical issue of bumps merged
   without releases cut.)

10. **Canonical doc.** Each repo documents its release process at
    `documentation/developer/guides/releasing.md` (or repo equivalent), pointing at
    this convention and listing only repo-specific deltas.

## Enforcement architecture (how consistency survives without daily policing)

Decided 2026-08-07 after the org audit (`repo-matrix.md`). Constraint: the org is
on GitHub Free — branch protection cannot be enforced on private repos, and
file-syncing workflows into repos was already tried and abandoned
(`sync-configurable-files`, now `.disabled` in state/quickbooks). Therefore:

1. **One reusable workflow** in the public `.github` repo (`workflow_call`);
   every repo carries only a fixed ~10-line caller. Org-wide CI changes are one
   edit in one repo. Private repos can call public reusable workflows on Free.
2. **DevCli is the enforcement point.** All gate logic (rules 6–7) runs inside
   release mode, identically on public and private repos, independent of GitHub
   plan. Branch protection / required status checks are defense-in-depth where
   available — never load-bearing.
3. **Attestation.** The audit — all checks and all
   fixers — stays in ganda (private). Ganda signs evidence that the audit
   passed; public CI only verifies the signature. Private key signs on operator
   machines; public key verifies anywhere. The tool never travels — only proofs
   do. A scheduled sweep and a public `dev audit-convention` subset were
   considered and rejected (458-010).

   - **Evidence:** Ed25519 signature over `(tree hash, check-set hash,
     timestamp)` stored in `refs/notes/ganda-audit`, keyed by **tree SHA**
     (a merge commit whose tree equals the PR head inherits the head's
     attestation). Frozen v1 note body:
     `{v:1, alg:"ed25519", tree, check_set, ts, key_id, sig}`.
   - **Signer:** `ganda repo attest` on operator machines (`post-merge` +
     `post-checkout` hooks). Nothing else on the machine signs. Private key:
     `~/.timewarp/ganda/keys/` — touch only via `ganda repo attest`.
   - **Verifier:** DevCli `dev workflow` Step 1 — fetch notes, compute the
     commit's tree hash, verify against the baked-in public key registry
     (`AttestationVerifier.KnownKeys`). No convention knowledge in CI.
   - **Policy:** `.timewarp/dev.jsonc` `attestation.mode` (`off` | `warn` |
     `require`); CLI `--attestation` overrides one run. Unset defaults:
     PR/merge → `warn`; release → `require` (TimeWarp-first hard gate).
   - **Waiver:** `"attestation": { "mode": "off" }` for dormant/sites/no-ganda
     consumers so they do not fail forever.
   - **Rotation (public half):** add the new `key_id` → hex to
     `AttestationVerifier.KnownKeys` (keep the old key during the grace
     window), bump DevCli, then ganda starts signing with the new key.
     `tw-audit-1` is the post-rotation (2026-08-08) production public key.
   - **PR prohibit:** required status check `ci` where GitHub Free allows it
     (public repos). Release-mode verify is the plan-independent hard gate.

## Shared implementation surface

Repos should share, not re-implement:

- `TimeWarp.Nuru.DevCli` content endpoints: `check-version` (gate semantics above),
  `workflow` (attestation verify + mode matrix below), `release` (tag/Release cutter).
- `.timewarp/dev.jsonc` for per-repo config (`attestation.mode`; optional
  `checkVersionConfig.packages` override — packable set is derived from
  `IsPackable`).
- Thin `workflow.yml`: checkout (fetch-depth 0 for tag history), setup, OIDC login
  gated on release condition, `dev workflow`.

### Event → mode matrix

| Event | Mode | Publishes |
|-------|------|-----------|
| `pull_request` | pr: attestation → clean → build → verify → test | no |
| `push` (master) | merge: attestation → clean → build → verify → test | no |
| `release: published` | release: full gate + pipeline (rule 7), attestation hard by default | yes |
| `workflow_dispatch` (default) | merge | no |
| `workflow_dispatch` (`mode: release` + confirm) | release | yes (break-glass) |
| local (no `GITHUB_EVENT_NAME`) | pr | no |
