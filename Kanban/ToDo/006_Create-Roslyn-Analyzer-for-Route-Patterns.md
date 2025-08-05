# Create Roslyn Analyzer for Route Patterns

## Description

Create a Roslyn analyzer that uses the new route pattern parser to provide compile-time validation of route patterns in `AddRoute` calls. This will catch syntax errors and provide IntelliSense support during development.

## Requirements

- Analyze `AddRoute` calls at compile time using the route pattern parser
- Report diagnostics for invalid route patterns
- Provide code fixes where appropriate
- Support for all route pattern syntax

## Diagnostics to Implement

- **NURU001**: Invalid parameter syntax (e.g., `<param>` instead of `{param}`)
- **NURU002**: Unbalanced braces in route pattern
- **NURU003**: Invalid option format
- **NURU004**: Invalid type constraint
- **NURU005**: Catch-all parameter not at end of route
- **NURU006**: Duplicate parameter names in route

## Implementation Steps

1. Create analyzer project `TimeWarp.Nuru.Analyzers`
2. Reference the parsing library to reuse parser
3. Implement analyzer that:
   - Finds `AddRoute` method calls
   - Extracts the route pattern string from first argument
   - Parses using `RoutePatternParser`
   - Reports diagnostics for any parse errors
4. Implement code fix providers for common mistakes
5. Create unit tests for analyzer
6. Package as NuGet for distribution

## Example

```csharp
// This should produce NURU001 diagnostic
app.AddRoute("deploy <env>", ...) // Suggests: "deploy {env}"

// This should produce NURU002 diagnostic  
app.AddRoute("deploy {env", ...) // Missing closing brace

// This should produce NURU005 diagnostic
app.AddRoute("deploy {*args} {env}", ...) // Catch-all must be last
```

## Success Criteria

- Analyzer catches all parser errors at compile time
- Provides helpful error messages with exact locations
- Suggests fixes for common mistakes
- No false positives on valid route patterns
- Minimal impact on build performance