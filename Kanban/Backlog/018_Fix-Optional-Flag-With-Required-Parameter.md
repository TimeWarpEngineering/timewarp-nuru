# 018 Fix Optional Flag With Required Parameter

## Description

The parser incorrectly handles the pattern `--flag? {param}` (optional flag with required parameter). When no arguments are provided, the parser throws an error instead of matching the route with a null parameter value.

**Discovered through:** Dogfooding Nuru in run-kijaribu-tests.cs

## Current Behavior

```csharp
builder.AddRoute("--tag? {tag}", (string? tag) => RunTests(tag), "...");
```

When invoked with **no arguments**:
```
Error executing handler: No value provided for required parameter 'tag'
```

## Expected Behavior

According to the design specification, `--flag? {param}` should work as:
- No args provided → route matches, `param = null`
- `--flag value` provided → route matches, `param = "value"`

The optional modifier on the flag (`?`) should make the entire option optional, including its parameter.

## Current Workaround

Using two separate routes:
```csharp
builder.AddDefaultRoute(() => RunTests(null), "Run all tests");
builder.AddRoute("--tag {tag}", (string tag) => RunTests(tag), "Filter by tag");
```

## Requirements

- [ ] Fix parser to correctly handle `--flag? {param}` pattern
- [ ] When flag is omitted, parameter receives null (nullable type)
- [ ] When flag is present, parameter receives the provided value
- [ ] Update tests to validate this pattern works
- [ ] Remove workaround from run-kijaribu-tests.cs

## Test Cases

1. **Pattern**: `--tag? {tag}` with no args → Should match, `tag = null`
2. **Pattern**: `--tag? {tag}` with `--tag Lexer` → Should match, `tag = "Lexer"`
3. **Pattern**: `--verbose? --output? {file}` with no args → Should match, `file = null`
4. **Pattern**: `--verbose? --output? {file}` with `--output test.txt` → Should match, `file = "test.txt"`

## Related Files

- `Tests/Scripts/run-kijaribu-tests.cs` - Contains workaround and TODO comment
- `Source/TimeWarp.Nuru.Parsing/` - Parser logic that needs fixing
- `documentation/developer/design/design/parser/syntax-rules.md` - Design spec for optional syntax

## Notes

This is a critical usability issue - optional flags should naturally support optional presence. The current behavior forces users to create multiple routes for what should be a single route pattern.

The bug was discovered while dogfooding Nuru for the test runner's CLI argument parsing, demonstrating the value of using our own framework in real scenarios.
