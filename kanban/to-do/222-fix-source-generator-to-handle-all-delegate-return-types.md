# Fix source generator to handle all delegate return types

## Description

The `NuruInvokerGenerator` source generator fails to generate invokers for delegates that return values (e.g., `Func<int>`). This causes runtime errors like:

```
Error executing handler: No source-generated invoker found for signature '_Returns_Int'. 
Ensure the NuruInvokerGenerator source generator is running and the delegate signature is supported.
```

The source generator has access to the full syntax tree - it should be able to detect and generate invokers for ANY delegate signature, not just a manually-curated list. We shouldn't have to discover missing signatures one by one.

## Checklist

- [ ] Investigate why `_Returns_Int` signature isn't being detected/generated
- [ ] Ensure generator handles all return types (int, string, bool, custom types, etc.)
- [ ] Ensure generator handles async return types (Task<T>, ValueTask<T>)
- [ ] Add tests for various return type signatures
- [ ] Verify calc-createbuilder.cs works with `return 0` restored

## Notes

**Reproduction:**
```csharp
builder.Map("")
  .WithHandler(() =>
  {
    WriteLine("Calculator - use --help for available commands");
    return 0;  // This causes the error
  })
  .AsQuery()
  .Done();
```

**Workaround:** Changed to return void (removed `return 0`).

**Root cause:** The source generator in `source/timewarp-nuru-analyzers/analyzers/nuru-invoker-generator*.cs` isn't extracting the signature properly for lambdas with return values.
