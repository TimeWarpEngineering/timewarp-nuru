# Split parser.cs into partial files

## Description

The `parser.cs` file (628 lines) is already declared as a partial class but doesn't use additional partial files. The segment parsing, validation, and token navigation concerns should be extracted into separate partials.

**Location:** `source/timewarp-nuru-parsing/parsing/parser/parser.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [x] Create `parser.segments.cs` - Segment parsing methods
- [x] Create `parser.validation.cs` - Type and identifier validation
- [x] Create `parser.navigation.cs` - Token navigation helpers

### Documentation
- [x] Add `<remarks>` to main file listing all partial files
- [x] Add XML summary to each new partial file

### Verification
- [x] All parser tests pass
- [x] Build succeeds

## Notes

### Proposed Split

| New File | ~Lines | Content |
|----------|--------|---------|
| `.segments.cs` | ~220 | `ParseLiteral()`, `ParseParameter()`, `ParseOption()`, `ParseEndOfOptions()` |
| `.validation.cs` | ~80 | `IsBuiltInType()`, `IsValidTypeConstraint()`, `IsValidIdentifierFormat()`, `IsValidIdentifier()` |
| `.navigation.cs` | ~70 | `Match()`, `Check()`, `Advance()`, `Peek()`, `Previous()`, `Consume()`, `Synchronize()` |
| Main file | ~260 | `Parse()`, error handling, AST construction orchestration |

### Type Validation

The `IsBuiltInType()` method uses a switch expression. Consider using `FrozenSet<string>` for better maintainability:
```csharp
private static readonly FrozenSet<string> BuiltInTypes = new[]
{
    "int", "string", "bool", "decimal", ...
}.ToFrozenSet();
```

### Token Navigation

Token navigation methods are standard parser infrastructure that could potentially be shared via a base class, but extracting to partial is simpler.

### Reference Pattern

Follow established partial class conventions with XML documentation.
