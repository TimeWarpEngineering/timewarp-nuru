# Fix Lexer Whitespace And End Of Options Handling

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M12, M14).

## Description

Two lexer whitespace defects in `source/timewarp-nuru-parsing/lexer/lexer.cs`:

- **M12** (line ~130): end-of-options `--` is recognized only when followed by end or an
  ASCII space (`IsAtEnd() || Peek() == ' '`). Verified divergence: `-- x` lexes as
  EndOfOptions + literal (and correctly errors "must be followed by a catch-all"), but
  `--\tx` lexes as long option `--x` and parses successfully. Tab/newline/CR after `--`
  silently change parse semantics. Use a whitespace class, not `' '`.
- **M14** (lines ~94-99): whitespace is fully discarded with no boundary token, so
  `greet {a}{b}` produces a token stream identical to `greet {a} {b}` — both verified to
  parse successfully. Adjacent parameters with no separator are almost certainly a typo
  and should be a diagnostic (or at minimum a documented, deliberate behavior).

## Checklist

- [x] `--` detection uses whitespace class (tab/CR/LF)
- [x] Adjacent segments without whitespace produce a diagnostic (or documented decision)
- [x] Lexer/parser tests for `--\tx` and `{a}{b}`
- [x] `ganda runfile cache --clear` + run CI tests

## Notes

### Implementation Plan (2026-07-06)

#### Decisions

| # | Question | Decision |
|---|----------|----------|
| 1 | M12 whitespace check | `char.IsWhiteSpace(Peek())` — replaces `Peek() == ' '` at lexer.cs:143 |
| 2 | M14 behavior | Emit a diagnostic error (ParseError) — `{a}{b}` is a likely typo |
| 3 | M14 detection level | Parser-level, not lexer — uses token positions: `Previous().EndPosition == Current().Position` means no whitespace gap between `}` and `{` |

#### Key architectural insight
The lexer has no error accumulator (only emits `Invalid` tokens). The parser already has `AddParseError` and token-position awareness (`Previous()`, `Current()`). So M14 is cleanest as a parser-level check: when `ParseSegment` sees a `LeftBrace` whose `Position` equals the previous `RightBrace`'s `EndPosition`, there was no whitespace — emit `AdjacentParametersError`. No new `RouteTokenType`, no lexer changes for M14.

#### Step 1: M12 — Whitespace class for end-of-options
File: `source/timewarp-nuru-parsing/parsing/lexer/lexer.cs:143`
- Change `if (IsAtEnd() || Peek() == ' ')` to `if (IsAtEnd() || char.IsWhiteSpace(Peek()))`
- Effect: `--\tx`, `--\rx`, `--\nx` now produce `EndOfOptions + Identifier` (same as `-- x` today)

#### Step 2: M14 — Add `AdjacentParametersError` ParseError subtype
File: `source/timewarp-nuru-parsing/parsing/parser/parse-error.cs`
- New record `AdjacentParametersError(int Position, int Length) : ParseError(Position, Length)`
- Message: "Adjacent parameters must be separated by whitespace (e.g., '{a} {b}' rather than '{a}{b}')"

#### Step 3: M14 — Detect adjacency in parser
File: `source/timewarp-nuru-parsing/parsing/parser/parser.cs` (`ParseSegment`)
- Before the `switch` in `ParseSegment`, check: if current token is `LeftBrace` AND `Previous()` is `RightBrace` AND `Previous().EndPosition == current.Position` → emit `AdjacentParametersError`
- Still parse the parameter (emit diagnostic, continue recovery — never throw, preserve fuzz guarantee)

#### Step 4: M12 tests
File: `tests/timewarp-nuru-tests/lexer/lexer-06-end-of-options.cs`
- `Should_tokenize_end_of_options_followed_by_tab` — `"--\tx"` → EndOfOptions + Identifier
- `Should_tokenize_end_of_options_followed_by_carriage_return` — `"--\rx"`
- `Should_tokenize_end_of_options_followed_by_newline` — `"--\nx"`
- `Should_tokenize_end_of_options_with_tab_separator` — `"git log --\t{*args}""` → 8 tokens

#### Step 5: M14 tests
New file: `tests/timewarp-nuru-tests/parser/parser-18-adjacent-parameters.cs`
1. `Should_error_on_adjacent_parameters_no_whitespace` — `"run {a}{b}"` → PatternException with AdjacentParametersError
2. `Should_allow_parameters_separated_by_space` — `"run {a} {b}"` → no throw
3. `Should_allow_parameters_separated_by_tab` — `"run {a}\t{b}"` → no throw
4. `Should_error_on_three_adjacent_parameters_reports_each_adjacency` — `"run {a}{b}{c}"` → at least two AdjacentParametersError
5. `Should_error_on_adjacent_option_parameter_then_top_parameter` — `"run --opt {a}{b}"` → AdjacentParametersError
6. `Should_not_error_on_option_with_parameter_followed_by_spaced_parameter` — `"run --opt {a} {b}"` → no throw

