# Fix Lexer Invalid Token Detection

## Problem
The lexer doesn't properly identify malformed patterns as Invalid tokens, instead tokenizing them as separate valid tokens. This allows nonsensical patterns to pass through to the parser.

## Test Case
`Tests/TimeWarp.Nuru.Tests/Lexer/test-invalid-token-detection.cs` - Shows 8 failing tests where lexer behavior doesn't match design

## Design Specification
`Documentation/Developer/Design/lexer-tokenization-rules.md` - Defines what patterns should produce Invalid tokens

## Failing Patterns
The following patterns should produce `Invalid` tokens but don't:

1. **Double dashes within identifiers:**
   - `test--case` → Should be `[Invalid]`, gets `[Id] test, [DD] --, [Id] case`
   - `foo--bar--baz` → Should be `[Invalid]`, gets multiple tokens
   - `my--option` → Should be `[Invalid]`, gets `[Id] my, [DD] --, [Id] option`

2. **Trailing dashes:**
   - `test-` → Should be `[Invalid]`, gets `[Id] test, [SD] -`
   - `test--` → Should be `[Invalid]`, gets `[Id] test, [EoO] --`
   - `foo---` → Should be `[Invalid]`, gets `[Id] foo, [DD] --, [SD] -`

3. **Ambiguous single dash patterns:**
   - `-test` → Should be `[Invalid]`, gets `[SD] -, [Id] test`
   - `-foo-bar` → Should be `[Invalid]`, gets `[SD] -, [Id] foo-bar`

## Implementation Notes
Update `RouteLexer.cs` to detect these malformed patterns during tokenization and produce `Invalid` tokens instead of trying to make sense of them.

## Acceptance Criteria
- [ ] All tests in `test-invalid-token-detection.cs` pass
- [ ] Parser tests still pass (ensure we don't break valid patterns)
- [ ] Clear error messages for Invalid tokens