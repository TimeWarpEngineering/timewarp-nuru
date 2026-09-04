# Stop option descriptions at EndOfOptions

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M6). Suggestion/nits folded: M22, M38, M39.

## Description

`ConsumeDescription(stopAtRightBrace: false)` (`parser.cs:180`) stops at DoubleDash / SingleDash / LeftBrace but **not** EndOfOptions. A standalone `--` is skipped, so it never becomes an `EndOfOptionsSyntax` segment.

Verified with `PatternParser.TryParse`:
- `run --flag -- {*args}` → success (baseline)
- `run --flag|desc -- {*args}` → fails (`{*args}` mis-attached as option value)
- `cmd --opt | a -- b {x}` → success with wrong AST (EndOfOptions discarded, description `"a b"`, `{x}` attached as `--opt` value)

Crash-safety: 50k malformed patterns (seed 470001) produced 0 uncaught exceptions; this is a semantic parse bug, not a throw.

Folded:
- M22: parameter names allow hyphens (`{my-param}`) contrary to documented identifier rules
- M38: `InvalidTypeConstraintError.SupportedTypes` omits types `IsBuiltInType` accepts
- M39: `AdjacentParametersError` spans only the `{` token

## Requirements

- Add `RouteTokenType.EndOfOptions` to the option-description stop set.
- Regression tests for both mis-parse shapes.
- Validate parameter names with `IsValidIdentifierFormat` (hyphens stay legal for option names).
- Align SupportedTypes with IsBuiltInType; widen adjacent-parameter span.

## Checklist

- [ ] EndOfOptions stop in ConsumeDescription
- [ ] Tests for `|desc -- {*args}` and silent-corruption shape
- [ ] M22 parameter-name validation
- [ ] M38 / M39 error UX
- [ ] Preserve no-throw fuzz guarantee

## Notes

Evidence: parent 470 `review/round-1/merged.md` M6, M22, M38, M39. Fuzz note in `review/round-1/parsing.md`.
