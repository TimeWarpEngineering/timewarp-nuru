# Lexer Tokenization Test Plan

This test plan provides comprehensive coverage for the lexer tokenization rules defined in [documentation/developer/design/lexer-tokenization-rules.md](../../../documentation/developer/design/lexer-tokenization-rules.md).

## Testing Strategy

The lexer's primary responsibility is to reject nonsensical character sequences early, making the parser's job simpler. Tests follow a progressive complexity model: basic single-token tests → valid compounds → invalid patterns → integration tests.

## 1. Basic Token Type Tests
**Test File**: `lexer-01-basic-token-types.cs`
**Purpose**: Verify each token type is correctly identified

- [x] **Identifier tokens** - plain text words like `status`, `version`, `help` - `Should_tokenize_plain_identifiers`
- [x] **Identifier tokens with dashes** - compound names like `dry-run`, `no-edit` - `Should_tokenize_compound_identifiers`
- [x] **LeftBrace** - `{` character - `Should_tokenize_left_brace`
- [ ] **RightBrace** - `}` character
- [ ] **Colon** - `:` character
- [ ] **Question** - `?` character
- [ ] **Asterisk** - `*` character
- [ ] **Pipe** - `|` character
- [ ] **Comma** - `,` character
- [ ] **DoubleDash** - `--` at start of option
- [ ] **SingleDash** - `-` at start of short option
- [ ] **EndOfOptions** - standalone `--`
- [ ] **Description** - text after `|`
- [ ] **EndOfInput** - EOF marker

## 2. Valid Compound Identifier Tests
**Test File**: `lexer-02-valid-compound-identifiers.cs`
**Purpose**: Ensure single internal dashes are accepted

- [ ] `dry-run` → single `Identifier` token
- [ ] `no-edit` → single `Identifier` token
- [ ] `save-dev` → single `Identifier` token
- [ ] `my-long-command-name` → single `Identifier` token
- [ ] Multiple dashes: `this-is-a-long-name` → single `Identifier`

## 3. Valid Option Tests
**Test File**: `lexer-03-valid-options.cs`
**Purpose**: Verify correct tokenization of short and long options

**Long Options:**
- [ ] `--dry-run` → `[DoubleDash]`, `[Identifier]`
- [ ] `--no-edit` → `[DoubleDash]`, `[Identifier]`
- [ ] `--save-dev` → `[DoubleDash]`, `[Identifier]`
- [ ] `--v` → `[DoubleDash]`, `[Identifier]` (single char after --)

**Short Options:**
- [ ] `-h` → `[SingleDash]`, `[Identifier]`
- [ ] `-v` → `[SingleDash]`, `[Identifier]`
- [ ] `-x` → `[SingleDash]`, `[Identifier]`

## 4. Invalid: Double Dashes Within Text
**Test File**: `lexer-04-invalid-double-dashes.cs`
**Purpose**: Detect malformed identifiers with embedded `--`

- [ ] `test--case` → `[Invalid]` (not split into 3 tokens)
- [ ] `foo--bar--baz` → `[Invalid]` (multiple double dashes)
- [ ] `my--option` → `[Invalid]` (double dash not at start)
- [ ] `a--b` → `[Invalid]` (minimal case)

## 5. Invalid: Trailing Dashes
**Test File**: `lexer-05-invalid-trailing-dashes.cs`
**Purpose**: Detect incomplete/malformed identifiers

- [ ] `test-` → `[Invalid]` (trailing single dash)
- [ ] `test--` → `[Invalid]` (trailing double dash)
- [ ] `foo---` → `[Invalid]` (multiple trailing dashes)
- [ ] `my-command-` → `[Invalid]` (compound with trailing dash)

## 6. Invalid: Leading Single Dash with Multi-Character
**Test File**: `lexer-06-invalid-leading-dash.cs`
**Purpose**: Detect ambiguous option patterns

- [ ] `-test` → `[Invalid]` or `[SingleDash]`, `[Identifier]` (document current behavior, decide if should be invalid)
- [ ] `-abc` → same as above
- [ ] `-multi-word` → same as above

**Note**: Need to determine desired behavior - should `-multi` be invalid or split into `[SingleDash]`, `[Identifier]`?

## 7. EndOfOptions Separator Tests
**Test File**: `lexer-07-end-of-options.cs`
**Purpose**: Distinguish standalone `--` from option prefix

- [ ] `git log --` → `[Identifier]`, `[Identifier]`, `[EndOfOptions]`
- [ ] `exec -- {*args}` → proper tokenization with `[EndOfOptions]`
- [ ] `-- something` → `[EndOfOptions]`, `[Identifier]`
- [ ] Trailing `--` at end of pattern
- [ ] `--` followed by EOF
- [ ] Multiple `--` separators in single pattern

## 8. Invalid: Angle Brackets
**Test File**: `lexer-08-invalid-angle-brackets.cs`
**Purpose**: Catch common mistake of using `<>` instead of `{}`

