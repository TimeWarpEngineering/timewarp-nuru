# Fix Source Generator Type Conversion Bug for Uri/FileInfo/DirectoryInfo

## Status: COMPLETED ✅

## Problem

The source generator failed to generate type conversion code for `Uri`, `FileInfo`, and `DirectoryInfo` type constraints, causing CS0103 errors (undefined variables) at compile time.

## Root Causes

### Bug 1: Missing type conversion code generation
**Location:** `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

`TypeConversionMap.GetBuiltInTryConversion()` returns `null` for `uri`, `fileinfo`, `directoryinfo` because they don't have `TryParse` methods. The special handling was never implemented, so only a warning comment was emitted with no actual variable declaration.

### Bug 2: False positive closure detection for conditional member access (`?.`)
**Location:** `source/timewarp-nuru-analyzers/validation/handler-validator.cs`

The closure detector only handled `MemberAccessExpressionSyntax` (for `obj.property`) but NOT `MemberBindingExpressionSyntax` (for `obj?.property`). This caused false positives when accessing properties on nullable parameters like `url?.Host`.

## Solution

### Fix 1: Added special-case handling in `route-matcher-emitter.cs`

Added handling for Uri/FileInfo/DirectoryInfo in both:
- `EmitTypeConversions()` - for positional parameters
- `EmitOptionTypeConversion()` - for option parameters

Patterns used:
- **Uri**: `Uri.TryCreate()` with `UriKind.RelativeOrAbsolute`
- **FileInfo/DirectoryInfo**: Constructor with `try/catch (ArgumentException)`

### Fix 2: Added `MemberBindingExpressionSyntax` check in `handler-validator.cs`

```csharp
// Skip if it's the name part of a conditional member access (obj?.name)
if (identifier.Parent is MemberBindingExpressionSyntax mb && mb.Name == identifier)
  continue;
```

## Files Modified

| File | Changes |
|------|---------|
| `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` | Added Uri/FileInfo/DirectoryInfo handling in `EmitTypeConversions()` and `EmitOptionTypeConversion()` |
| `source/timewarp-nuru-analyzers/validation/handler-validator.cs` | Added `MemberBindingExpressionSyntax` check for conditional member access |

## New Test File

**File:** `tests/timewarp-nuru-tests/routing/routing-23-uri-fileinfo-directoryinfo.cs`

18 tests covering:
- Uri, FileInfo, DirectoryInfo positional parameters (required and optional)
- Uri, FileInfo, DirectoryInfo option parameters (required and optional)
- Mixed type combinations
- Case-insensitive constraint names
- Proper property access (e.g., `url.Scheme`, `file.Name`, `dir?.Name`)

## Verification Results

### Sample runs successfully:
```
$ dotnet run samples/10-type-converters/01-builtin-types.cs -- fetch https://example.com/api/data
🌐 Fetching from https://example.com/api/data
   Scheme: https
   Host: example.com
   Path: /api/data

$ dotnet run samples/10-type-converters/01-builtin-types.cs -- list /tmp
📁 Directory: tmp
   Full path: /tmp
   Parent: /
   Exists: True
   Contains: 231 files, 727 directories

$ dotnet run samples/10-type-converters/01-builtin-types.cs -- read /etc/passwd
📄 File: passwd
   Full path: /etc/passwd
   Directory: /etc
   Extension: 
   Exists: True
   Size: 1,440 bytes
   Last modified: 01/13/2025 14:20:07
```

### Test results:
- New test file: **18 tests pass**
- CI tests: **984 total, 978 passed, 6 skipped** - no regressions

## Checklist

- [x] Add Uri type conversion handling in EmitTypeConversions()
- [x] Add FileInfo type conversion handling in EmitTypeConversions()
- [x] Add DirectoryInfo type conversion handling in EmitTypeConversions()
- [x] Add Uri/FileInfo/DirectoryInfo handling in EmitOptionTypeConversion()
- [x] Fix false positive closure detection for `?.` operator
- [x] Create routing-23-uri-fileinfo-directoryinfo.cs test file
- [x] Verify samples/10-type-converters/01-builtin-types.cs builds and runs
- [x] Run CI tests to ensure no regressions
