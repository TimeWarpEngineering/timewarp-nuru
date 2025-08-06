# Add Support for Additional Primitive Types

## Overview
TimeWarp.Nuru currently supports a subset of .NET primitive types for route parameter type constraints. We should add support for the remaining primitive types for completeness.

## Currently Supported Types
- string
- int (Int32)
- long (Int64)
- double
- decimal
- bool
- DateTime
- Guid
- TimeSpan

## Missing Primitive Types to Add
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

## Implementation Notes
- Add type converters for each missing type in `/Source/TimeWarp.Nuru/TypeConversion/Converters/`
- Update `DefaultTypeConverters.GetTypeForConstraint()` to include new type mappings
- Update `DefaultTypeConverters.TryConvert()` to handle new types
- Update NURU004 diagnostic message to include new supported types
- Add tests for each new type converter

## Priority
Low - The current set of supported types covers the most common use cases. These additional types can be added as needed.