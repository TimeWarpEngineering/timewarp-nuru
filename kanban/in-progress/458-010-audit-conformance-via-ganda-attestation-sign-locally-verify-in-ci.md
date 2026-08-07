# Audit conformance via ganda attestation: sign locally, verify in CI

Parent: 458 (Layer 3 of the enforcement architecture in `review/repo-matrix.md`).

## Description

Ganda is private by design and stays that way; public repos' CI can never run
it. Git hooks alone are weak (skippable, uninstalled on fresh clones, invisible
to CI). A scheduled sweep only observes — it prohibits nothing. And splitting
the audit into a public `dev audit-convention` subset fragments one convention
across two tools.

**Decision (operator + review, 2026-08-07): attestation.** The audit — all
checks and all fixers — stays in ganda, private, one convention in one home.
Ganda *signs evidence* that the audit passed; public CI only *verifies* the
signature. Private key signs on operator machines; public key verifies
anywhere. The tool never travels — only proofs do.

### Design

- **Attestation** = signature over `(git tree hash, ganda audit
  version/check-set hash, timestamp)`, stored in `refs/notes/ganda-audit`
  (git notes attach to existing commits without rewriting history — required
  because GitHub creates merge commits server-side).
- **Tree-hash keying** makes merge-commit workflow (no squash, no rebase —
  operator preference) the friendly case: a merge commit whose tree equals the
  PR head's tree inherits the head's attestation for free. Only
  concurrent-merge trees (master moved under the PR) are novel.
- **Signers: operator machines only.** Ganda installs `post-merge` +
  `post-checkout` hooks (there is no native pull hook; `git pull` = fetch +
  merge). On pull/checkout: tree unattested → run audit → sign → push note.
  Novel merge trees on master get attested passively on the next pull. Ganda's
  own sync commands re-check belt-and-suspenders. Audit-and-sign is one atomic
  ganda operation — nothing else on the machine signs.
- **Verifiers: CI only, via a small DevCli step** (no convention knowledge):
  `git fetch origin 'refs/notes/*:refs/notes/*'`, compute the commit's tree
  hash, verify the note's signature against the committed/org public key.
  Runs in PR mode ("requires attestation" check) and in release mode as a
  **hard gate** on the tag's tree — the runner never signs; a missing
  attestation fails with "pull master so ganda can attest."
- **Green-flip loop:** pushing a note does not trigger workflows. After
  attesting, ganda closes the loop itself — posts a commit status (context
  `ganda/attestation`) on the SHA and/or re-runs the failed check via
  `gh run rerun`. The status is the fast UX signal; the signed note is the
  tamper-proof record the release gate trusts.
- **Outside contributors:** their PRs cannot produce attestations. The PR
  stays red until the operator pulls the branch locally — ganda audits and
  attests during review. Running ganda on incoming code becomes part of
  review, which it should be anyway.
- **Trust model, stated honestly:** the trust root is the operator's machines.
  "Attested" means "passed ganda's audit on an operator-controlled machine" —
  it defends against forgot-to-run and agent-claimed-it-ran, not against
  compromise of the operator's own machines (out of scope). Agents share
  those machines; the mitigation is that ganda is the only signing path.
- **Staleness:** the attestation embeds the audit version/check-set hash, so
  policy can decide whether attestations from older check-sets still count.

### Enforcement matrix

| Point | Mechanism | Prohibits? |
|-------|-----------|------------|
| Commit (local) | hook: audit + sign | yes, at source (operator machines) |
| PR | CI verify step / `ganda/attestation` status | where required status checks exist (public repos on Free plan); loud-advisory on private |
| Release | verify step in release mode, tag tree | **always — universal, plan-independent** |

One-off remediation still precedes turn-on: operator runs
`ganda repo audit --fix` across all active repos so gates start green
(baseline: `review/audit-results-2026-08-07.json`).

### Considered and rejected (decision history — do not re-litigate)

