# Fix Source Generator to Detect builder.AddTypeConverter() Calls

## Problem

The source generator fails to find custom type converters registered via `builder.AddTypeConverter()` method calls. It only detects converters registered in `ConfigureServices` blocks.

**Affected sample:**
- `samples/10-type-converters/02-custom-type-converters.cs` (fails with CS0103 errors)

**Root Cause:**
The source generator's custom converter detection logic in `route-matcher-emitter.cs` looks for converters in the `customConverters` parameter, which is populated from `ConfigureServices` registrations. However, converters can also be registered directly on the builder using `builder.AddTypeConverter()`, which is a valid and common pattern.

**Generated code shows the problem:**
```csharp
// WARNING: No converter found for type constraint 'EmailAddress'
// Register a converter with: builder.AddTypeConverter<YourConverter>();
void __handler(global::EmailAddress recipient, ...)
{
  // No conversion code generated!
}
__handler(recipient, ...);  // 'recipient' doesn't exist
```

The generator emits a warning comment but no actual conversion code, because it can't find the converter.

## Solution

Update the source generator to detect `builder.AddTypeConverter()` calls in the same file as route definitions.

### Files to Modify

1. **`source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`**
   - Currently extracts `ConfigureServices` for custom converters
   - Need to also extract `AddTypeConverter()` calls on the builder

2. **`source/timewarp-nuru-analyzers/generators/models/generator-model.cs`**
   - May need to add extraction from builder method calls

### Implementation Approach

1. **Add extraction for builder method calls:**
   - Find `AddTypeConverter` invocations on the builder variable
   - Extract the converter type from `new ConverterType()` argument
   - Populate `CustomConverterDefinition` list

2. **Pattern to detect:**
```csharp
builder.AddTypeConverter(new EmailAddressConverter());
builder.AddTypeConverter<EmailAddressConverter>();  // Also support generic form
```

3. **The extraction should find:**
   - Converter type name (from `new TypeName()` or `<TypeName>`)
   - Target type (from converter's `TargetType` property at compile time)
   - Constraint alias (if any)

### Technical Details

**Location:** `generators/extractors/app-extractor.cs`

**Current behavior:**
```csharp
// Only looks in ConfigureServices
var services = root.DescendantNodes()
  .OfType<InvocationExpressionSyntax>()
  .Where(n => n.Expression.ToString() == "ConfigureServices")
  // ... extracts from ConfigureServices block
```

**New behavior needed:**
```csharp
// Also look for builder.AddTypeConverter() calls
var builderCalls = root.DescendantNodes()
  .OfType<InvocationExpressionSyntax>()
  .Where(n =>
    n.Expression is MemberAccessExpressionSyntax maes &&
    maes.Expression.ToString() == "builder" &&
    maes.Name.ToString() == "AddTypeConverter")
// ... extract converter types from these calls
```

## Checklist

- [ ] Add extraction for `builder.AddTypeConverter()` calls in app-extractor.cs
- [ ] Update generator-model.cs if needed to support builder-based converters
- [ ] Test that samples/10-type-converters/02-custom-type-converters.cs builds
- [ ] Verify samples/10-type-converters/01-builtin-types.cs still works
- [ ] Run verify-samples to confirm all pass

## Notes

Priority: high

This is not a sample issue - it's a source generator bug. The sample uses a valid API pattern that the generator should support.

The fix ensures the generator detects converters registered via:
1. `builder.ConfigureServices(s => s.Add<Converter>())` (DI pattern)
2. `builder.AddTypeConverter(new Converter())` (direct registration)
3. `builder.AddTypeConverter<Converter>()` (generic form)

This is the proper fix - making the generator support all valid registration patterns.
