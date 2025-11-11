# Support Custom Type Converters and Add Common CLI Types

## Status: TODO
## Priority: High
## Category: Bug Fix + Feature Enhancement
## Related Issue: [#62](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/62)

## Problem

Custom type converters implementing `IRouteTypeConverter` cannot be used in route patterns with type constraints because the parser's `IsValidTypeConstraint()` method only allows a hardcoded list of built-in types.

### User Report (Issue #62)

A user created a custom `FileInfoTypeConverter`:

```csharp
public class FileInfoTypeConverter : IRouteTypeConverter
{
    public bool TryConvert(string value, out object? result)
    {
        try
        {
            result = new FileInfo(value);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    public Type TargetType => typeof(FileInfo);
    public string ConstraintName => "fileinfo";
}
```

They attempted to use it in a route pattern:

```csharp
.AddTypeConverter(new FileInfoTypeConverter())
.AddRoute("process {file:fileinfo}", (FileInfo file) => ...)
```

But the parser rejects `"fileinfo"` as an invalid type constraint.

### Design Inconsistency

The framework provides:
- ✅ `IRouteTypeConverter` interface with `ConstraintName` property
- ✅ `AddTypeConverter()` API to register custom converters
- ✅ Documentation showing how to create custom converters
- ✅ Runtime `TypeConverterRegistry` that works perfectly
- ❌ Parser that rejects custom constraint names

## Current Behavior (Broken)

**Parser.cs** (line 549-564):

```csharp
private static bool IsValidTypeConstraint(string type)
{
  return type switch
  {
    "string" => true,
    "int" => true,
    "long" => true,
    "double" => true,
    "decimal" => true,
    "bool" => true,
    "DateTime" => true,
    "Guid" => true,
    "TimeSpan" => true,
    _ => false  // ❌ Rejects ALL custom types
  };
}
```

Error message returned:
```
Error at position X: Invalid type constraint 'fileinfo' - supported types: string, int, double, bool, DateTime, Guid, long, decimal, TimeSpan
```

### Root Cause

The parser is **stateless** and validates patterns at parse time, but custom converters are registered at build time via `NuruAppBuilder`. The parser has no access to the `TypeConverterRegistry` and currently rejects anything not in the hardcoded list.

## Impact

### What Works Today ✅
```csharp
// Without type constraint - works but no validation
.AddRoute("process {file}", (FileInfo file) => ...)
```

### What's Broken ❌
```csharp
// With type constraint - parser rejects it
.AddRoute("process {file:fileinfo}", (FileInfo file) => ...)
```

