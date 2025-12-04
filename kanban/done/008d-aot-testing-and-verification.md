# AOT Testing and Verification

## Description

Verify AOT compilation works without warnings and test performance. This is the validation step that confirms the source generator implementation achieves its AOT compatibility goals.

## Parent

008-implement-source-generators-for-reflection-free-routing

## Dependencies

- 008c-wire-invokers-to-delegate-executor

## Requirements

- Build test app with PublishAot=true
- Verify no IL2XXX or IL3XXX trimming warnings
- Verify no DynamicInvoke calls remain for generated routes
- Benchmark performance vs reflection-based invocation
- Document AOT compatibility status

## Checklist

### AOT Compilation Verification
- [x] Create AOT test application with representative routes
- [x] Build with `<PublishAot>true</PublishAot>`
- [x] Capture and analyze all trimming warnings
- [x] Verify zero IL2XXX warnings (trimming)
- [x] Verify zero IL3XXX warnings (AOT)
- [x] Test application runs correctly after AOT publish

### DynamicInvoke Verification
- [x] Add compile-time check that generated invokers exist
- [x] Verify DynamicInvoke only used for explicit fallback scenarios
- [x] DelegateRequestHandler throws if no invoker found (no silent fallback)

### Performance Benchmarking
- [ ] Create benchmark comparing generated vs DynamicInvoke
- [ ] Measure invocation latency
- [ ] Measure memory allocations
- [ ] Document performance improvement percentage
- [ ] Add results to benchmarks/ directory

### Documentation
- [ ] Update AOT compatibility documentation
- [ ] Document any limitations or edge cases
- [ ] Add migration guide for existing users
- [ ] Update README with AOT status

## Implementation Details

### Files Modified
- `source/timewarp-nuru-core/execution/delegate-request.cs` - DelegateRequestHandler now uses InvokerRegistry
- `source/timewarp-nuru-core/nuru-core-app-builder.routes.cs` - Added DynamicallyAccessedMembers to MapMediator
- `source/timewarp-nuru-core/nuru-core-app.cs` - Removed CreateDelegateInvoker method
- `source/timewarp-nuru-core/services/lightweight-service-provider.cs` - Use LoggerFactory.CreateLogger(Type) instead of MakeGenericType
- `tests/Directory.Build.props` - Added direct analyzer ProjectReference (source generators don't flow transitively)
- `tests/test-apps/timewarp-nuru-testapp-delegates/program.cs` - Added AddMediator() registration

### Key Fixes
1. **IL2067** - Added `[DynamicallyAccessedMembers]` to `MapMediator` parameter
2. **IL3050** - Fixed `LightweightServiceProvider` to avoid `MakeGenericType`
3. **IL2075** - Removed `CreateDelegateInvoker` fallback that used `GetProperty("Result")`

### Test Results (2025-12-04)

**AOT Compilation:**
```
dotnet publish -c Release (with PublishAot=true)
Zero IL2XXX/IL3XXX warnings
```

**Runtime Tests:**
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

Total: 117/117 tests passed (100.0%) in 562.1s
```

## Notes

Source generators don't flow transitively with ProjectReference. Each project that needs the NuruInvokerGenerator must have a direct reference to timewarp-nuru-analyzers with:
```xml
<ProjectReference Include="path/to/timewarp-nuru-analyzers.csproj">
  <OutputItemType>Analyzer</OutputItemType>
  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
</ProjectReference>
```

Performance benchmarking deferred to separate task.
