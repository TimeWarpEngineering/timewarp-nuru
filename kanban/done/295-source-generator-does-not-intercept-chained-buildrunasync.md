# Source Generator Does Not Intercept Chained .Build().RunAsync()

**Priority:** medium  
**Tags:** bug, generator, interceptor

## Description

The source generator interceptor does not find the `RunAsync()` call when it's chained directly to `.Build()`:

```csharp
// NOT intercepted (bug):
return await NuruApp.CreateBuilder(args)
  .Map("hello")
    .WithHandler(() => "Hello")
    .Done()
  .Build()
  .RunAsync(args);

// Intercepted (works):
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("hello")
    .WithHandler(() => "Hello")
    .Done()
  .Build();

await app.RunAsync(args);
```

The chained pattern throws at runtime:
```
System.InvalidOperationException: RunAsync was not intercepted. Ensure the source generator is enabled.
```

## Checklist

- [x] Investigate how the syntax tree differs between chained and non-chained patterns
- [x] Check the DslInterpreter at line 668 (comment mentions "BuiltAppMarker - from chained .Build().RunAsync()")
- [x] Review interceptor attribute generation for finding chained call sites
- [x] Identify the root cause of missing interception
- [x] Implement fix for chained `.Build().RunAsync()` pattern
- [x] Add test cases for both chained and non-chained patterns
- [x] Verify fix works with different method chain lengths

## Results

**Status:** This bug was already fixed by Bug #298 (return await app.RunAsync not intercepted).

The fix in Bug #298 added handling for `ReturnStatementSyntax` in `DslInterpreter.ProcessStatement()`, which enables the generator to intercept `return await ... .Build().RunAsync(args)` patterns.

**Verified working:**
```csharp
return await NuruApp.CreateBuilder(args)
  .Map("hello")
    .WithHandler(() => WriteLine("Hello from chained pattern!"))
    .Done()
  .Build()
  .RunAsync(args);
```

- `InterceptsLocationAttribute` is generated ✓
- Routes are matched correctly ✓
- No runtime exception ✓

## Notes

**Investigation starting points:**
- The DslInterpreter has a comment at line 668 mentioning "BuiltAppMarker - from chained .Build().RunAsync()"
- The interceptor attribute generation may not be finding the chained call site
- The issue is likely in how the source generator walks the syntax tree to find `RunAsync()` invocations

**Related files to examine:**
- DslInterpreter (contains BuiltAppMarker handling)
- Interceptor generator (attribute generation logic)
- Syntax tree analysis code for finding call sites