### Affects
- Custom type converters for domain types
- Enum type converters (documented but can't use constraint syntax)
- Missing common CLI types like FileInfo, Uri, IPAddress

## Proposed Solution: Option C (Both Fixes)

### Part 1: Fix Parser to Accept Custom Types

**Update `IsValidTypeConstraint()`** to accept both built-in types AND any valid identifier:

```csharp
private static bool IsValidTypeConstraint(string type)
{
  // Accept known built-in types (for good error messages)
  if (IsBuiltInType(type)) return true;

  // Accept any valid identifier format for custom types
  return IsValidIdentifierFormat(type);
}

private static bool IsBuiltInType(string type) => type switch
{
  "string" or "int" or "long" or "double" or "decimal" or
  "bool" or "DateTime" or "Guid" or "TimeSpan" or
  "fileinfo" or "directoryinfo" or "uri" or "dateonly" or
  "timeonly" or "ipaddress" => true,
  _ => false
};

private static bool IsValidIdentifierFormat(string type)
{
  if (string.IsNullOrWhiteSpace(type)) return false;
  if (!char.IsLetter(type[0]) && type[0] != '_') return false;

  foreach (char c in type)
  {
    if (!char.IsLetterOrDigit(c) && c != '_') return false;
  }

  return true;
}
```

**Update error message** in `InvalidTypeConstraintError`:

```csharp
public override string ToString() =>
  $"Error at position {Position}: Invalid type constraint '{InvalidType}'. " +
  $"Built-in types: {SupportedTypes}. " +
  $"For custom types, ensure you register an IRouteTypeConverter and use a valid identifier name.";
```

### Part 2: Add Common Built-in Type Converters

Based on research of System.CommandLine, Cocona, and common CLI scenarios, add these Priority 1 types:

#### Essential File System Types
- **`FileInfo`** - File paths with metadata (requested in issue #62)
- **`DirectoryInfo`** - Directory paths with metadata

#### Network Types
- **`Uri`** - ⚠️ Already documented but NOT implemented (bug!)
- **`IPAddress`** - IPv4/IPv6 addresses

#### Modern .NET Date/Time (.NET 6+)
- **`DateOnly`** - Pure dates (already mentioned in SyntaxExamples.cs)
- **`TimeOnly`** - Pure time (already mentioned in SyntaxExamples.cs)

#### Additional Numeric Types (see Task 009)
- Defer to separate task for completeness

## Implementation Plan

### Phase 1: Fix Parser (Unblocks Custom Types)

**Files to Update:**
1. `/Source/TimeWarp.Nuru.Parsing/Parsing/Parser/Parser.cs`
   - Add `IsBuiltInType()` helper method
   - Add `IsValidIdentifierFormat()` helper method
   - Update `IsValidTypeConstraint()` to use both helpers (lines 549-564)

2. `/Source/TimeWarp.Nuru.Parsing/Parsing/Parser/ParseError.cs`
   - Update `InvalidTypeConstraintError.ToString()` message (lines 57-67)

3. `/Source/TimeWarp.Nuru.Analyzers/Analyzers/DiagnosticDescriptors.cs`
   - Update NURU004 diagnostic description if needed

**Tests to Add:**
- Test custom type converter with type constraint
- Test valid identifier formats (alphanumeric, underscore)
- Test invalid identifier formats (starts with number, special chars)
- Test case sensitivity

### Phase 2: Add Built-in Type Converters

**Files to Update:**
1. `/Source/TimeWarp.Nuru/TypeConversion/DefaultTypeConverters.cs`
   - Add `TryConvertUri()` method
   - Add `TryConvertFileInfo()` method
   - Add `TryConvertDirectoryInfo()` method
   - Add `TryConvertIPAddress()` method
   - Add `TryConvertDateOnly()` method
   - Add `TryConvertTimeOnly()` method
   - Update main `TryConvert()` method to call new converters

2. `/Source/TimeWarp.Nuru/TypeConversion/TypeConverterRegistry.cs`
   - Ensure new types are registered with correct constraint names
   - Constraint names: `uri`, `fileinfo`, `directoryinfo`, `ipaddress`, `dateonly`, `timeonly`

3. `/Source/TimeWarp.Nuru.Parsing/Parsing/Parser/Parser.cs`
   - Update `IsBuiltInType()` to include new type names

**Implementation Notes:**
- Use `Uri.TryCreate()` with `UriKind.Absolute` for Uri
- FileInfo: Just construct, optionally validate existence with parameter
- DirectoryInfo: Just construct, optionally validate existence with parameter
- IPAddress: Use `IPAddress.TryParse()`
- DateOnly/TimeOnly: Use respective `TryParse()` methods
- Follow zero-allocation pattern from existing converters

**Tests to Add:**
- Test each new type converter with valid inputs
- Test each with invalid inputs (returns false, sets result to null)
- Test nullable variants (`FileInfo?`, `Uri?`, etc.)
- Test arrays (`FileInfo[]`, `Uri[]`, etc.) per Task 007
- Test in both Direct and Mediator routing approaches

### Phase 3: Update Documentation

**Files to Update:**
1. `/documentation/user/reference/supported-types.md`
   - Add Uri implementation (currently documented but missing!)
   - Add FileInfo with examples
   - Add DirectoryInfo with examples
   - Add IPAddress with examples
   - Add DateOnly with examples
   - Add TimeOnly with examples
   - Update custom type converter section to mention type constraints work

2. `/readme.md`
   - Update type count if prominently displayed
   - Add note about custom type support

**Example Documentation:**

```markdown
### FileInfo

File paths with metadata access.

```csharp
.AddRoute("process {file:fileinfo}", (FileInfo file) =>
{
    Console.WriteLine($"File: {file.Name}");
    Console.WriteLine($"Size: {file.Length} bytes");
    Console.WriteLine($"Exists: {file.Exists}");
});
```

```bash
$ myapp process document.txt
File: document.txt
Size: 1024 bytes
Exists: True
```
```

### Phase 4: Add Sample Application

**New Sample:**
- `/Samples/FileProcessing/` or similar
- Demonstrate FileInfo, DirectoryInfo, Uri usage
- Show custom type converter with type constraint
- Include both Direct and Mediator examples

### Phase 5: Update Analyzer (if needed)

**Considerations:**
- Analyzer might need to treat unknown types as warnings, not errors
- Or accept them silently (validation happens at runtime)
- Update analyzer tests

## Files to Update Summary

### Core Changes
- `/Source/TimeWarp.Nuru.Parsing/Parsing/Parser/Parser.cs`
- `/Source/TimeWarp.Nuru.Parsing/Parsing/Parser/ParseError.cs`
- `/Source/TimeWarp.Nuru/TypeConversion/DefaultTypeConverters.cs`
- `/Source/TimeWarp.Nuru/TypeConversion/TypeConverterRegistry.cs`

### Analyzer Changes
- `/Source/TimeWarp.Nuru.Analyzers/Analyzers/DiagnosticDescriptors.cs`
- `/Source/TimeWarp.Nuru.Analyzers/Analyzers/NuruRouteAnalyzer.cs`

### Documentation
- `/documentation/user/reference/supported-types.md`
- `/readme.md`

### Tests
- `/Tests/TimeWarp.Nuru.Tests/` (add custom type converter tests)
- `/Tests/TimeWarp.Nuru.Analyzers.Tests/` (update analyzer tests)

### Samples
- `/Samples/FileProcessing/` or appropriate location

## Success Criteria

### Parser Fix
- [ ] Parser accepts valid identifier formats for type constraints
- [ ] Built-in types still validated and get good error messages
- [ ] Invalid identifiers (start with number, special chars) rejected
- [ ] Error message mentions custom type converters
- [ ] Analyzer updated to handle unknown types gracefully

### Type Converters Added
- [ ] Uri converter implemented and working
- [ ] FileInfo converter implemented and working
- [ ] DirectoryInfo converter implemented and working
- [ ] IPAddress converter implemented and working
- [ ] DateOnly converter implemented and working
- [ ] TimeOnly converter implemented and working

### Testing
- [ ] Custom type converter with constraint works in tests
- [ ] Each new built-in type tested with valid inputs
- [ ] Each new built-in type tested with invalid inputs
- [ ] Nullable variants tested
- [ ] Array variants tested (per Task 007)
- [ ] Both Direct and Mediator approaches tested

### Documentation
- [ ] Uri documentation added (fixing documentation bug)
- [ ] FileInfo documentation added
- [ ] DirectoryInfo documentation added
- [ ] IPAddress documentation added
- [ ] DateOnly documentation added
- [ ] TimeOnly documentation added
- [ ] Custom type converter docs updated to show constraint usage
- [ ] README updated if needed

### Samples
- [ ] Sample application demonstrating new types
- [ ] Sample showing custom type converter with constraint

### Issue Resolution
- [ ] Issue #62 resolved
- [ ] Response posted to issue with solution details

## Related Tasks

- **Task 009**: Add-Support-for-Additional-Primitive-Types (byte, short, uint, etc.)
- **Task 007**: Add-Typed-Array-Support-for-Catch-All-Parameters

## Breaking Changes

**None** - This is backward compatible:
- Existing route patterns continue to work
- Validation is relaxed (accepts more), not tightened
- New type converters are additive

## Benefits

### For Users
- ✅ Can use custom type converters with type constraint syntax
- ✅ Get common CLI types out of the box (FileInfo, Uri, etc.)
- ✅ Framework feels complete and professional
- ✅ Better than System.CommandLine in some aspects

### For Framework
- ✅ Resolves design inconsistency
- ✅ Honors documented API contract
- ✅ Competitive feature parity with other CLI frameworks
- ✅ Unblocks real-world use cases

## Timeline Estimate

- **Phase 1 (Parser Fix)**: 2-4 hours
- **Phase 2 (Type Converters)**: 4-6 hours
- **Phase 3 (Documentation)**: 2-3 hours
- **Phase 4 (Sample)**: 2-3 hours
- **Phase 5 (Testing)**: 3-4 hours

**Total Estimate**: 13-20 hours

**Complexity**: Medium (multiple coordinated changes but each is straightforward)

## Priority Justification

**High Priority** because:
1. Blocks real users (Issue #62 is active)
2. Design inconsistency undermines framework credibility
3. Missing documented feature (Uri is documented but not implemented)
4. Common types (FileInfo) are table stakes for CLI frameworks
5. Relatively quick to implement with high user value
