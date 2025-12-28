# #293-005: Convert AddTypeConverter() to No-Op

## Parent

#293 Make DSL Builder Methods No-Ops

## Description

Convert `NuruCoreAppBuilder.AddTypeConverter()` from registering converters in a runtime registry to a no-op. The source generator handles type conversion at compile time.

## File

`source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs`

## Checklist

- [ ] `AddTypeConverter(IRouteTypeConverter)` → no-op, return `(TSelf)this` (line 98-103)
- [ ] Add comment explaining source gen handles type conversion
- [ ] Verify `TypeConverterRegistry` is not used elsewhere at runtime

## Current Implementation

```csharp
public virtual TSelf AddTypeConverter(IRouteTypeConverter converter)
{
  ArgumentNullException.ThrowIfNull(converter);
  TypeConverterRegistry.RegisterConverter(converter);
  return (TSelf)this;
}
```

## Target Implementation

```csharp
public virtual TSelf AddTypeConverter(IRouteTypeConverter converter)
{
  // Source generator handles type conversion at compile time.
  // Custom converters are registered via attributes or DSL analysis.
  _ = converter;
  return (TSelf)this;
}
```

## Related Dead Code

After this change, these may become dead code (verify in #293-006):
- `TypeConverterRegistry` class/field
- `IRouteTypeConverter` interface (if only used at runtime)

## Notes

- Small, isolated change
- Can be done in parallel with other tasks
- The source generator already handles built-in types (int, bool, DateTime, etc.)
- Custom type converters would need source generator support to work (separate concern)
