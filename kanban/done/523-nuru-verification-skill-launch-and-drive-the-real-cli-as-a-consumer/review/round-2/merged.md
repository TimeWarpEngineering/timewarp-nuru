# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:485
- Description: The docs step missed the pre-3.0 two-argument `.Map(pattern, handler)`. Fixed on `1e767fd6..9b4a097a` and re-verified: those fences compile in a builder context, `CS1501` is broken, and `evidence/4.0.0-beta.5/snippets.tsv` fails all ten plus `use-cases.md` line 149.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Verified fixed in round 2. No regression on the host-extension carve-out for this release.

### M2 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/probes/discover-endpoints.cs:7
- Description: `DiscoverEndpoints()` had no runtime probe. Fixed and re-verified: `probes/discover-endpoints.cs` calls it on two `[NuruRoute]` classes with no `Map<T>()`, and both cases PASS on 4.0.0-beta.5. The generator includes those routes only when the call is interpreted.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Verified fixed in round 2.

### M3 — Severity: bug — Status: fixed
- File: skills/tw-nuru-verify/scripts/nuru-consumer-check.sh:123
- Description: A missing `script` or `nc` could SKIP and still exit 0. Fixed and re-verified: doctor refuses (exit 2), SKIP is gone, and `record` fails the run on any non-PASS.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Verified fixed in round 2.

### M4 — Severity: nit — Status: fixed
- File: skills/tw-nuru-verify/SKILL.md:11
- Description: Agent-authored `skills/tw-nuru-verify/SKILL.md` had no `why:` clause. Fixed and re-verified: `(why: task 523, Steven approved the consumer-use check 2026-10-23)`.
- Suggestion: none remaining.
- Source: general
- Disposition notes: Verified fixed in round 2.

## Duplicates / conflicts

- Single reviewer. Prior IDs carried forward. No new findings.
