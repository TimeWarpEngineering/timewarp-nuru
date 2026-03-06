# Fix Unit type ambiguity in source generator - use fully qualified TimeWarp.Nuru.Unit

## Description

The Nuru source generator emits `Unit` as an unqualified type name in generated code, which causes ambiguity when `Mediator.Unit` is also in scope. The generator should always emit `global::TimeWarp.Nuru.Unit` to avoid conflicts with other packages that define their own `Unit` type.

## Checklist

- [ ] Create a test that reproduces the bug
  - [ ] Test app that references both TimeWarp.Nuru and Mediator packages
  - [ ] Define a route with a handler returning `Unit`
  - [ ] Verify generated code uses fully qualified `global::TimeWarp.Nuru.Unit`
  - [ ] Test should fail initially, proving the bug exists
- [ ] Fix the root cause in `GetUnwrappedReturnTypeName()` 
  - [ ] Location: `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` lines 268-280
  - [ ] Currently strips `global::TimeWarp.Nuru.Unit` to just `"Unit"`
  - [ ] Should preserve fully qualified name for `Unit` type specifically
- [ ] Review and fix other potential emission sites
  - [ ] `EmitExpressionBodyHandler` line 128 - uses `GetUnwrappedReturnTypeName()`
  - [ ] `EmitBlockBodyHandler` line 179 - uses `GetUnwrappedReturnTypeName()`
  - [ ] `EmitMethodInvocation` line 559-564 - uses `GetUnwrappedReturnTypeName()`
- [ ] Verify the string-based pattern matching is correct
  - [ ] `GetOutputStrategy()` line 685 - already handles both `"global::TimeWarp.Nuru.Unit"` and `"Unit"`
  - [ ] `IsKnownValueType()` line 754 - already handles both forms
- [ ] Run all tests to ensure no regressions
- [ ] Commit the fix

## Notes

### Root Cause Analysis

The bug is in `GetUnwrappedReturnTypeName()` which strips the namespace from type names:

```csharp
private static string GetUnwrappedReturnTypeName(HandlerDefinition handler)
{
  if (handler.IsAsync && handler.ReturnType.UnwrappedTypeName is not null)
  {
    string unwrapped = handler.ReturnType.UnwrappedTypeName;
    int lastDot = unwrapped.LastIndexOf('.');
    return lastDot >= 0 ? unwrapped[(lastDot + 1)..] : unwrapped;  // BUG: strips to "Unit"
  }
  return handler.ReturnType.ShortTypeName;
}
```

For `global::TimeWarp.Nuru.Unit`, this returns just `"Unit"`, which gets emitted as the variable type in generated code:

```csharp
Unit result = await __handler.Handle(__command, ...);  // Ambiguous!
```

When `Mediator.Unit` is also in scope, the compiler cannot resolve which `Unit` to use.

### Fix Strategy

The fix should preserve the fully qualified name for `Unit` specifically, since it's a common type name that may conflict with other packages:

```csharp
private static string GetUnwrappedReturnTypeName(HandlerDefinition handler)
{
  if (handler.IsAsync && handler.ReturnType.UnwrappedTypeName is not null)
  {
    string unwrapped = handler.ReturnType.UnwrappedTypeName;
    
    // Preserve fully qualified name for Unit to avoid ambiguity with Mediator.Unit, etc.
    if (unwrapped == "global::TimeWarp.Nuru.Unit")
      return unwrapped;
    
    int lastDot = unwrapped.LastIndexOf('.');
    return lastDot >= 0 ? unwrapped[(lastDot + 1)..] : unwrapped;
  }
  return handler.ReturnType.ShortTypeName;
}
```

### Test Strategy

Create a test app that:
1. References both `TimeWarp.Nuru` and `Mediator` (or a mock package with `Unit` type)
2. Defines a route with a handler returning `Unit`
3. Verifies the generated code compiles without ambiguity errors
4. Verifies the generated code uses `global::TimeWarp.Nuru.Unit` explicitly

### Related Code Paths

The type name flows through:
1. **Extraction** (`handler-extractor.cs`) - Uses `SymbolDisplayFormat.FullyQualifiedFormat` ✅
2. **Model** (`HandlerReturnType`) - Stores both `FullTypeName` and `ShortTypeName` ✅
3. **Emission** (`handler-invoker-emitter.cs`) - Strips to short name for variable declarations ❌

The extraction is correct; the emission is the problem.
