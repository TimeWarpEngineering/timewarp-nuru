# Fix attributed route generator for optional typed options with defaults

## Description

When an `[Option]` property has a default value (e.g., `public int Replicas { get; set; } = 1;`), the generator should use that default when the option is not provided. Currently it errors with "Invalid value '(missing)' for option '--replicas'. Expected: int".

## Bug Analysis

**Root cause:** The generator only checks `ParameterIsOptional` (nullable annotation) to determine if an option value is optional. It doesn't consider property initializers.

**Current behavior:**
- `public int Replicas { get; set; } = 1;` → `ParameterIsOptional = false` (not nullable)
- Generated code requires the value and errors when not provided

**Expected behavior:**
- Properties with default values should be treated as optional
- When option not provided, use the property's default value

## Files to modify

1. **`segment-definition.cs`** - Add `DefaultValue` field to `OptionDefinition`
2. **`attributed-route-extractor.cs`** - Extract property initializer value
3. **`route-matcher-emitter.cs`** - Use default value when option not provided

## Checklist

- [ ] Add `DefaultValue` property to `OptionDefinition` record
- [ ] Extract property initializer in `ExtractOptionFromAttribute`
- [ ] Modify `EmitOptionTypeConversion` to use default when option not provided
- [ ] Test: `deploy dev` should use default `Replicas = 1`
- [ ] Test: `deploy dev --replicas 5` should use provided value
- [ ] Run generator-11 tests - all 6 should pass

## Notes

Generated code currently (lines 145-152 of NuruGenerated.g.cs):
```csharp
int replicas = default;
if (__replicas_raw is null || !int.TryParse(__replicas_raw, ..., out replicas))
{
  app.Terminal.WriteLine($"Error: Invalid value ...");
  return 1;  // FAILS when option not provided!
}
```

Should be:
```csharp
int replicas = 1;  // Use property default
if (__replicas_raw is not null && !int.TryParse(__replicas_raw, ..., out replicas))
{
  app.Terminal.WriteLine($"Error: Invalid value ...");
  return 1;  // Only error if value provided but invalid
}
```

Key insight: Non-nullable types with default values should behave like optional - use default when not provided, only error if provided but unparseable.
