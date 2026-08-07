# Disposition — 458-002

**Outcome: accepted-exceptions**

- Rounds: 2 (find → fix+verify). Roster: 1 × general-purpose (sonnet)
  adversarial reviewer (ae1cd0fe0cd240e0a); implementer/fixer
  aacf5800ee1ad838d; effort 1.
- Findings: 5 — 3 MED resolved (pull_request runs excluded from promotion
  candidacy [reviewer proved merge-ref checkout from real CI logs and
  verified zero collisions across 100 real runs]; GhFailed/GhUnavailable
  split with stderr surfaced [live 401 repro]; readme residual-closure
  overclaim corrected to "improved, not closed" with 458-006 as full
  closure), 1 LOW resolved (release runs skip redundant artifact re-upload;
  condition verified across all 5 trigger shapes + YAML/GHA syntax), 1
  LOW/INFO wontfix.
- **Accepted exceptions:**
  1. Partial-download leftover state in artifacts/packages — self-heals
     (unconditional clear on next attempt), cannot cause an unsafe push
     (wontfix).
  2. Outstanding operator step (checklist, not a defect): the
     branch-protection PUT (required status check `ci`) must run AFTER this
     branch merges to master — exact command recorded in the task Notes/plan
     (D8 sequencing).
- Commits: implementation `02cbcf4f`, round-1 fixes `bd79b9bc`.
- CI: 1498 total / 1491 passed / 7 skipped / 0 failed (reviewer reproduced
  independently both rounds; gate order verified live after each round).
