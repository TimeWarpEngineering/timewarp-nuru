# Fix Grouped Short Option Over Matching

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M11).

## Resolution

**Resolved by 454-005** (same decision, same commit): the grouping heuristic in
`source/timewarp-nuru-parsing/parsing/runtime/matchers/option-matcher.cs` was REMOVED
rather than fixed. It was undocumented, matched the short char anywhere in the arg
(`-e` matched `-help`), was gated to single-char shorts, and conflicted with the
now-supported multi-char single-dash options. Matching is exact string equality against
declared forms, mirroring the source-generated path. The per-call `ToString()`
allocation went away with the heuristic.

Regression coverage: `tests/timewarp-nuru-tests/parser/parser-16-multi-char-short-options.cs`
(`Should_match_short_forms_exactly_not_grouped`, `Should_not_cross_match_multi_char_and_single_char_shorts`).

If POSIX-style bundling is ever wanted, design it as an opt-in feature with validator
support — see the Design region in option-matcher.cs.

## Session

- Created: 2026-07-06 (full-repo review session)
- Resolved: 2026-07-06 (folded into 454-005)
