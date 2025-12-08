# Runfile Test Harness via Ambient Delegate

## Description

Enable testing of runfiles without modifying the runfile code by using an ambient delegate pattern. The test code is included at build time via `Directory.Build.props` and sets a delegate that `RunAsync` calls instead of normal execution.

This allows:
- Zero changes to the runfile being tested
- Same process execution (AsyncLocal works)
- Full control over test scenarios with real `NuruCoreApp` instance
- Multiple test cases per test file

## Design

### Ambient Delegate

```csharp
// In Nuru
public static class NuruTestContext
{
    private static readonly AsyncLocal<Func<NuruCoreApp, Task<int>>?> _testRunner = new();
    
    public static Func<NuruCoreApp, Task<int>>? TestRunner
    {
        get => _testRunner.Value;
        set => _testRunner.Value = value;
    }
}
```

### RunAsync Check

```csharp
public async Task<int> RunAsync(string[] args)
{
    if (NuruTestContext.TestRunner != null)
    {
        return await NuruTestContext.TestRunner(this);
    }
    
    // Normal execution...
}
```

### Test File Structure

```csharp
// test-real-app.cs (included via Directory.Build.props when NURU_TEST is set)
public static class TestSetup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        NuruTestContext.TestRunner = async (app) =>
        {
            // Test 1
            using (var terminal = new TestTerminal())
            {
                TestTerminalContext.Current = terminal;
                await app.RunAsync(["greet", "Alice"]);
                terminal.OutputContains("Hello, Alice!").ShouldBeTrue();
            }
            
            // Test 2
            using (var terminal = new TestTerminal())
            {
                TestTerminalContext.Current = terminal;
                await app.RunAsync(["deploy", "prod", "--dry-run"]);
                terminal.OutputContains("DRY RUN").ShouldBeTrue();
            }
            
            Console.WriteLine("All tests passed!");
            return 0;
        };
    }
}
```

### Directory.Build.props (in runfile folder)

```xml
<Project>
  <ItemGroup Condition="'$(NURU_TEST)' != ''">
    <Compile Include="$(NURU_TEST)" />
  </ItemGroup>
</Project>
```

### Execution

```bash
# Normal run
./real-app.cs greet World

# Test run - includes test file, delegate takes over
NURU_TEST=test-real-app.cs ./real-app.cs

# IMPORTANT: Clean after changing NURU_TEST (runfile cache doesn't track env vars)
dotnet clean ./real-app.cs
```

## Checklist

### Implementation
- [x] Add `NuruTestContext` class with `AsyncLocal<Func<NuruCoreApp, Task<int>>?>` 
- [x] Update `RunAsync` to check for and invoke `TestRunner` delegate
- [x] Ensure inner `RunAsync` calls don't re-trigger the delegate (only top-level)

### Testing
- [x] Create sample runfile `real-app.cs`
- [x] Create test file `test-real-app.cs` with `ModuleInitializer`
- [x] Create `Directory.Build.props` for conditional inclusion
- [x] Verify tests run with `NURU_TEST=test-real-app.cs ./real-app.cs`
- [x] Create automated test runner `run-real-app-tests.cs` using Amuru

### Documentation
- [x] Document the pattern in `samples/testing/overview.md`
- [ ] Add example to MCP
- [ ] Update issue #109 with this approach

## Implementation Notes

### Files Created/Modified

- `source/timewarp-nuru-core/io/nuru-test-context.cs` - NuruTestContext with AsyncLocal delegate
- `source/timewarp-nuru-core/nuru-core-app.cs` - RunAsync checks for test runner delegate
- `samples/testing/runfile-test-harness/real-app.cs` - Sample CLI app (system under test)
- `samples/testing/runfile-test-harness/test-real-app.cs` - Test harness with ModuleInitializer
- `samples/testing/runfile-test-harness/run-real-app-tests.cs` - Automated test runner using Amuru
- `samples/testing/runfile-test-harness/Directory.Build.props` - Conditional test file inclusion
- `samples/testing/runfile-test-harness/overview.md` - Subfolder documentation
- `samples/testing/overview.md` - Updated documentation

### Key Insight: Runfile Cache

The runfile build cache does NOT track environment variables. When toggling `NURU_TEST`, you MUST run `dotnet clean ./app.cs` to force a rebuild with the new configuration.

### CI/CD Usage

```yaml
# Test job
- name: Run Tests
  run: |
    export NURU_TEST=test-real-app.cs
    dotnet clean ./real-app.cs
    ./real-app.cs

# Production build (no NURU_TEST set)
- name: Build Production
  run: |
    dotnet clean ./real-app.cs
    dotnet publish ./real-app.cs
```

## References

- GitHub Issue: #109
- Related: Task 124 (AsyncLocal TestTerminal context)
- Builds on `TestTerminalContext` from task 124
