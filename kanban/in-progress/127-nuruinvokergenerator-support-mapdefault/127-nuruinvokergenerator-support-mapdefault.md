# NuruInvokerGenerator: Support MapDefault invocations

## Description

The `NuruInvokerGenerator` source generator only detects `Map` invocations when scanning for delegate signatures to generate typed invokers. It does not detect `MapDefault` invocations, causing runtime errors when `MapDefault` handlers use delegate signatures that aren't also used by a `Map` call.

**Error observed:**
```
Error executing handler: No source-generated invoker found for signature '_Returns_Int'. 
Ensure the NuruInvokerGenerator source generator is running and the delegate signature is supported. 
Route: 
```

## Requirements

- `NuruInvokerGenerator` must detect both `Map` and `MapDefault` invocations
- Delegate signatures from `MapDefault` handlers must generate typed invokers
- Existing `Map` detection behavior must remain unchanged

## Checklist

### Implementation
- [ ] Update `IsMapInvocation` in `nuru-invoker-generator.cs` to detect `MapDefault`
- [ ] Verify `GetRouteWithSignature` handles `MapDefault` argument positions correctly
- [ ] Add test for `MapDefault` with `Func<int>` signature

### Verification
- [ ] Run analyzer tests
- [ ] Verify generated code includes `MapDefault` signatures

## Notes

The fix is straightforward - change line 60 in `source/timewarp-nuru-analyzers/analyzers/nuru-invoker-generator.cs`:

```csharp
// Before
return memberAccess.Name.Identifier.Text == "Map";

// After
return memberAccess.Name.Identifier.Text is "Map" or "MapDefault";
```

`MapDefault` takes only a handler and optional description (no pattern argument), so `GetRouteWithSignature` may need adjustment since it expects a pattern as the first argument. The handler argument extraction logic should handle this case.

File location: `source/timewarp-nuru-analyzers/analyzers/nuru-invoker-generator.cs`
