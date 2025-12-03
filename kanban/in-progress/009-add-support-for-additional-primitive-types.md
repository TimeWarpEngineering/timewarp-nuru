# Add Support for Additional Primitive Types

## Summary

Add support for remaining .NET primitive types: byte, sbyte, short, ushort, uint, ulong, float, and char.

## Todo List

- [ ] Add type converters for numeric types (byte, sbyte, short, ushort, uint, ulong, float)
- [ ] Add type converter for char
- [ ] Update `DefaultTypeConverters.GetTypeForConstraint()` with new type mappings
- [ ] Update `DefaultTypeConverters.TryConvert()` to handle new types
- [ ] Update NURU004 diagnostic message to include new supported types
- [ ] Add tests for each new type converter
- [ ] Update documentation with new supported types

## Notes

### Currently Supported Types
- string
- int (Int32)
- long (Int64)
- double
- decimal
- bool
- DateTime
- Guid
- TimeSpan

### Types Being Added
- **Numeric Types:**
  - byte (Byte)
  - sbyte (SByte)
  - short (Int16)
  - ushort (UInt16)
  - uint (UInt32)
  - ulong (UInt64)
  - float (Single)
  
- **Character Type:**
  - char

### Implementation Locations
- Type converters: `/Source/TimeWarp.Nuru/TypeConversion/Converters/`
- DefaultTypeConverters: Update `GetTypeForConstraint()` and `TryConvert()`
- Diagnostic NURU004: Update supported types list