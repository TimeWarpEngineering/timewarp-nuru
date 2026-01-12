# Bug: Typed repeated options not converted from string array

## Description

When the source generator processes route patterns with typed repeated options (e.g., `{id:int}*`), the generated code passes `string[]` directly to handlers expecting typed arrays like `int[]`, `double[]`, `bool[]`, etc., causing CS1503 type conversion errors.

## Reproduction

**Files affected:** Multiple routing tests in `tests/timewarp-nuru-core-tests/routing/`

**Example patterns:**
```csharp
.Map("process --id {id:int}*").WithHandler((int[] id) => ...)
.Map("calc --values {v:double}*").WithHandler((double[] v) => ...)
.Map("flags --enable {f:bool}*").WithHandler((bool[] f) => ...)
.Map("dates --when {d:DateTime}*").WithHandler((DateTime[] d) => ...)
```

**Errors:**
```
error CS1503: Argument 1: cannot convert from 'string[]' to 'int[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'double[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'bool[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'DateTime[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'byte[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'short[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'long[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'float[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'decimal[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'Guid[]'
```

## Expected Behavior

The generator should emit type conversion code for each element in the array:
```csharp
// Collect string values
List<string> idStrings = new();
// ... collect from args ...

// Convert to typed array
int[] boundId = idStrings.Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
```

Or use a helper method:
```csharp
int[] boundId = ConvertArray<int>(idStrings, int.Parse);
```

## Checklist

- [ ] Identify where repeated option values are collected
- [ ] Add type conversion step before passing to handler
- [ ] Support all primitive types: int, double, bool, DateTime, byte, short, long, float, decimal, Guid
- [ ] Consider using existing TypeConverter infrastructure
- [ ] Test with various typed array patterns
- [ ] Verify affected routing tests compile and pass

## Notes

- Single typed options work correctly (type conversion exists)
- The bug is specific to repeated/array options with type constraints
- The `*` modifier indicates repeated option
- Related tests in `routing-06-repeated-options.cs` and `routing-17-additional-primitive-types.cs`

## Files to Investigate

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- Look for repeated option handling and compare with single option type conversion
