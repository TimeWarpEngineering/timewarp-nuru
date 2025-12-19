# Update Runfile Test Harness Sample to Use Jaribu

## Description

Refactor `samples/testing/runfile-test-harness/test-real-app.cs` to use Jaribu instead of the manual test runner implementation. The `NuruTestContext.TestRunner` pattern remains unchanged - Jaribu provides test discovery, execution, and reporting while the captured app instance is used by test methods.

## Requirements

- Keep zero-modification pattern intact (app under test unchanged)
- Capture `NuruCoreApp` from `NuruTestContext.TestRunner` callback
- Use Jaribu's `RunTests<T>()` for test execution
- Convert manual tests to Jaribu test methods with `Should_` naming
- Add `CleanUp()` method to reset `TestTerminalContext.Current`

## Checklist

### Implementation
- [x] Add `#:package TimeWarp.Jaribu` directive to test-real-app.cs (added via Directory.Build.props)
- [x] Create static field to hold captured app instance
- [x] Update `[ModuleInitializer]` to capture app and call `RunTests<T>()`
- [x] Convert 5 manual tests to Jaribu test methods
- [x] Add `CleanUp()` method for terminal context cleanup
- [x] Verify tests pass with `./run-real-app-tests.cs`

### Documentation
- [x] Update `overview.md` to reflect Jaribu usage
- [x] Add note about Jaribu benefits (attributes, data-driven tests, etc.)

### Additional Fixes
- [x] Fixed `real-app.cs` project path (was `../../source`, now `../../../source`)
- [x] Updated `run-real-app-tests.cs` to recognize Jaribu output format

## Notes

Analysis at: `.agent/workspace/2025-12-08T21-15-00_jaribu-runfile-test-harness-analysis.md`

Key pattern:
```csharp
public static class TestHarness
{
    internal static NuruCoreApp? App;

    [ModuleInitializer]
    public static void Initialize()
    {
        NuruTestContext.TestRunner = async (app) =>
        {
            App = app;  // Capture the real app
            return await RunTests<RealAppTests>(clearCache: false);
        };
    }
}
```
