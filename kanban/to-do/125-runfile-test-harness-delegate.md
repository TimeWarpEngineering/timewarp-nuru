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
```

## Checklist

### Implementation
- [ ] Add `NuruTestContext` class with `AsyncLocal<Func<NuruCoreApp, Task<int>>?>` 
- [ ] Update `RunAsync` to check for and invoke `TestRunner` delegate
- [ ] Ensure inner `RunAsync` calls don't re-trigger the delegate (only top-level)

### Testing
- [ ] Create sample runfile `real-app.cs`
- [ ] Create test file `test-real-app.cs` with `ModuleInitializer`
- [ ] Create `Directory.Build.props` for conditional inclusion
- [ ] Verify tests run with `NURU_TEST=test-real-app.cs ./real-app.cs`

### Documentation
- [ ] Document the pattern in testing guide
- [ ] Add example to MCP
- [ ] Update issue #109 with this approach

## Notes

The delegate check should only trigger at the top-level `RunAsync` call. When test code calls `app.RunAsync(args)` to run test scenarios, those should execute normally. Options:
- Clear the delegate after first invocation
- Use a flag to track "already handed off"
- Pass a different method to tests (e.g., `app.ExecuteAsync(args)`)

## References

- GitHub Issue: #109
- Related: Task 124 (AsyncLocal TestTerminal context)
- Builds on `TestTerminalContext` from task 124
