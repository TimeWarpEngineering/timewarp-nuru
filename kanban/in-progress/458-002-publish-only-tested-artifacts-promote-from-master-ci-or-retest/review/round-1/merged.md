# Round 1 — merged findings (reviewer: general-purpose/sonnet, agent ae1cd0fe0cd240e0a)

Diff: commit `02cbcf4f`. Reviewer live-verified: 100 real runs queried (zero
pr/push headSha collisions today — structurally prevented by merge-commit-only
+ enforce_admins), real PR-run logs proving merge-ref checkout, real gh auth
failure reproduced, real artifact download extraction layout (flat), gh api
placeholder syntax, RFC3339 timestamp format stability (20 samples), fixture
data verbatim-real, CI 1488/0 reproduced, gate order intact live.

| # | Sev | Finding | Status | Disposition |
|---|-----|---------|--------|-------------|
| 1 | MED | `OrderCandidateRuns` does not exclude `pull_request`-event runs; PR runs check out synthetic merge refs, so their artifacts are built from a DIFFERENT tree than their reported headSha. Dormant today (merge-commit-only + protection prevent collisions) but reliant on unstated external repo config | fix | Exclude `event == "pull_request"` from candidacy outright (can never represent "this exact commit, tested"); design-comment the reasoning; add pr-event test case. |
| 2 | MED | `GhUnavailable` collapses every non-zero gh exit (network, rate-limit, bad token) into "install gh / gh auth login" — misleading on runners; gh's actionable stderr is discarded (sibling outcome records in the same file DO carry stderr) | fix | Add Detail to LocateRunOutcome; print stderr alongside guidance; message distinguishes launch-failure from gh-reported-error. |
| 3 | MED | Readme claims the 458-005 untagged double-break-glass residual is "fully closed" — overstated: two break-glass attempts from different commits under one unbumped, untagged version can still mix CI-tested-but-different-commit packages (each package now individually tested — genuine improvement — but cross-attempt commit consistency is unverified) | fix | Soften wording per reviewer's suggested formulation; keep the accurate improvement claim; point at 458-006 tag-first tooling for the full closure. |
| 4 | LOW | Upload step re-uploads the downloaded set on every release run — byte-identical (harmless) but pure storage/bandwidth waste and a redundant artifact per release | fix | Gate upload: skip when in release mode (release event or confirmed break-glass dispatch). |
| 5 | LOW/INFO | Failed download can leave partial state in artifacts/packages — self-heals (clear runs unconditionally next attempt) and cannot cause an unsafe push (throw aborts before push) | wontfix | Cosmetic only; recorded. Decider: orchestrator. |

Open after dispositions: 1–4 → fix (one batch); 5 → wontfix.
