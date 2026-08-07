# Disposition — 458-004

**Outcome: accepted-exceptions**

- Rounds: 2 (find → fix+verify). Roster: 1 × general-purpose (sonnet)
  adversarial reviewer (ae1cd0fe0cd240e0a); implementer/fixer
  aecc74bb408b79301; effort 1.
- Findings: 7 — 1 HIGH composite (brittle first-brace JSON anchor
  [demonstrated live], silent blank-PackageId drop, no under-count signal) →
  resolved (Properties-anchored backward brace-walk with both-output-shape +
  brace-in-noise tests; fail-loud throw on blank ID with a non-SDK fixture —
  the only real reproduction, since NuGet pack targets backfill PackageId
  from AssemblyName, independently verified; duplicate-ID guard
  ValidateDerivedSet; "Packable set (N)" membership printed on every release
  run); 3 LOW resolved (config-override nudge, derivation before Clean/Build,
  readme collision notes for all four new public types); 1 LOW/INFO resolved
  (readme); 2 INFO wontfix.
- **Accepted exceptions:**
  1. obj/bin exclusion is case-sensitive — default MSBuild conventions;
     defensive code; no such csproj exists (wontfix).
  2. Derivation runs twice per release run (check-version internal +
     pipeline) — ~2s, nothing mutates between (wontfix).
  3. Round-2 residual (LOW/INFO, contrived): noise containing the literal
     quoted string `"Properties"` with a preceding brace, before the real
     payload, can still mis-anchor the parse. Dramatically narrower than the
     closed vector; untested; accept. Full closure option if ever needed:
     try-parse each `"Properties"` occurrence until success.
  4. Adjacent-list note: BuildCommand's curated build list remains hardcoded
     (out of task scope — pack/push/check-version were the mandate); becomes
     moot under 458-002 promotion or is unified then.
- Commits: implementation `789cadd1`, round-1 fixes `bdf03e6a`.
- CI: 1468 total / 1461 passed / 7 skipped / 0 failed (reviewer reproduced
  independently both rounds).
