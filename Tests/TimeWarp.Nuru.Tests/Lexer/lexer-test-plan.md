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

- [x] `-test` → `[SingleDash]`, `[Identifier]` - `Should_tokenize_multi_char_short_options`
- [x] `-bl` → `[SingleDash]`, `[Identifier]` (real-world example: dotnet binary logger) - `Should_tokenize_multi_char_short_options`
- [x] `-verbosity` → `[SingleDash]`, `[Identifier]` - `Should_tokenize_multi_char_short_options`
- [x] `-abc` → `[SingleDash]`, `[Identifier]` - `Should_tokenize_multi_char_short_options`

**Rationale**: Real-world CLI tools use multi-character short options. Rejecting these would prevent legitimate use cases.

## 6. EndOfOptions Separator Tests
**Test File**: `lexer-06-end-of-options.cs`
**Purpose**: Distinguish standalone `--` from option prefix

- [x] `git log --` → `[Identifier]`, `[Identifier]`, `[EndOfOptions]` - `Should_tokenize_end_of_options_after_commands`
- [x] `exec -- {*args}` → proper tokenization with `[EndOfOptions]` - `Should_tokenize_end_of_options_in_middle_of_pattern`
- [x] `-- something` → `[EndOfOptions]`, `[Identifier]` - `Should_tokenize_end_of_options_at_start`
- [x] Trailing `--` at end of pattern - covered by `Should_tokenize_end_of_options_after_commands`
- [x] `--` followed by EOF - covered by Section 1's `Should_tokenize_end_of_options`
- [x] Multiple `--` separators in single pattern - `Should_tokenize_multiple_end_of_options_separators`

## 7. Invalid: Angle Brackets
**Test File**: `lexer-07-invalid-angle-brackets.cs`
**Purpose**: Catch common mistake of using `<>` instead of `{}`

- [x] `test<param>` → `[Identifier]`, `[Invalid]` - `Should_reject_angle_brackets_after_identifier`
- [x] `<param>` → `[Invalid]` - `Should_reject_angle_brackets_at_start`
- [x] `{param}` → valid (ensure angle brackets specifically invalid) - `Should_accept_curly_braces`
- [x] Mixed brackets: `{param>` - `Should_reject_mixed_bracket_syntax`

## 8. Whitespace Handling
**Test File**: `lexer-08-whitespace-handling.cs`
**Purpose**: Verify whitespace properly separates tokens

- [x] Single space between tokens - `Should_produce_identical_tokens_regardless_of_whitespace`
- [x] Multiple spaces between tokens - `Should_produce_identical_tokens_regardless_of_whitespace`
- [x] Tab characters - `Should_produce_identical_tokens_regardless_of_whitespace`
- [x] Leading whitespace - `Should_produce_identical_tokens_regardless_of_whitespace`
- [x] Trailing whitespace - `Should_produce_identical_tokens_regardless_of_whitespace`
- [x] Whitespace around special characters (`{`, `}`, `|`, etc.) - `Should_handle_whitespace_around_special_characters`
- [x] No whitespace between adjacent special chars - `Should_handle_no_whitespace_between_special_characters`

## 9. Complex Pattern Integration Tests
**Test File**: `lexer-09-complex-patterns.cs`
**Purpose**: Real-world patterns combining multiple token types

- [x] `deploy {env} --dry-run` → proper tokenization - `Should_tokenize_deploy_pattern_with_option`
- [x] `git commit -m {message}` → proper tokenization - `Should_tokenize_git_commit_with_short_option`
- [x] `build --config {mode:string}` → proper tokenization - `Should_tokenize_build_with_typed_parameter`
- [x] `exec -- {*args}` → proper tokenization - `Should_tokenize_exec_with_catchall`
- [x] `greet {name?} | Say hello` → proper tokenization with description - `Should_tokenize_pattern_with_optional_and_description`
- [x] `cmd {a} {b:int} --flag {c?} | description` → comprehensive pattern - `Should_tokenize_comprehensive_complex_pattern`

## 10. Edge Cases ✓
**Test File**: `lexer-10-edge-cases.cs`
**Purpose**: Boundary conditions and unusual inputs

- [x] Empty string → `[EndOfInput]` - `Should_tokenize_empty_string`
- [x] Only whitespace → `[EndOfInput]` - `Should_tokenize_only_whitespace` (5 parameterized inputs)
- [x] Single character tokens - `Should_tokenize_single_character_tokens`
- [x] Very long identifiers (150 chars) - `Should_tokenize_very_long_identifier`
- [x] Adjacent special characters: `{}`, `{?}`, `{*}`, `{:}` - `Should_tokenize_adjacent_special_characters`
- [x] Mixed valid/invalid patterns - `Should_handle_mixed_valid_invalid_patterns`
- [x] Unicode characters in identifiers - `Should_tokenize_unicode_identifiers` (Chinese, Greek, French)

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
