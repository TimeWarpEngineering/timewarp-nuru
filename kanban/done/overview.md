# Document AOT Compatibility

## Summary

Update documentation to reflect full AOT (Ahead-of-Time) compilation support achieved in task 008. TimeWarp.Nuru now compiles with zero IL2XXX/IL3XXX warnings and runs correctly as a native AOT binary.

## Todo List

- [x] Update README.md with AOT compatibility section
- [x] Document source generator requirements (direct analyzer reference needed)
- [x] Add AOT sample/example to samples/ directory
- [x] Update user documentation in documentation/user/
- [x] Document any limitations or edge cases
- [x] Add migration notes for users upgrading from non-AOT versions

## Notes

### Key Points to Document

1. **Zero AOT Warnings** - PublishAot=true produces no IL2XXX/IL3XXX warnings

2. **Source Generator** - The NuruInvokerGenerator source generator is included in the TimeWarp.Nuru package and runs automatically for consumers. (Note: In-repo development requires direct ProjectReference to the analyzer since source generators don't flow transitively with ProjectReference.)

3. **AddMediator() Required** - When using NuruApp.CreateBuilder with DI, must call `services.AddMediator()`

4. **No Reflection Fallback** - If a delegate signature doesn't have a generated invoker, an exception is thrown (fail-fast, no silent fallback)

### Reference Implementation

See `tests/test-apps/timewarp-nuru-testapp-delegates/` for a working AOT-compatible example with:
- PublishAot=true
- TrimMode=partial
- All route patterns working

### Related Tasks

- 008 - Implement source generators for reflection-free routing (completed)
- 008d - AOT testing and verification (completed)

## Completion Notes

**Files Created:**
- `samples/aot-example/aot-example.cs` - Complete AOT sample with typed parameters, options, async, and catch-all routes
- `samples/aot-example/aot-example.csproj` - Project file with AOT configuration and source generator references

**Files Updated:**
- `documentation/user/guides/deployment.md` - Added sections:
  - "Source Generators for AOT" - Explains NuruInvokerGenerator and AddMediator() requirement
  - "Fail-Fast Behavior" - Documents no-reflection-fallback approach
  - "AOT Limitations and Edge Cases" - Lists supported patterns and considerations
  - "Migration from Non-AOT Versions" - Step-by-step upgrade guide
- `readme.md` - Enhanced AOT feature description and added link to AOT sample
