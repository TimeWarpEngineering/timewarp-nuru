# Update NuruInvokerGenerator for New API

## Description

Update the existing `NuruInvokerGenerator` to work with the new `Map(pattern).WithHandler(delegate).Done()` API instead of the old `Map(pattern, delegate)` syntax.

## Parent

151-implement-delegate-generation-phase-2

## Dependencies

- Task 192: Old API removed
- Task 193: New generator detection pattern established

## Checklist

### Detection Updates
- [ ] Change detection from `Map(pattern, handler)` to `WithHandler(handler)`
- [ ] Find `WithHandler()` calls on `EndpointBuilder`
- [ ] Extract delegate from `WithHandler()` argument
- [ ] Reuse detection logic from Task 193 if possible

### Signature Extraction
- [ ] Keep existing signature extraction logic (already works well)
- [ ] Ensure it handles both lambda and method group in new context

### Invoker Generation
- [ ] Keep existing invoker generation (unchanged)
- [ ] Invokers are still needed for typed delegate invocation

### Coordination with NuruDelegateCommandGenerator
- [ ] Ensure no duplicate detection/generation
- [ ] Option A: Share syntax provider results
- [ ] Option B: Each generator runs independently (simpler)
- [ ] Recommend Option B for now - generators are separate concerns

### Testing
- [ ] Verify invokers still generated correctly with new API
- [ ] Verify AOT compatibility maintained

## Archived

**Reason:** Superseded - this work was completed as part of Task 193.

Task 193's notes confirm: "**Approach taken:** Option A - Updated existing `NuruInvokerGenerator` rather than creating new generator."

The parent epic (151) also marks this as done: "Task 197: Invoker generator updates (done as part of Task 193)".

## Notes

### What NuruInvokerGenerator Does

It generates typed invoker methods that avoid `DynamicInvoke`:

```csharp
// Generated invoker for (string, bool) => void
public static object? Invoke_String_Bool_Void(Delegate handler, object?[] args)
{
    ((Action<string, bool>)handler)((string)args[0]!, (bool)args[1]!);
    return null;
}
```

This is still needed even with Command/Handler generation for:
- Direct delegate execution path (if kept)
- Fallback scenarios
- AOT compatibility

### Relationship to NuruDelegateCommandGenerator

- **NuruInvokerGenerator**: Generates typed invokers for delegate signatures
- **NuruDelegateCommandGenerator**: Generates Command/Handler classes from delegates

Both extract delegate info but for different purposes. They can run independently.
