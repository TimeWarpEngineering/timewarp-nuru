# Round 1 — parsing
**Date:** 2026-09-04
**Scope reviewed:** `source/timewarp-nuru-parsing/` (lexer, parser, semantic validation, compiler, syntax, runtime matchers) and `tests/timewarp-nuru-tests/lexer/`, `parser/`, `routing/` (matcher behavior). Judged product tree at pinned SHA `38480f57` / product `648369f6` (`3.0.0-beta.77`).

## Summary

Parsing remains crash-safe under a 50k malformed-pattern fuzz (0 uncaught exceptions). All listed 454 parsing regressions are absent in the current tree (multi-char shorts supported, grouping removed, EndOfOptions whitespace + adjacent-parameter diagnostics, duplicate long/short alias validation, InvalidModifierCombination span + dead code cleanup). One new **bug** found: option-description consumption does not treat `EndOfOptions` as a segment boundary, so a trailing `-- {*args}` after `|description` is swallowed / mis-attached. One **suggestion** on parameter-name validation vs documented identifier rules; two **nits** on error UX.

## Crash-safety

Ran a throwaway harness under `/tmp/nuru-parse-fuzz` that Compile-Includes the parsing sources and exercises the public API:

```text
cd /tmp/nuru-parse-fuzz && dotnet run -c Release --nologo
```

- **API:** `PatternParser.TryParse` (analyzer-host path) + `PatternParser.Parse` (allowing only `PatternException`)
- **N:** 50,000 random patterns
- **Seed:** `470001`
- **Alphabet:** metacharacter-biased printable set + occasional empty/whitespace/NUL/Unicode/combining-accent cases
- **TryParse_ok:** 4166
- **TryParse_fail:** 45834
- **Uncaught exceptions:** **0**

(454 previously ran 200k with zero uncaught; this re-check is smaller but actually executed against the current sources.)

## 454 regression check

| 454 ID | Still present? | Evidence |
|--------|----------------|----------|
| 454-005 H5 multi-char single-dash options | No | Lexer emits `SingleDash`+`Identifier` for multi-char shorts (`lexer.cs:154-157`); parser accepts them (`parser.segments.cs:189-192`); `OptionMatcher` exact-matches (`option-matcher.cs:83-89`). Covered by `lexer-05`, `parser-16`. |
| 454-014 M11 grouped short-option over-match | No | Grouping/`Contains` heuristic removed; matcher is exact equality only (`option-matcher.cs:6-11,83-91`). `parser-16` asserts `-e` does not match `-help`/`-ea`. |
| 454-015 M12 end-of-options whitespace; M14 adjacent params | No | EndOfOptions uses `char.IsWhiteSpace` (`lexer.cs:143-145`); tests cover space/tab/CR/LF (`lexer-06`). Adjacent `{a}{b}` → `AdjacentParametersError` (`parser.cs:141-149`; `parser-18`). |
| 454-016 M13 duplicate long-form options | No | `ValidateDuplicateOptionAliases` checks short and long forms (`semantic-validator.cs:261-302`; `parser-17`). Dead `OptionAliases` property gone from `ValidationContext`. |
| 454-029 LOW span / dead code | No (fixed; checklist box still open on 454 parent) | `InvalidModifierCombinationError` spans through `?` (`parser.segments.cs:80-90`; probe `{*name?}` → pos=0 len=7). `PeekNext` removed from lexer. `OptionAliases` removed. `ValidateDuplicateParameters` has no unused locals. |

## Issues

### Issue 1 — Severity: bug
- File: `source/timewarp-nuru-parsing/parsing/parser/parser.cs:180`
- Description: `ConsumeDescription(stopAtRightBrace: false)` stops option descriptions at `DoubleDash` / `SingleDash` / `LeftBrace`, but **not** at `EndOfOptions`. A standalone `--` token is therefore taken by the `else` branch (`parser.cs:196-199`) and skipped, so it never becomes an `EndOfOptionsSyntax` segment. Consequences verified with `PatternParser.TryParse`:
  - `run --flag -- {*args}` → success (baseline).
  - `run --flag|desc -- {*args}` → **fails** with `InvalidParameterSyntaxError` for `{*args}` (catch-all mis-attached as the option's value parameter).
  - `cmd --opt | a -- b {x}` → **success with wrong AST**: EndOfOptions discarded, description becomes `"a b"`, and `{x}` is attached as `--opt`'s value (`ExpectsValue=true`).
- Suggestion: Add `RouteTokenType.EndOfOptions` to the option-description stop set (same list as DoubleDash/SingleDash/LeftBrace). Add regression tests for `|desc -- {*args}` and for the silent-corruption shape with a following `{param}`.
- Status: open

### Issue 2 — Severity: suggestion
- File: `source/timewarp-nuru-parsing/parsing/parser/parser.validation.cs:75-85`
- Description: Parameter-name validation via `IsValidIdentifier` only checks that the first character is a letter or `_`. Hyphenated names such as `{my-param}` therefore parse successfully (verified: `TryParse` success, `ParameterMatcher` name `"my-param"`), contradicting the documented rule “Alphanumeric + underscore, must start with letter” in `documentation/developer/design/parser/route-pattern-anatomy.md` (§4.1). Type constraints already reject hyphens via `IsValidIdentifierFormat` (`parser.validation.cs:48-73`; `parser-15`). Positional binding compares the raw name (no kebab→camel), so `{my-param}` cannot bind to any legal C# handler parameter.
- Suggestion: Validate parameter names with `IsValidIdentifierFormat` (or an equivalent that rejects `-`), keeping hyphenated identifiers legal for option *names* only.
- Status: open

### Issue 3 — Severity: nit
- File: `source/timewarp-nuru-parsing/parsing/parser/parse-error.cs:63`
- Description: `InvalidTypeConstraintError.SupportedTypes` omits several types accepted by `IsBuiltInType` (`parser.validation.cs:11-36`): `byte`, `sbyte`, `short`, `ushort`, `uint`, `ulong`, `float`, `char`. Users hitting an invalid constraint see an incomplete built-in list (e.g. `byte` works but is not listed).
- Suggestion: Generate or share one source of truth between `IsBuiltInType` and the error message list.
- Status: open

### Issue 4 — Severity: nit
- File: `source/timewarp-nuru-parsing/parsing/parser/parser.cs:147`
- Description: `AdjacentParametersError` is constructed with the opening `{` token's span only (`Position`/`Length` of `LeftBrace`). For `run {a}{b}` the diagnostic is `pos=7 len=1`, underlining `{` rather than `{b}`.
- Suggestion: After parsing the adjacent parameter (or using lookahead), widen the span to the full second parameter segment for clearer squiggles in the analyzer host.
- Status: open
