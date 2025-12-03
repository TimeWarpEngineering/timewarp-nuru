# Wire Invokers to DelegateExecutor

## Description

Replace DynamicInvoke in DelegateExecutor with generated typed invokers. This is the integration point where the source-generated code connects to the runtime execution path.

## Parent

008-implement-source-generators-for-reflection-free-routing

## Dependencies

- 008b-generate-typed-invoker-methods

## Requirements

- Create registry mapping routes to generated invokers
- Modify DelegateExecutor to use registry lookup
- Fall back to DynamicInvoke if no generated invoker exists (for backwards compatibility)
- Ensure all existing tests still pass
- Maintain API compatibility

## Checklist

### Implementation
- [x] Generate route-to-invoker registry (`InvokerRegistry`)
- [x] Create lookup mechanism by signature key (`ComputeSignatureKey`)
- [x] Modify DelegateExecutor to check registry first
- [x] Implement DynamicInvoke fallback for unregistered routes
- [x] Handle async invoker coordination with existing async flow

### Generated Registry Shape
```csharp
// Source generator produces module initializer that calls:
InvokerRegistry.RegisterSyncBatch(GeneratedRouteInvokers.SyncInvokers);
InvokerRegistry.RegisterAsyncInvokerBatch(GeneratedRouteInvokers.AsyncInvokers);
```

### Testing
- [x] All existing routing tests pass (117/117)
- [x] InvokerRegistry unit tests pass (12/12)
- [x] Verify fallback to DynamicInvoke works

## Implementation Details

### Files Created
- `source/timewarp-nuru-core/execution/invoker-registry.cs` - Static registry with:
  - `SyncInvoker` and `AsyncInvoker` delegate types
  - `RegisterSync`, `RegisterSyncBatch`, `RegisterAsyncInvoker`, `RegisterAsyncInvokerBatch` methods
  - `TryGetSync`, `TryGetAsyncInvoker` lookup methods
  - `ComputeSignatureKey(MethodInfo)` - computes signature key at runtime to match source generator
- `tests/timewarp-nuru-core-tests/invoker-registry-01-basic.cs` - Unit tests for registry

### Files Modified
- `source/timewarp-nuru-core/execution/delegate-executor.cs` - Now looks up generated invokers before DynamicInvoke
- `source/timewarp-nuru-analyzers/analyzers/nuru-invoker-generator.cs` - Added `[ModuleInitializer]` generation

### Signature Key Format
Keys match between source generator and runtime:
- `NoParams` - Action with no parameters
- `String` - Action<string>
- `Int_Int_Returns_Int` - Func<int, int, int>
- `String_Returns_Task` - Func<string, Task>
- `NullableInt` - Action<int?>
- `StringArrayArray` - Action<string[]>

## Notes

The module initializer automatically registers all generated invokers at assembly load time. DelegateExecutor checks the registry first and falls back to DynamicInvoke for any unregistered signatures.

## Test Results (2025-12-04)

```
========================================
SUMMARY
========================================
Completion      26/26 (100.0%)
Factory         1/1 (100.0%)
Lexer           15/15 (100.0%)
MCP             6/6 (100.0%)
Parser          15/15 (100.0%)
Repl            35/35 (100.0%)
Routing         18/18 (100.0%)
TypeConversion  1/1 (100.0%)

Total: 117/117 tests passed (100.0%) in 616.0s
```

InvokerRegistry unit tests: 12/12 passed
