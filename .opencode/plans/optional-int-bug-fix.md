# TimeWarp.Nuru Optional Int Option Bug Fix Implementation Plan

## Problem Summary

Commands with optional `int` options cause routing failures instead of skipping to next route candidate when option not provided. This prevents ANY command from working if there's a prior route with optional int option.

**Root Cause**: In `RouteMatcherEmitter.cs` lines 161-166, condition `if ({rawVarName} is null || !{tryParseCondition})` triggers error for optional options when not provided (rawVarName is null), causing route failure instead of skip.

## Affected Areas

### Primary Issue Location
- **File**: `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- **Method**: `EmitOptionTypeConversion`
- **Lines**: 161-166 (non-nullable value types)

### Additional Affected Type Conversions
1. **Line 1193**: Uri type conversion
2. **Line 1361**: Custom converters (required options) 
3. **Line 1409**: Enum converters (required options)

## Implementation Strategy

### Phase 1: Core Fix for Built-in Types
**Location**: Lines 147-167 in `EmitOptionTypeConversion`

**Current Logic (BROKEN)**:
```csharp
if (option.ParameterIsOptional || option.DefaultValueLiteral is not null)
{
  // Optional option value OR has default: declare with default, error only if value provided but invalid
  sb.AppendLine($"      {clrType} {varName} = {defaultValue};");
  sb.AppendLine($"      if ({rawVarName} is not null && !{tryParseCondition})");
  // ... error handling
}
else
{
  // Required option value (no default): TryParse with error message on failure
  sb.AppendLine($"      {clrType} {varName} = default;");
  sb.AppendLine($"      if ({rawVarName} is null || !{tryParseCondition})");  // BUG HERE
  // ... error handling
}
```

**Fixed Logic**:
```csharp
if (option.ParameterIsOptional || option.DefaultValueLiteral is not null)
{
  // Optional option value OR has default: declare with default, error only if value provided but invalid
  sb.AppendLine($"      {clrType} {varName} = {defaultValue};");
  sb.AppendLine($"      if ({rawVarName} is not null && !{tryParseCondition})");
  // ... error handling
}
else
{
  // Required option value (no default): TryParse with error message on failure ONLY when flag was found
  sb.AppendLine($"      {clrType} {varName} = default;");
  sb.AppendLine($"      if ({rawVarName} is not null && !{tryParseCondition})");  // FIXED: removed null check
  // ... error handling
}
```

**Key Change**: In the else block (required options), remove the null check. If rawVarName is null and the option is required, the earlier logic at lines 470-473 should have already handled skipping the route.

### Phase 2: Fix Uri Type Conversion
**Location**: Lines 1191-1198

**Current Logic**:
```csharp
sb.AppendLine($"      if ({rawVarName} is null || !global::System.Uri.TryCreate({rawVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {varName}) || {varName} is null)");
```

**Fixed Logic**: Separate null check from validation:
```csharp
sb.AppendLine($"      if ({rawVarName} is not null && (!global::System.Uri.TryCreate({rawVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {varName}) || {varName} is null))");
```

### Phase 3: Fix Custom Converter Type Conversions
**Location**: Lines 1359-1367 and 1407-1415

**Current Logic**:
```csharp
sb.AppendLine($"      if ({rawVarName} is null || !{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
```

**Fixed Logic**:
```csharp
sb.AppendLine($"      if ({rawVarName} is not null && !{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
```

### Phase 4: Comprehensive Testing

#### Test Scenarios to Create
1. **Optional int option not provided** - should skip to next route
2. **Optional int option with valid value** - should work
3. **Optional int option with invalid value** - should show error
4. **Multiple commands where first has optional int** - second should work
5. **All numeric types** (long, short, byte, double, float, decimal)
6. **Uri type** with optional behavior
7. **Custom converters** with optional behavior
8. **Enum types** with optional behavior

#### Test Structure
```csharp
// Test app demonstrating bug
NuruApp.CreateBuilder()
  .Map("deploy {env}").WithHandler((string env, int replicas = 1) => { /* handler */ }).AsCommand().Done()
  .Map("status").WithHandler(() => { /* handler */ }).AsCommand().Done()
  .Build();

// Test: ["deploy", "dev"] - should work but currently fails
// Test: ["status"] - should work but currently fails when deploy comes first
```

## Implementation Order

1. **Phase 1**: Fix core built-in type conversion logic
2. **Phase 2**: Fix Uri type conversion
3. **Phase 3**: Fix custom converter conversions  
4. **Phase 4**: Create comprehensive test suite
5. **Phase 5**: Run existing tests to ensure no regressions
6. **Phase 6**: Run new tests to verify fix

## Risk Assessment

**Low Risk**: 
- Changes are surgical and isolated to type validation logic
- Preserve existing error messages and behavior
- Only affects what happens when options are not provided

**Mitigation**:
- Comprehensive test suite covering all affected type families
- Run full test suite before and after changes
- Focus on ensuring existing functionality remains unchanged

## Success Criteria

1. ✅ Optional int options not provided no longer cause routing failures
2. ✅ Routes properly skip to next candidate when validation fails  
3. ✅ All existing tests continue to pass
4. ✅ New test suite covers all affected type families
5. ✅ Error messages remain clear and helpful for actual validation failures