# Add support for array of enums in repeated options

## Description

Implement support for `MyEnum[]` or `IEnumerable<MyEnum>` in repeated options (e.g., `--env Dev --env Staging`).

Currently, single enum parameters work correctly via `EnumTypeConverter<T>`, but repeated options with enum element types silently degrade to `string[]` instead of the typed enum array.

## Checklist

- [ ] Investigate `EmitRepeatedOptionTypeConversion` in route-matcher-emitter.cs (~line 1104)
- [ ] Add `RouteDefinition route` parameter usage (currently discarded with `_ = route;`)
- [ ] Check `route.Handler.Parameters` for `IsEnumType` matching the option name
- [ ] Emit `.Select(s => enumConverter.Convert(s)).ToArray()` style code for enum arrays
- [ ] Handle error cases (invalid enum values in array)
- [ ] Add unit tests for `MyEnum[]` in option position
- [ ] Add unit tests for `IEnumerable<MyEnum>` in option position
- [ ] Add unit tests for nullable enum arrays (`MyEnum[]?`)
- [ ] Add unit tests for error messages showing valid enum values
- [ ] Verify CI tests pass (clear runfile cache first)
- [ ] Update completion/REPL support if needed

## Notes

### Current State

**What works:**
- Single enum as positional parameter: `deploy {env}` ✅
- Single enum as option: `--env {value}` ✅
- Nullable enum (`MyEnum?`) ✅

**What doesn't work:**
- Array of enums (`MyEnum[]`) ❌ - falls back to `string[]`

### Key Code Locations

1. **`EmitRepeatedOptionTypeConversion`** (route-matcher-emitter.cs ~line 1104)
   - Handles type conversion for repeated options
   - Only handles built-in primitives via `TypeConversionMap.GetBuiltInTryConversion()`
   - Unknown types (including enums) fall through to `string[]` fallback
   - Has TODO comment: `// TODO: Add custom converter support for arrays`

2. **`EmitRepeatedValueOptionParsingWithIndexTracking`** (route-matcher-emitter.cs ~line 610)
   - Entry point for repeated option parsing
   - Line 617 explicitly notes: `// Note: route parameter is available for future enum support in repeated options`
   - The `route` parameter is discarded (`_ = route;`) as a placeholder

3. **`EmitOptionEnumTypeConversion`** (route-matcher-emitter.cs ~line 1449)
   - Handles single enum option conversion using `EnumTypeConverter<T>`
   - Can be used as reference for enum-specific error messages

### Reference: Single Enum Implementation

For single enums, the generator emits:
```csharp
var __enumConverter_env_0 = new global::TimeWarp.Nuru.EnumTypeConverter<MyEnum>();
if (!__enumConverter_env_0.TryConvert(rawValue, out object? __temp))
{
  app.Terminal.WriteLine($"Error: Invalid value '{rawValue}' for option '--env'. {__enumConverter_env_0.GetValidValuesMessage()}");
  return 1;
}
MyEnum env = (MyEnum)__temp!;
```

### Suggested Implementation Approach

For enum arrays, emit something like:
```csharp
var __enumConverter_envs_0 = new global::TimeWarp.Nuru.EnumTypeConverter<MyEnum>();
MyEnum[] envs;
try
{
  envs = __envs_list_0.Select(s =>
  {
    if (!__enumConverter_envs_0.TryConvert(s, out object? temp))
      throw new FormatException($"Invalid enum value '{s}'. {__enumConverter_envs_0.GetValidValuesMessage()}");
    return (MyEnum)temp!;
  }).ToArray();
}
catch (FormatException)
{
  app.Terminal.WriteLine($"Error: Invalid value in option '--env'. Expected: MyEnum");
  return 1;
}
```

### Test File Pattern

Follow existing test patterns from:
- `tests/timewarp-nuru-tests/routing/routing-24-enum-option-parameters.cs` (single enum options)
- Tests for repeated options (search for `IsRepeated` or `IsArray`)

### Related

- `ParameterBinding.IsEnumType` - flag indicating enum type parameter
- `EnumTypeConverter<T>` - runtime converter in `source/timewarp-nuru/type-conversion/converters/enum-type-converter.cs`
