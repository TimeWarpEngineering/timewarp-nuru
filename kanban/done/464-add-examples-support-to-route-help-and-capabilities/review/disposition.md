# Disposition — task 464

**Date:** 2026-08-13
**Outcome:** accepted-exceptions
**Rounds:** 3
**Final open count:** 0

## Summary

Single general reviewer (effort 1) over implementation commit a1ae675d. Five findings across
the loop: M1 (bug — fluent named-args silently swapped command/description), M2/M3 (nits —
empty-value inconsistencies between DSL paths and emitters), M4 (nit — pre-existing systemic
escaping gap), M5 (suggestion — M2's fix silently dropped non-literal commands). M1–M3 fixed in
6c7ac49a; M5 fixed in f8c3936b, which also uncovered and fixed a latent generator bug
(ExtractFromBuildCall discarded all collected diagnostics when an aborted Build() chain
produced no model — code compiled clean and failed only at runtime). Round 3 verified both
fixes empirically, including the app-extractor semantic change (Empty → Failure(diagnostics))
being safe for all consumers and incrementality. Final counts: bug 0 open / 1 fixed;
suggestion 0 open / 1 fixed; nit 0 open / 2 fixed / 1 wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit | Pre-existing systemic gap (EscapeForStringLiteral vs U+0085/U+2028/U+2029) across all description call sites, predates 464 — dispositioned to dedicated follow-up task 465 | orchestrator |

## Escalations

- None.
