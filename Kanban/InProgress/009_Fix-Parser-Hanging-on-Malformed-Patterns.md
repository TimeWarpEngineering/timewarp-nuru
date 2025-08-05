# Fix Parser Hanging on Malformed Patterns

## Description

The RoutePatternParser hangs indefinitely on certain malformed patterns containing unmatched or nested braces. This critical bug can freeze any tool using the parser, including IDEs and the upcoming Roslyn analyzer.

## Problem

### Affected Patterns
The parser hangs on these specific pattern types:

1. **Closing brace without opening brace**
   - `build config}` - HANGS
   - `deploy }` - HANGS

2. **Nested braces**
   - `test {a{b}}` - HANGS

### Correctly Handled Invalid Patterns
These patterns properly return errors without hanging:
- `test {` - Returns error properly
- `build --config {` - Returns error properly  
- `test {param` - Returns error properly
- `{}` - Returns error properly

## Requirements

- Fix lexer/parser to handle unmatched closing braces without hanging
- Prevent infinite loops when encountering nested parameter syntax
- Add timeout protection as a safety net
- Ensure all invalid patterns return proper error messages
- Add regression tests for all hanging patterns

## Root Cause Analysis

Likely causes:
1. Infinite loop or excessive backtracking in lexer when encountering unexpected closing braces
2. Parser attempting to parse nested parameter syntax indefinitely
3. Missing validation for brace matching before parsing

## Implementation Steps

1. Analyze lexer code to identify infinite loop conditions
2. Add pre-validation for brace matching
3. Implement proper error recovery for unmatched braces
4. Add guards against nested parameter parsing
5. Add comprehensive tests including timeout tests
6. Verify fix doesn't break existing valid patterns

## Test Files

- `/Tests/test-hanging-patterns.cs` - Reproduces the hanging patterns
- `/Tests/test-analyzer-patterns.cs` - Comprehensive pattern testing

## Success Criteria

- All previously hanging patterns return errors within milliseconds
- No regression in parsing valid patterns
- Clear error messages for invalid patterns
- Timeout protection in place as safety net
- All tests pass including new hanging pattern tests