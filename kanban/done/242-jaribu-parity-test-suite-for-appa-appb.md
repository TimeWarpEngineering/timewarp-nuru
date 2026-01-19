# Jaribu parity test suite for AppA/AppB

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Create comprehensive Jaribu tests that run both AppA (runtime) and AppB (generated) with the same inputs, verifying they produce identical outputs. This proves the generated implementation is correct.

## Sub-Tasks

This task is broken into steps:

| Step | Task | Description |
|------|------|-------------|
| 1 | #242-step-1 | Parse route pattern to design-time model |
| 2 | #242-step-2 | Manual runtime construction from design-time model |
| 3 | #242-step-3 | Source generator for design-time model |
| 4 | #242-step-4 | Source generator emits runtime structures |

## Two-Phase Approach

**Phase 1: Build Design-Time Model**
```
Source syntax (pattern string) → RouteDefinition, SegmentDefinition, etc.
```

**Phase 2: Generate Runtime Code**
```
Design-Time Model → Runtime structures (Endpoint[], matchers, etc.)
```

Keeping phases separate allows independent testing and easier reasoning.

## Requirements

- Tests run both executables with same args
- Compare exit codes, stdout, stderr
- Cover all routes, help, errors, edge cases
- Use `Shell.Builder()` for process execution (not `Process.Start`)

## Checklist

- [ ] #242-step-1: Parse patterns to design-time model
- [ ] #242-step-2: Manual runtime construction works (`add 2 2`)
- [ ] #242-step-3: Source generator builds design-time model
- [ ] #242-step-4: Source generator emits runtime code
- [ ] Create parity test file `tests/timewarp-nuru-gen-tests/parity-01-basic.cs`
- [ ] Test all calculator routes produce same output
- [ ] Test `--help` produces same output
- [ ] Test invalid commands produce same errors

## Notes

### Test Structure

```csharp
#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.SourceGen.Parity
{

[TestTag("SourceGen")]
public class AppParityTests
{
    [ModuleInitializer]
    internal static void Register() => RegisterTests<AppParityTests>();

    [Input("add", "2", "3")]
    [Input("subtract", "10", "4")]
    [Input("--help")]
    public static async Task Should_match_runtime_output(params string[] args)
    {
        // Arrange
        string appA = GetAppAPath();
        string appB = GetAppBPath();
        
        // Act
        CommandOutput resultA = await Shell.Builder(appA)
            .WithArguments(args)
            .WithNoValidation()
            .CaptureAsync();
            
        CommandOutput resultB = await Shell.Builder(appB)
            .WithArguments(args)
            .WithNoValidation()
            .CaptureAsync();
        
        // Assert
        resultA.ExitCode.ShouldBe(resultB.ExitCode);
        resultA.Stdout.ShouldBe(resultB.Stdout);
        resultA.Stderr.ShouldBe(resultB.Stderr);
        
        await Task.CompletedTask;
    }
}

} // namespace
```

### Success Criteria

When all parity tests pass, AppB is ready to replace AppA.