1. **Run ganda in public CI** (App token + private artifact): feasible;
   rejected — imposes the stability contract ganda's privacy exists to avoid
   (20 repos' CI breaking on any ganda change) and puts the private binary on
   every public runner one compromised action away from exfiltration.
2. **Split audit: public `dev audit-convention` subset + private ganda
   superset**: rejected by operator — no duplication but real fragmentation;
   one convention split across two tools and two repos.
3. **Move all audit checks public**: rejected by operator — the convention
   stays private with ganda.
4. **Scheduled sweep as enforcement**: rejected — a sweep observes and
   prohibits nothing. Optional detection-only sweep may still be worth keeping
   for out-of-band state no in-repo gate can see (branch protection settings,
   NuGet TP policies, non-adopted repos); explicitly decide keep-or-drop below.

## Checklist

### Ganda (private side — signer)

> Tracked in the ganda repo as **timewarp-ganda kanban 199** — **DONE**
> (2026-08-08, ganda commits 440235a/27602af/cb43967, clean review
> disposition; shipped in ganda v1.0.0-beta.23). CLI surface:
> `ganda repo attest` (audit+sign, `--force/--no-push/--no-status`),
> `repo attest keygen` (Ed25519), `repo attest key-show`,
> `repo attest status`, `ganda hooks install attest`.
>
> **Frozen verifier contract (v1) as implemented — the DevCli verifier must
> match this exactly:**
> - Notes ref `refs/notes/ganda-audit`, keyed by **tree SHA** (note attaches
>   to the tree object, not the commit).
> - Note body: compact JSON `{v:1, alg:"ed25519", tree, check_set, ts,
>   key_id, sig}` (field names frozen).
> - Signed bytes: UTF-8 of `v1\ned25519\n{tree}\n{check_set}\n{ts}\n{key_id}`
>   — NO trailing newline after key_id.
> - `ts`: ISO-8601 UTC, second precision, trailing Z.
> - Private key: `~/.timewarp/ganda/keys/audit-ed25519.pem` (operator
>   machines); public material via `ganda repo attest key-show`.
> - Green-flip commit-status context: `ganda/attestation`.
> - `check_set`: hash of the audit check-set (staleness policy input).
> - Branch policy AS IMPLEMENTED: branches tracking a remote are allowed
>   (observed: dev branch → "allow — tracks origin/dev") — broader than the
>   recorded master+pulled-only decision; operator's implementation call,
>   recorded here for the verifier's staleness/policy design.

- [ ] Attestation format: signature over (tree hash, audit version/check-set hash, timestamp); note in `refs/notes/ganda-audit`
- [ ] Key management: private key on operator machines, ganda-only signing path; public key published (repo- or org-level); rotation procedure written down
- [ ] `post-merge` + `post-checkout` hook install (via ganda) — audit + sign + push note when tree unattested
- [ ] Green-flip: post `ganda/attestation` commit status and/or `gh run rerun` after attesting
- [x] Attest-branch policy — decided 2026-08-08 (operator): **master + explicitly pulled PR branches only**; hooks do not attest arbitrary checkouts

### DevCli / reusable workflow (public side — verifier)

- [ ] Verify step: fetch notes, compute tree hash, verify signature with public key — no convention knowledge
- [ ] Wire into PR mode ("requires attestation") and release mode (hard gate on tag tree)
- [ ] Required status checks on public repos so the PR check prohibits (same enabler as 458-002 Design B)
- [ ] Clear failure text: which tree, why unattested, "pull master so ganda can attest"

### Rollout

- [ ] Clone the 3 publisher repos missing locally (needed for remediation): `ganda repo clone git@github.com:TimeWarpEngineering/timewarp-fixie.git`, `…/timewarp-quickbooks.git`, `…/timewarp-build-tasks.git` (verified vs `ganda repo list` 2026-08-08 — all other 18 publishers already cloned)
- [ ] One-off remediation: `ganda repo audit --fix` across all active repos; record before/after here
- [ ] Waiver mechanism for repos where the attestation requirement is N/A (dormant/sites), so they don't red forever
- [x] Sweep — decided 2026-08-08 (operator): **dropped**. No detection-only sweep; revisit only if out-of-band drift (branch protection, lapsed TP policies) actually bites.
- [x] TimeWarp.Ganda public NuGet — decided 2026-08-08 (operator): **stop publishing**. Install from private repo on operator machines; remove ganda from the 458-009 TP roster and delete its existing TP policy. (Existing public versions up to beta.15 remain on nuget.org — NuGet does not truly delete; unlist them.)
- [ ] Update `review/convention.md` Layer 3 wording to the attestation model when this lands

## Notes

Implementation spans ganda (signer, hooks, status posting) and DevCli + the
`.github` reusable workflow (verifier); tracked here because 458 owns the org
consistency program. This model answers "how do you use a private tool with
public repos": you don't run the tool in public — it emits verifiable evidence
and the public side verifies evidence. Checks and fixers never leave ganda.
