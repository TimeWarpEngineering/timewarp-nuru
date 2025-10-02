# Lexer Tokenization Test Plan

This test plan provides comprehensive coverage for the lexer tokenization rules defined in [documentation/developer/design/lexer-tokenization-rules.md](../../../documentation/developer/design/lexer-tokenization-rules.md).

## Testing Strategy

The lexer's primary responsibility is to reject nonsensical character sequences early, making the parser's job simpler. Tests follow a progressive complexity model: basic single-token tests → valid compounds → invalid patterns → integration tests.

## 1. Basic Token Type Tests
**Test File**: `lexer-01-basic-token-types.cs`
**Purpose**: Verify each token type is correctly identified

- [x] **Identifier tokens** - plain text words like `status`, `version`, `help` - `Should_tokenize_plain_identifiers`
- [x] **Identifier tokens with dashes** - compound names with 1-3 internal dashes like `dry-run`, `no-edit`, `this-is-a-long-name` - `Should_tokenize_compound_identifiers`
- [x] **LeftBrace** - `{` character - `Should_tokenize_left_brace`
- [x] **RightBrace** - `}` character - `Should_tokenize_right_brace`
- [x] **Colon** - `:` character - `Should_tokenize_colon`
- [x] **Question** - `?` character - `Should_tokenize_question`
- [x] **Asterisk** - `*` character - `Should_tokenize_asterisk`
- [x] **Pipe** - `|` character - `Should_tokenize_pipe`
- [x] **Comma** - `,` character - `Should_tokenize_comma`
- [x] **DoubleDash** - `--` at start of option - `Should_tokenize_double_dash`
- [x] **SingleDash** - `-` at start of short option - `Should_tokenize_single_dash`
- [x] **EndOfOptions** - standalone `--` - `Should_tokenize_end_of_options`
- [x] **Description** - text after `|` (lexer continues normal tokenization, parser handles description) - `Should_tokenize_description`
- [x] **EndOfInput** - EOF marker (always present, even with empty input) - `Should_tokenize_end_of_input`

## 2. Valid Option Tests
**Test File**: `lexer-02-valid-options.cs`
**Purpose**: Verify correct tokenization of short and long options

**Long Options:**
- [x] `--dry-run` → `[DoubleDash]`, `[Identifier]` - `Should_tokenize_long_options`
- [x] `--no-edit` → `[DoubleDash]`, `[Identifier]` - `Should_tokenize_long_options`
- [x] `--save-dev` → `[DoubleDash]`, `[Identifier]` - `Should_tokenize_long_options`
- [x] `--v` → `[DoubleDash]`, `[Identifier]` (single char after --) - `Should_tokenize_long_options`

**Short Options:**
- [x] `-h` → `[SingleDash]`, `[Identifier]` - `Should_tokenize_short_options`
- [x] `-v` → `[SingleDash]`, `[Identifier]` - `Should_tokenize_short_options`
- [x] `-x` → `[SingleDash]`, `[Identifier]` - `Should_tokenize_short_options`

## 3. Invalid: Double Dashes Within Text
**Test File**: `lexer-03-invalid-double-dashes.cs`
**Purpose**: Detect malformed identifiers with embedded `--`

- [x] `test--case` → `[Invalid]` (not split into 3 tokens) - `Should_reject_double_dashes_within_identifiers`
- [x] `foo--bar--baz` → `[Invalid]` (multiple double dashes) - `Should_reject_double_dashes_within_identifiers`
- [x] `my--option` → `[Invalid]` (double dash not at start) - `Should_reject_double_dashes_within_identifiers`
- [x] `a--b` → `[Invalid]` (minimal case) - `Should_reject_double_dashes_within_identifiers`

## 4. Invalid: Trailing Dashes
**Test File**: `lexer-04-invalid-trailing-dashes.cs`
**Purpose**: Detect incomplete/malformed identifiers

