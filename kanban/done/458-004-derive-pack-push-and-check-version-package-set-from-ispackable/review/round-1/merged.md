# Round 1 — merged findings (reviewer: general-purpose/sonnet, agent ae1cd0fe0cd240e0a)

Diff: commit `789cadd1`. Reviewer verified live: msbuild output shapes, error
surfacing (stderr preserved), AOT wiring (probed reflection-serializer is
disabled in env; source-gen path exercised), fixture non-tautology (subprocess
timing + real AssemblyName-derived ID), search-pattern literalness (`+` safe),
ctor ripples complete, CI 1454/0 reproduced.

| # | Sev | Finding | Status | Disposition |
|---|-----|---------|--------|-------------|
| 1 | HIGH (composite) | (a) `ParseGetPropertyOutput` anchors on first `{` — noise containing a brace before the JSON silently yields (false,null) [demonstrated]; (b) IsPackable=true with blank PackageId is silently dropped, indistinguishable from not-packable; (c) no under-count oracle — a silently dropped project ships an incomplete release with "SUCCEEDED" (extras cross-check only catches over-count, and only when the project still builds) | fix | (a) anchor on `{"Properties"`; noise-with-brace test. (b) throw naming the project (fail loud — packable without ID is a config error); fixture coverage. (c) print the derived set in the release pipeline; duplicate-PackageId guard (throw); note BuildCommand's curated list as adjacent-list follow-up (out of task scope, 458-002 territory). |
| 2 | LOW | Config-override path gives consumers zero signal that derivation exists — the population still hand-maintaining lists never learns to stop | fix | One nudge line when config override is used. |
| 3 | LOW | Abort-if-empty derivation runs at Step 5, after Clean+Build wasted | fix | Derive + abort-if-empty right after check-version; pass the set to pack/push; print membership. |
| 4 | LOW/INFO | Readme collision note not extended to `PackableProject`/`MsBuildEvaluationOutput` (latter genuinely generic name) | fix | One line, TagAssertion-precedent style. |
| 5 | INFO | Duplicate PackageId across projects would collapse in the HashSet / overwrite nupkg | fix | Cheap guard folded into #1c (throw on duplicate IDs after derivation). |
| 6 | INFO | obj/bin exclusion is case-sensitive (custom-cased output paths not excluded) | wontfix | Default MSBuild conventions are lowercase; no such csproj exists in repo; defensive code. Decider: orchestrator. |
| 7 | INFO | Derivation runs twice per release (check-version internal + pack/push) — incidental, unpinned invariant | wontfix | ~2s cost, nothing mutates between; enforced identity would couple the steps for no failure mode found. Recorded here. |

Open after dispositions: 1–5 → fix (one batch); 6–7 → wontfix.
