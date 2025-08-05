# Parser Hanging Bug Report

## Issue
The RoutePatternParser hangs indefinitely on certain malformed patterns containing unmatched or nested braces.

## Affected Patterns

### 1. Closing brace without opening brace
- `build config}` - HANGS
- `deploy }` - HANGS

### 2. Nested braces
- `test {a{b}}` - HANGS

## Non-Hanging Invalid Patterns (Correctly Handled)
- `test {` - Returns error properly
- `build --config {` - Returns error properly  
- `test {param` - Returns error properly
- `{}` - Returns error properly

## Impact
- Parser becomes unresponsive, requiring process termination
- No timeout protection in parser
- Could cause IDE/tool freezes when analyzing invalid code

## Root Cause
Likely infinite loop or excessive backtracking in lexer/parser when encountering:
1. Unexpected closing braces
2. Nested parameter syntax

## Recommendation
1. Add timeout protection in parser
2. Fix lexer to handle unmatched closing braces
3. Prevent nested brace parsing
4. Add these patterns to test suite after fix

## Test File
See `/Tests/test-hanging-patterns.cs` for reproduction