# Fix Lexer Invalid Token Detection

## Problem
The lexer doesn't properly identify malformed patterns as Invalid tokens, instead tokenizing them as separate valid tokens. This allows nonsensical patterns to pass through to the parser.

## Test Case
`Tests/TimeWarp.Nuru.Tests/Lexer/test-invalid-token-detection.cs` - All 23 tests now passing

## Design Specification
`Documentation/Developer/Design/lexer-tokenization-rules.md` - Defines what patterns should produce Invalid tokens

## Fixed Patterns
The following patterns now correctly produce `Invalid` tokens:

1. **Double dashes within identifiers:**
   - `test--case` → `[Invalid: test--case]`
   - `foo--bar--baz` → `[Invalid: foo--bar--baz]`
   - `my--option` → `[Invalid: my--option]`

2. **Trailing dashes:**
   - `test-` → `[Invalid: test-]`
   - `test--` → `[Invalid: test--]`
   - `foo---` → `[Invalid: foo---]`

3. **Ambiguous single dash patterns:**
   - `-test` → `[Invalid: -test]`
   - `-foo-bar` → `[Invalid: -foo-bar]`

## Implementation Details
Updated `RouteLexer.cs` to detect malformed patterns during tokenization:
- Modified `ScanIdentifier()` to detect trailing dashes and double dashes within identifiers
- Modified dash case in `ScanToken()` to detect single dash with multi-character identifiers
- All invalid patterns now produce `TokenType.Invalid` tokens

## Acceptance Criteria
- [x] All tests in `test-invalid-token-detection.cs` pass (23/23 passing)
- [x] Parser tests still pass (ensure we don't break valid patterns)
- [x] Clear error messages for Invalid tokens

## Status
✅ **COMPLETED** - Fixed in commit bd000c1