- [x] `test-` → `[Invalid]` (trailing single dash) - `Should_reject_trailing_dashes`
- [x] `test--` → `[Invalid]` (trailing double dash) - `Should_reject_trailing_dashes`
- [x] `foo---` → `[Invalid]` (multiple trailing dashes) - `Should_reject_trailing_dashes`
- [x] `my-command-` → `[Invalid]` (compound with trailing dash) - `Should_reject_trailing_dashes`

## 5. Valid: Multi-Character Short Options
**Test File**: `lexer-05-multi-char-short-options.cs`
**Purpose**: Verify multi-character short options are accepted (e.g., `dotnet run -bl`)

- [ ] `-test` → `[SingleDash]`, `[Identifier]`
- [ ] `-bl` → `[SingleDash]`, `[Identifier]` (real-world example: dotnet binary logger)
- [ ] `-verbosity` → `[SingleDash]`, `[Identifier]`
- [ ] `-abc` → `[SingleDash]`, `[Identifier]`

**Rationale**: Real-world CLI tools use multi-character short options. Rejecting these would prevent legitimate use cases.

## 6. EndOfOptions Separator Tests
**Test File**: `lexer-06-end-of-options.cs`
**Purpose**: Distinguish standalone `--` from option prefix

- [ ] `git log --` → `[Identifier]`, `[Identifier]`, `[EndOfOptions]`
- [ ] `exec -- {*args}` → proper tokenization with `[EndOfOptions]`
- [ ] `-- something` → `[EndOfOptions]`, `[Identifier]`
- [ ] Trailing `--` at end of pattern
- [ ] `--` followed by EOF
- [ ] Multiple `--` separators in single pattern

## 7. Invalid: Angle Brackets
**Test File**: `lexer-07-invalid-angle-brackets.cs`
**Purpose**: Catch common mistake of using `<>` instead of `{}`

- [ ] `test<param>` → `[Identifier]`, `[Invalid]`
- [ ] `<param>` → `[Invalid]`
- [ ] `{param}` → valid (ensure angle brackets specifically invalid)
- [ ] Mixed brackets: `{param>` or `<param}`

## 8. Whitespace Handling
**Test File**: `lexer-08-whitespace-handling.cs`
**Purpose**: Verify whitespace properly separates tokens

- [ ] Single space between tokens
- [ ] Multiple spaces between tokens
- [ ] Tab characters
- [ ] Leading whitespace
- [ ] Trailing whitespace
- [ ] Whitespace around special characters (`{`, `}`, `|`, etc.)
- [ ] No whitespace between adjacent special chars

## 9. Complex Pattern Integration Tests
**Test File**: `lexer-09-complex-patterns.cs`
**Purpose**: Real-world patterns combining multiple token types

- [ ] `deploy {env} --dry-run` → proper tokenization
- [ ] `git commit -m {message}` → proper tokenization
- [ ] `build --config {mode:string}` → proper tokenization
- [ ] `exec -- {*args}` → proper tokenization
- [ ] `greet {name?} | Say hello` → proper tokenization with description
- [ ] `cmd {a} {b:int} --flag {c?} | description` → comprehensive pattern

## 10. Edge Cases
**Test File**: `lexer-10-edge-cases.cs`
**Purpose**: Boundary conditions and unusual inputs

- [ ] Empty string → `[EndOfInput]`
- [ ] Only whitespace → `[EndOfInput]`
- [ ] Single character tokens
- [ ] Very long identifiers (100+ chars)
- [ ] Adjacent special characters: `{}`, `{?}`, `{*}`, `{:}`
- [ ] Mixed valid/invalid patterns
- [ ] Unicode characters in identifiers (if supported)

## 11. Error Reporting Tests
**Test File**: `lexer-11-error-reporting.cs`
**Purpose**: Verify clear error messages for invalid tokens

- [ ] Invalid token includes position information
- [ ] Error message explains why token is invalid
- [ ] Multiple invalid tokens in single pattern
- [ ] Invalid token at start of pattern
- [ ] Invalid token at end of pattern
- [ ] Invalid token in middle of pattern

