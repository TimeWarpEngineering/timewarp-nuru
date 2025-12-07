# Fix Long Form Option With Alias And Description

## Problem
When an option has both a short alias and a description, the long form of the option fails to match, but the short form works correctly.

## Example Pattern
```csharp
.AddRoute("hello {name|Your name} --upper,-u|Convert to uppercase", ...)
```

## Current Behavior
- `hello World -u` → ✓ Works correctly (matches and executes)
- `hello World --upper` → ✗ Fails to match (shows help menu instead)

## Expected Behavior
Both long form (`--upper`) and short form (`-u`) should match when the option has an alias and description.

## Test Case
`/Tests/SingleFileTests/Features/test-desc.cs` - Test 1b demonstrates this bug

## Implementation Notes
The issue appears to be in the route matching logic when parsing options that have both:
1. A short alias (e.g., `-u`)
2. A description (e.g., `|Convert to uppercase`)

The parser likely needs to be fixed to correctly handle the long form when both are present.

## Acceptance Criteria
- [x] Long form (`--upper`) matches correctly when alias and description are present
- [x] Short form (`-u`) continues to work
- [x] Help output shows both forms with the description
- [x] test-desc.cs Test 1b passes

## Results

**Bug Fixed Successfully**

Testing confirms both long and short forms work correctly when an option has both an alias AND a description:

**Pattern:** `hello {name} --upper,-u|Convert to uppercase`
- `hello World -u` ✅ PASSES
- `hello World --upper` ✅ PASSES
- `hello World` (omitted) ✅ PASSES

**Pattern:** `deploy {env} --dry-run,-d|Preview mode`
- `deploy prod -d` ✅ PASSES
- `deploy prod --dry-run` ✅ PASSES

Created test file `routing-18-option-alias-with-description.cs` with 5 tests - all passing.

The original bug where long form failed with alias+description is now fixed.