# Round 1 — merged findings (reviewer: general-purpose/sonnet, agent ae1cd0fe0cd240e0a)

Diff: commit `f002e65a`. Reviewer independently re-ran both new test files, the
full CI suite (1424/0), decompiled Amuru.Tools to verify Git.FindRoot, and
disproved the `+`-in-tag hypothesis via `git check-ref-format` (build-metadata
tags are legal — recorded as verified non-issue). Gate abort paths, exit-code
propagation, removal completeness, serializer skip-unknown behavior,
TagAssertion matrix, packaging: clean.

| # | Sev | Finding | Status | Disposition |
|---|-----|---------|--------|-------------|
| 1 | MED | `IsHeadAncestorOfMasterAsync` swallows git errors: unresolvable master (shallow clone, missing remote) is indistinguishable from a real not-an-ancestor verdict; origin/master→master fallback is silent; stderr never surfaced. Fail-closed but misleading. | fix | Distinguish outcomes: log which master ref resolved; if neither resolves, print a distinct "cannot resolve master — ensure full history (fetch-depth: 0)" error; treat merge-base exit 1 as not-ancestor and >1 as git error with stderr surfaced. |
| 2 | LOW | `ReadPropsVersion` doesn't trim the `<Version>` value — stray whitespace in props makes every release abort with a confusing mismatch | fix | `.Trim()` the element value. |
| 3 | INFO | DevCli package now ships new public `TagAssertion` type; readme migration note could mention it (collision risk categorically lower than the CiMode case — no downstream local equivalent exists) | fix | One-line addition to the existing migration-notes subsection. |

Open after dispositions: 1–3 → fix (one batch). No wontfix this round.