## 12. Description Tokenization
**Test File**: `lexer-12-description-tokenization.cs`
**Purpose**: Verify text after `|` is captured correctly

- [ ] Simple description: `command | help text`
- [ ] Description with special chars: `cmd | use --force carefully`
- [ ] Description at end of complex pattern
- [ ] Multiple `|` characters (should only first be separator?)
- [ ] Empty description after `|`
- [ ] Description with braces: `cmd | use {syntax} here`
- [ ] Trailing whitespace in description

## 13. Parameter Context Tests
**Test File**: `lexer-13-parameter-context.cs`
**Purpose**: Ensure tokens inside `{}` are handled correctly

- [ ] `{name}` → proper tokenization
- [ ] `{name:int}` → proper tokenization with colon
- [ ] `{name?}` → proper tokenization with question
- [ ] `{*args}` → proper tokenization with asterisk
- [ ] `{invalid--name}` → should invalid be detected inside braces?
- [ ] `{name:type?}` → combined type and optional
- [ ] `{name:enum1|enum2|enum3}` → enum values with pipes
- [ ] Nested or malformed braces: `{{name}}`, `{name`

## 14. Token Position and Span Tests
**Test File**: `lexer-14-token-position.cs`
**Purpose**: Verify accurate source location tracking

- [ ] Token start position is correct
- [ ] Token end position is correct
- [ ] Token length matches actual text
- [ ] Position tracking across whitespace
- [ ] Position tracking for multi-char tokens (`--`, `EndOfOptions`)
- [ ] Position tracking for invalid tokens

---

## Test Categories Summary

| # | Category | Test File | Purpose | Estimated Tests |
|---|----------|-----------|---------|----------------|
| 1 | Basic Token Types | `lexer-01-basic-token-types.cs` | Core token identification | 14 |
| 2 | Valid Options | `lexer-02-valid-options.cs` | Short/long option parsing | 7 |
| 3 | Invalid Double Dashes | `lexer-03-invalid-double-dashes.cs` | Malformed identifiers | 4 |
| 4 | Invalid Trailing Dashes | `lexer-04-invalid-trailing-dashes.cs` | Incomplete identifiers | 4 |
| 5 | Multi-Char Short Options | `lexer-05-multi-char-short-options.cs` | Real-world patterns | 4 |
| 6 | EndOfOptions | `lexer-06-end-of-options.cs` | Separator handling | 6 |
| 7 | Invalid Angle Brackets | `lexer-07-invalid-angle-brackets.cs` | Common mistakes | 4 |
| 8 | Whitespace | `lexer-08-whitespace-handling.cs` | Token separation | 7 |
| 9 | Complex Integration | `lexer-09-complex-patterns.cs` | Real patterns | 6 |
| 10 | Edge Cases | `lexer-10-edge-cases.cs` | Boundaries | 7 |
| 11 | Error Reporting | `lexer-11-error-reporting.cs` | Diagnostics | 6 |
| 12 | Descriptions | `lexer-12-description-tokenization.cs` | Help text | 7 |
| 13 | Parameter Context | `lexer-13-parameter-context.cs` | Inside braces | 8 |
| 14 | Token Position | `lexer-14-token-position.cs` | Source tracking | 6 |

**Total Test Categories**: 14
**Estimated Individual Test Cases**: 90

## Implementation Notes

1. **Test Organization**: Group tests by category, potentially one test file per category
2. **Test Data**: Consider using theory/parameterized tests for similar patterns
3. **Assertion Strategy**: Verify both token type and token text content
4. **Error Messages**: Include expected error message text in invalid token tests
5. **Performance**: Include a benchmark test for lexing large patterns

## Open Questions

1. Should `-test` be treated as invalid or split into `[SingleDash]`, `[Identifier]`?
2. Should invalid patterns inside `{}` be caught by lexer or parser?
3. What's the maximum supported identifier length?
4. Should the lexer support Unicode identifiers?
5. How should multiple `|` characters be handled (first wins vs error)?