#### Step 6: M14 lexer documentation test
File: `tests/timewarp-nuru-tests/lexer/lexer-08-whitespace-handling.cs`
- `Should_tokenize_adjacent_parameter_blocks_without_special_token` — documents that the lexer still tokenizes `{a}{b}` as two parameter groups (no separator token); the diagnostic is parser-level

#### Step 7: Verify
1. `ganda runfile cache --clear`
2. Run targeted test files standalone
3. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI)

#### Files touched
- Edit: `lexer.cs` (M12 one-liner)
- Edit: `parse-error.cs` (add AdjacentParametersError)
- Edit: `parser.cs` (add adjacency check in ParseSegment)
- Edit: `lexer-06-end-of-options.cs` (4 M12 tests)
- Edit: `lexer-08-whitespace-handling.cs` (1 M14 doc test)
- Create: `parser-18-adjacent-parameters.cs` (6 M14 tests)

#### Risk assessment
- M12 is additive: only accepts MORE whitespace chars as EndOfOptions separator. No existing test breaks (all use `' '` which is still whitespace).
- M14 is a breaking change: `{a}{b}` previously parsed successfully, now errors. But this is almost certainly a typo. Unlikely to affect real route patterns.
- No throws from new paths: M14 emits a ParseError and continues parsing (recovery). Fuzz guarantee preserved.
- No new RouteTokenType: parser-level detection uses token positions only.

## Results

### What was implemented

Fixed two lexer whitespace defects (M12, M14) in the route pattern parser.

- **M12 (end-of-options whitespace)**: Changed `Peek() == ' '` to `char.IsWhiteSpace(Peek())` at `lexer.cs:143`. Now `--\tx`, `--\rx`, `--\nx` correctly tokenize as `EndOfOptions + Identifier` instead of `DoubleDash + Identifier` (which silently created a long option `--x`). Tab/CR/LF after `--` no longer change parse semantics.
- **M14 (adjacent parameters)**: Added `AdjacentParametersError` ParseError subtype and a parser-level check in `ParseSegment`. When a `LeftBrace` token immediately follows a `RightBrace` token with no position gap (`Previous().EndPosition == token.Position`), there was no whitespace separator — emit a diagnostic. The parser continues parsing (emits error, doesn't throw) to preserve the 200k-fuzz no-throws guarantee.
- Detection is parser-level (not lexer-level) because the parser already has `AddParseError` and token-position awareness. No new `RouteTokenType` needed.

### Files changed

- `source/timewarp-nuru-parsing/parsing/lexer/lexer.cs` — M12 one-liner (`char.IsWhiteSpace`)
- `source/timewarp-nuru-parsing/parsing/parser/parse-error.cs` — added `AdjacentParametersError` record
- `source/timewarp-nuru-parsing/parsing/parser/parser.cs` — added adjacency check in `ParseSegment`
- `tests/timewarp-nuru-tests/lexer/lexer-06-end-of-options.cs` — 4 M12 tests (tab/CR/LF/newline after `--`)
- `tests/timewarp-nuru-tests/lexer/lexer-08-whitespace-handling.cs` — 1 M14 lexer documentation test
- `tests/timewarp-nuru-tests/parser/parser-18-adjacent-parameters.cs` (new) — 6 M14 parser tests

### Key decisions made

- **`char.IsWhiteSpace` over inline four-char check**: Standard .NET whitespace check; route patterns are unlikely to contain exotic Unicode whitespace.
- **Parser-level detection for M14**: The lexer has no error accumulator (only emits `Invalid` tokens). The parser already has `AddParseError` and token positions, so M14 is cleanest there. `Token.EndPosition` exists as a computed property (`Position + Length`).
- **Emit diagnostic, continue parsing**: The adjacency check adds a `ParseError` but still calls `ParseParameter()` — the parser recovers and the error makes `result.Success` false, surfacing as `PatternException`. No throws, fuzz guarantee preserved.
- **Scope limited to `}{` adjacency**: Only consecutive parameter blocks (`{a}{b}`) are checked. `literal{a}` or `--opt{a}` (non-`}{` adjacency) are out of scope per the design decision.

### Test outcomes

- **Standalone lexer-06**: 9 passed (5 existing + 4 new M12 tests)
- **Standalone lexer-08**: 8 passed (7 existing + 1 new M14 doc test)
- **Standalone parser-18**: 6 passed (all new M14 tests)
- **Full CI** (`dotnet run tests/ci-tests/run-ci-tests.cs`): 1337 passed, 7 skipped, 0 failed. No regressions.

(End of file - total 93 lines)
