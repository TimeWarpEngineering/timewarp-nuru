# Analyzer: REPL AutoStartWhenEmpty conflicts with default routes

## Summary

Create a Roslyn analyzer that detects when `AddRepl(options => options.AutoStartWhenEmpty = true)` is used with a default route (empty string `""`), which creates an ambiguous situation where there's no way to distinguish between the REPL and the default route.

## Description

When configuring a Nuru CLI app, there's a conflict between:
1. REPL auto-start with empty args (`AutoStartWhenEmpty = true`)
2. Having a default route (route pattern `""`)

If both are present, there's no way to distinguish between:
- User wants the REPL (no arguments provided)
- User wants the default route (no arguments provided)

This analyzer should catch this at compile-time and report a diagnostic error.

## Checklist

- [ ] Create analyzer class `ReplAutoStartConflictsWithDefaultRouteAnalyzer`
- [ ] Detect `.AddRepl()` calls with `AutoStartWhenEmpty = true`
- [ ] Detect default routes: `[NuruRoute("")]` or `.Map("")` at the top level
- [ ] Report diagnostic error when both conditions are met
- [ ] Add unit tests for the analyzer
- [ ] Add documentation for the diagnostic code

## Notes

### Problem Example
```csharp
// This configuration creates ambiguity:
NuruApp.CreateBuilder(args)
    .AddRepl(options =>
    {
        options.AutoStartWhenEmpty = true;  // Starts REPL when no args
    })
    .Map("").WithHandler(() => "Hello World")  // Default route
    .AsCommand().Done()
    .Build();

// Or with attribute:
[NuruRoute("", Description = "Say hello world")]
public class HelloCommand { }
```

### Expected Diagnostic
- **ID**: NURU001 (or next available)
- **Severity**: Error
- **Message**: "REPL AutoStartWhenEmpty cannot be used with a default route. Remove either the default route or disable AutoStartWhenEmpty."

### Implementation Notes
- The analyzer needs to check both attribute-based routes (`[NuruRoute]`) and fluent API routes (`.Map()`)
- Should only check `.Map("")` at the top level, not within sub-groups
- The conflict occurs when BOTH conditions are true in the same compilation unit
