# Fix source generator nullable value type handling for Options (GH #146)

## Description

The Nuru source generator doesn't generate type conversion code for nullable value types (`long?`, `int?`, etc.) on `[Option]` properties.

**Current Behavior:** Generator produces direct string assignment for nullable types like `long?`, causing CS0029 compilation error:
```csharp
ExcludeInvoiceId = excludeinvoiceid,  // ERROR: Cannot convert string to long?
```

**Expected Behavior:** Generator should produce TryParse code for nullable value types, similar to non-nullable types:
```csharp
long? excludeinvoiceid = null;
if (__excludeinvoiceid_raw != null)
{
    if (long.TryParse(__excludeinvoiceid_raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
    {
        excludeinvoiceid = parsed;
    }
}
```

## Checklist

- [ ] Identify where option type conversion is generated in the source generator
- [ ] Add logic to detect nullable value types (Nullable<T>)
- [ ] Generate appropriate TryParse code for nullable types
- [ ] Add tests for nullable option types (long?, int?, bool?, etc.)

## Notes

- Reference: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/146
- Affected types: `long?`, `int?`, and other nullable value types on Options
- Non-nullable value types already work correctly with TryParse
