# Resolve Multi Char Single Dash Option Support

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Decision

**Support multi-char single-dash options end-to-end; remove the POSIX grouping heuristic.**
(User approved 2026-07-06.)

Investigation showed this was an unfinished feature, not a deliberate rejection:
- Lexer support was added deliberately (commit d00fde0b, Oct 2025, TDD, motivated by
  `dotnet run -bl`) and documented with rationale in
  `documentation/developer/design/lexer/token-types.md`.
- The parser's single-char validation predates that work and was never updated.
- The source-GENERATED matcher already matched short forms by exact string equality —
  multi-char shorts worked there with zero changes.
- POSIX grouping existed only as an undocumented, buggy `Contains` heuristic in the
  runtime `OptionMatcher` (M11: `-e` matched `-help`). Grouping and multi-char shorts
  are mutually exclusive conventions; in a route-based framework where options are
  declared literally and matched exactly, grouping is the feature that doesn't fit.

## Checklist

- [x] Decision recorded (support multi-char; drop grouping)
- [x] Parser: removed single-char validation in `parsing/parser/parser.segments.cs`
- [x] Matcher: removed grouping heuristic in `parsing/runtime/matchers/option-matcher.cs`
      (this also resolves 454-014 / finding M11)
- [x] Agent context regions added/reconciled: parser.segments.cs, option-matcher.cs,
      lexer.cs (Purpose + Design recording the decision and the grouping trade-off)
- [x] Docs updated to match: `documentation/developer/design/parser/syntax-rules.md`
      (NURU_P003 section), `route-pattern-anatomy.md` (6.3 Option Name),
      `documentation/user/features/analyzer.md` (NURU_P003 section)
- [x] Tests: `tests/timewarp-nuru-tests/parser/parser-16-multi-char-short-options.cs`
      (6 tests: -bl flag, -verbosity {level}, --binary-log,-bl alias, exact-match
      semantics incl. `-e` NOT matching `-help`, no prefix cross-match, and an
      end-to-end generated-path test binding the flag)
- [x] `ganda runfile cache --clear` + full CI: 1280 multi-mode tests green + 2
      standalone Roslyn-hosted phases, exit 0

## Notes

- `InvalidOptionFormatError` / NURU_P003 now have no producer (the removed validation
  was the only site). The public error type, JSON serialization, and descriptor are
  kept — reserved for genuinely malformed option syntax.
- Discovered along the way and recorded in 454-013: boolean flags NEVER check
  IsOptional in the generated matcher (`EmitFlagParsingWithIndexTracking`) — a flag
  route also matches when the flag is absent (binds false). The NURU_R003 report that
  initially blocked the two-route e2e test shape is CONSISTENT with those semantics;
  454-013 now covers deciding required-vs-optional flag semantics AND aligning the
  overlap validator (plus a route-display artifact: short-only options render as
  `--,-bl`).

## Session

- Created: 2026-07-06 (full-repo review session)
- Investigation + implementation: 2026-07-06 (same session)
