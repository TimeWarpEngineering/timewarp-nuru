# Migrate tests to Jaribu multi-mode for faster execution

## Description

Current test execution takes ~15 minutes because each of the 191 test files compiles and runs independently. Jaribu's multi-mode pattern allows all test files to be compiled together once, reducing execution time to ~10-30 seconds.

### Problem
- 191 test files × ~5-10 seconds compile time = ~15+ minutes
- Each file triggers separate compilation/JIT phase
- No shared compilation across files

### Solution
Use Jaribu multi-mode pattern:
1. Add `[ModuleInitializer]` to each test class for auto-registration
2. Wrap `RunAllTests()` in `#if !JARIBU_MULTI` to prevent self-execution in multi-mode
3. Create single orchestrator that compiles all tests together with `JARIBU_MULTI` defined

### Per-File Migration Pattern

From:
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

return await RunTests<MyTests>(clearCache: true);

[TestTag("Category")]
[ClearRunfileCache]
public class MyTests
{
    public static async Task Should_do_something() { ... }
}
```

To:
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Category")]
public class MyTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MyTests>();
    
    public static async Task Should_do_something() { ... }
}
```

## Checklist

- [ ] Update ~191 test files with new pattern
- [ ] Create orchestrator directory: tests/ci-tests/
- [ ] Create tests/ci-tests/run-ci-tests.cs orchestrator
- [ ] Create tests/ci-tests/Directory.Build.props with JARIBU_MULTI and file includes
- [ ] Update tests/scripts/run-all-tests.cs to use orchestrator
- [ ] Update CI workflow if needed
- [ ] Test standalone execution still works
- [ ] Test multi-mode execution works
- [ ] Verify test count matches between modes

## Acceptance Criteria

- [ ] All tests pass in multi-mode
- [ ] Individual test files still run standalone
- [ ] CI pipeline uses multi-mode orchestrator
- [ ] Test execution time under 1 minute

## Notes

- Helper files (no test classes) don't need [ModuleInitializer]
- Individual test files remain runnable standalone for debugging
- Single orchestrator chosen over per-category for simplicity
- Expected result: Test execution time reduced from ~15 minutes to ~10-30 seconds
