# Create Roslyn Analyzer for Route Patterns

## Description

Create a Roslyn analyzer that uses the new route pattern parser to provide compile-time validation of route patterns in `AddRoute` calls. This will catch syntax errors and provide IntelliSense support during development.

## Requirements

- Analyze `AddRoute` calls at compile time using the route pattern parser
- Report diagnostics for invalid route patterns
- Provide code fixes where appropriate
- Support for all route pattern syntax as defined in `/Documentation/Developer/Reference/RoutePatternSyntax.md`

## Diagnostics to Implement

### Syntax Errors
- **NURU001**: Invalid parameter syntax (e.g., `<param>` instead of `{param}`)
- **NURU002**: Unbalanced braces in route pattern
- **NURU003**: Invalid option format (must start with `--` or `-`)
- **NURU004**: Invalid type constraint (must be one of: string, int, double, bool, DateTime, Guid, long, float, decimal, or any enum type)

### Semantic Validations
- **NURU005**: Catch-all parameter not at end of route
- **NURU006**: Duplicate parameter names in route
- **NURU007**: Conflicting optional parameters (e.g., `{opt1?} {opt2?}` is ambiguous)
- **NURU008**: Mixed catch-all with optional parameters in same route
- **NURU009**: Option with same short and long form

## Implementation Steps

1. Create analyzer project `TimeWarp.Nuru.Analyzers`
2. Reference the parsing library to reuse parser
3. Implement analyzer that:
   - Finds `AddRoute` method calls
   - Extracts the route pattern string from first argument
   - Parses using `RoutePatternParser`
   - Reports diagnostics for any parse errors
   - Performs semantic validation on the parsed AST
   - Validates parameter names, catch-all position, etc.
4. Implement code fix providers for common mistakes
5. Create unit tests for analyzer
6. Package as NuGet for distribution

## Example

```csharp
// Syntax Errors
app.AddRoute("deploy <env>", ...)           // NURU001: Suggests: "deploy {env}"
app.AddRoute("deploy {env", ...)            // NURU002: Missing closing brace
app.AddRoute("test -verbose", ...)          // NURU003: Should be --verbose or -v
app.AddRoute("get {id:invalid}", ...)       // NURU004: Unknown type constraint

// Semantic Validations
app.AddRoute("deploy {*args} {env}", ...)   // NURU005: Catch-all must be last
app.AddRoute("copy {file} {file}", ...)     // NURU006: Duplicate parameter 'file'
app.AddRoute("backup {src?} {dst?}", ...)   // NURU007: Ambiguous optional parameters
app.AddRoute("exec {cmd?} {*args}", ...)    // NURU008: Can't mix optional and catch-all
app.AddRoute("test --verbose,-v,-v", ...)   // NURU009: Duplicate short form '-v'
```

## Success Criteria

- Analyzer catches all parser errors at compile time
- Provides helpful error messages with exact locations
- Suggests fixes for common mistakes
- No false positives on valid route patterns
- Minimal impact on build performance