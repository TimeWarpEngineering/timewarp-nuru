# Round 1 — merged findings (reviewer: general-purpose/sonnet, agent ae1cd0fe0cd240e0a)

Diff: commit `30dce371`. Classifier semantics, exit codes, release-event
gate ordering, packaging/props, RCS1037: clean. Reviewer live-reproduced the
HIGH (not hypothesized).

| # | Sev | Finding | Status | Disposition |
|---|-----|---------|--------|-------------|
| 1 | HIGH | `--package ","` (or any delimiter-only list, e.g. a dev.jsonc template typo) passes the whitespace guard, splits to zero packages, and `Classify(0,0)` throws an unhandled ArgumentOutOfRangeException — CI crash with stack trace instead of the friendly "no packages specified" error; regression vs pre-30dce371 | fix | Guard on the parsed count: extract testable `ParsePackageList`, route count==0 to the existing friendly error (exit 1). Endpoint-level tests for delimiter-only inputs. |
| 2 | MED | Partial warning overstates `--skip-duplicate` safety: it only suppresses HTTP conflicts — it cannot verify already-published bytes match this run's source; resume from changed source ships mixed content under one version | fix | Honest wording: resume is byte-safe only from the same commit that produced the earlier partial push; enforced via #3 when the tag exists; otherwise bump. Update publish-state.cs design comment too. |
| 3 | MED | Break-glass path has no tag check, so a partial publish from commit A can be "resumed" from commit B (both on master) — 3 packages from A + 2 from B ship under one version with no gate complaint | fix | Add tag-pin gate step in release mode: if tag `v{propsVersion}` exists, HEAD must be at that tag's commit (4-state outcome: NoTag/Match/Mismatch/GitError; abort on Mismatch/GitError with precise message). Release events pass trivially (checkout is at tag); break-glass resume is forced onto the tag commit; fresh never-tagged break-glass (no tag) is unconstrained beyond ancestor check — acceptable, no split bytes exist yet. |
| 4 | LOW | No endpoint-level tests (three-state exit codes / messages / zero-package input) — the gap that let #1 ship | fix | Add what's cleanly testable: ParsePackageList unit tests + endpoint-level zero-package friendly-error test if Handler construction is feasible without HTTP; otherwise document the boundary. |
| 5 | INFO | Duplicate names in package list untested (traced not-a-bug: lists grow 1:1) | wontfix | Traced safe by reviewer; ParsePackageList tests cover dedup-adjacent shapes incidentally. Decider: orchestrator. |

Note: full byte-identity verification (published nupkg's embedded commit vs
HEAD) belongs to 458-002's build-once/promote design, which eliminates the
rebuild entirely; #3 is the correct rebuild-world closure.

Open after dispositions: 1–4 → fix (one batch); 5 → wontfix.