- [ ] `test<param>` → `[Identifier]`, `[Invalid]`
- [ ] `<param>` → `[Invalid]`
- [ ] `{param}` → valid (ensure angle brackets specifically invalid)
- [ ] Mixed brackets: `{param>` or `<param}`

## 9. Whitespace Handling
**Test File**: `lexer-09-whitespace-handling.cs`
**Purpose**: Verify whitespace properly separates tokens

- [ ] Single space between tokens
- [ ] Multiple spaces between tokens
- [ ] Tab characters
- [ ] Leading whitespace
- [ ] Trailing whitespace
- [ ] Whitespace around special characters (`{`, `}`, `|`, etc.)
- [ ] No whitespace between adjacent special chars

## 10. Complex Pattern Integration Tests
**Test File**: `lexer-10-complex-patterns.cs`
**Purpose**: Real-world patterns combining multiple token types

- [ ] `deploy {env} --dry-run` → proper tokenization
- [ ] `git commit -m {message}` → proper tokenization
- [ ] `build --config {mode:string}` → proper tokenization
- [ ] `exec -- {*args}` → proper tokenization
- [ ] `greet {name?} | Say hello` → proper tokenization with description
- [ ] `cmd {a} {b:int} --flag {c?} | description` → comprehensive pattern

## 11. Edge Cases
**Test File**: `lexer-11-edge-cases.cs`
**Purpose**: Boundary conditions and unusual inputs

- [ ] Empty string → `[EndOfInput]`
- [ ] Only whitespace → `[EndOfInput]`
- [ ] Single character tokens
- [ ] Very long identifiers (100+ chars)
- [ ] Adjacent special characters: `{}`, `{?}`, `{*}`, `{:}`
- [ ] Mixed valid/invalid patterns
- [ ] Unicode characters in identifiers (if supported)

## 12. Error Reporting Tests
**Test File**: `lexer-12-error-reporting.cs`
**Purpose**: Verify clear error messages for invalid tokens

- [ ] Invalid token includes position information
- [ ] Error message explains why token is invalid
- [ ] Multiple invalid tokens in single pattern
- [ ] Invalid token at start of pattern
- [ ] Invalid token at end of pattern
- [ ] Invalid token in middle of pattern

## 13. Description Tokenization
**Test File**: `lexer-13-description-tokenization.cs`
**Purpose**: Verify text after `|` is captured correctly

- [ ] Simple description: `command | help text`
- [ ] Description with special chars: `cmd | use --force carefully`
- [ ] Description at end of complex pattern
- [ ] Multiple `|` characters (should only first be separator?)
- [ ] Empty description after `|`
- [ ] Description with braces: `cmd | use {syntax} here`
- [ ] Trailing whitespace in description

## 14. Parameter Context Tests
**Test File**: `lexer-14-parameter-context.cs`
**Purpose**: Ensure tokens inside `{}` are handled correctly

- [ ] `{name}` → proper tokenization
- [ ] `{name:int}` → proper tokenization with colon
- [ ] `{name?}` → proper tokenization with question
- [ ] `{*args}` → proper tokenization with asterisk
- [ ] `{invalid--name}` → should invalid be detected inside braces?
- [ ] `{name:type?}` → combined type and optional
- [ ] `{name:enum1|enum2|enum3}` → enum values with pipes
- [ ] Nested or malformed braces: `{{name}}`, `{name`

## 15. Token Position and Span Tests
**Test File**: `lexer-15-token-position.cs`
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
| 2 | Valid Compound Identifiers | `lexer-02-valid-compound-identifiers.cs` | Dash handling in names | 5 |
| 3 | Valid Options | `lexer-03-valid-options.cs` | Short/long option parsing | 7 |
| 4 | Invalid Double Dashes | `lexer-04-invalid-double-dashes.cs` | Malformed identifiers | 4 |
| 5 | Invalid Trailing Dashes | `lexer-05-invalid-trailing-dashes.cs` | Incomplete identifiers | 4 |
| 6 | Invalid Leading Dash | `lexer-06-invalid-leading-dash.cs` | Ambiguous patterns | 3 |
| 7 | EndOfOptions | `lexer-07-end-of-options.cs` | Separator handling | 6 |
| 8 | Invalid Angle Brackets | `lexer-08-invalid-angle-brackets.cs` | Common mistakes | 4 |
| 9 | Whitespace | `lexer-09-whitespace-handling.cs` | Token separation | 7 |
| 10 | Complex Integration | `lexer-10-complex-patterns.cs` | Real patterns | 6 |
| 11 | Edge Cases | `lexer-11-edge-cases.cs` | Boundaries | 7 |
| 12 | Error Reporting | `lexer-12-error-reporting.cs` | Diagnostics | 6 |
| 13 | Descriptions | `lexer-13-description-tokenization.cs` | Help text | 7 |
| 14 | Parameter Context | `lexer-14-parameter-context.cs` | Inside braces | 8 |
| 15 | Token Position | `lexer-15-token-position.cs` | Source tracking | 6 |

**Total Test Categories**: 15
**Estimated Individual Test Cases**: 94

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
