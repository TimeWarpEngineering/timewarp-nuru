# Create Roslyn Analyzer for Route Patterns Using Incremental Generators

## Description

Create a Roslyn incremental generator that uses the new route pattern parser to provide compile-time validation of route patterns in `AddRoute` calls. This modern approach provides better performance through incremental compilation and caching, making it suitable for large-scale projects.

## Requirements

- Implement as IIncrementalGenerator for optimal performance
- Analyze `AddRoute` calls at compile time using the route pattern parser
- Report diagnostics for invalid route patterns through the generator pipeline
- Provide code fixes where appropriate
- Support for all route pattern syntax as defined in `/Documentation/Developer/Reference/RoutePatternSyntax.md`
- Use pipeline-based architecture with proper caching for incremental compilation

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

1. Create incremental generator project `TimeWarp.Nuru.Analyzers`
   - Target .NET Standard 2.0 for broad compatibility
   - Reference Microsoft.CodeAnalysis.CSharp (4.8.0 or later for latest features)
   - Reference the parsing library to reuse RoutePatternParser

2. Implement IIncrementalGenerator with pipeline architecture:
   - Use SyntaxProvider to efficiently find `AddRoute` method invocations
   - Create equatable RouteInfo models for caching between compilations
   - Parse route patterns using existing `RoutePatternParser`
   - Report diagnostics through RegisterSourceOutput

3. Optimize for performance:
   - Use predicate filtering to minimize syntax nodes examined
   - Forward cancellation tokens to all Roslyn API calls
   - Create immutable, equatable data structures for pipeline caching
   - Avoid allocations in hot paths

4. Implement code fix providers:
   - Create separate CodeFixProvider for each diagnostic
   - Use incremental approach for fix computation
   - Support batch fixing where appropriate

5. Testing strategy:
   - Use Microsoft.CodeAnalysis.Testing for generator tests
   - Verify incremental behavior and caching
   - Test diagnostic accuracy and locations
   - Ensure no false positives on valid patterns

6. Package and distribution:
   - Package as NuGet with analyzer assets
   - Include proper analyzer configuration
   - Document installation and usage

## Implementation Example

```csharp
[Generator]
public class NuruRouteAnalyzer : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find all AddRoute invocations efficiently
        var routeInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsAddRouteInvocation(node),
                transform: static (ctx, ct) => GetRouteInfo(ctx, ct))
            .Where(static info => info is not null);

        // Step 2: Collect diagnostics from route pattern analysis
        var diagnostics = routeInvocations
            .Select(static (info, ct) => AnalyzeRoutePattern(info!, ct))
            .Where(static result => result.HasDiagnostics);

        // Step 3: Report diagnostics through the pipeline
        context.RegisterSourceOutput(diagnostics, static (ctx, diagnostic) =>
        {
            foreach (var diag in diagnostic.Diagnostics)
            {
                ctx.ReportDiagnostic(diag);
            }
        });
    }

    private static bool IsAddRouteInvocation(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax invocation &&
               invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.Text == "AddRoute";
    }

    private static RouteInfo? GetRouteInfo(GeneratorSyntaxContext context, CancellationToken ct)
    {
        // Extract route pattern string and create equatable model
        // This enables caching between compilations
    }

    private static DiagnosticResult AnalyzeRoutePattern(RouteInfo info, CancellationToken ct)
    {
        // Use RoutePatternParser to parse and validate
        // Return any diagnostics found
    }
}
```

## Usage Examples

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

## Key Advantages of Incremental Generator Approach

1. **Performance**: Only re-analyzes changed code, not entire compilation
2. **Caching**: Equatable models enable efficient caching between compilations
3. **Scalability**: Designed for large codebases (tested on Roslyn/CoreCLR scale)
4. **Pipeline Architecture**: Composable transformations for maintainability
5. **Modern API**: Aligns with current Roslyn best practices and tooling
6. **Unified Approach**: Can extend to generate code in future if needed

## Success Criteria

- Incremental generator catches all parser errors at compile time
- Provides helpful error messages with exact locations
- Suggests fixes for common mistakes through code fix providers
- No false positives on valid route patterns
- Excellent build performance through incremental compilation
- Proper caching behavior - only re-analyzes changed AddRoute calls
- Works efficiently in large codebases with thousands of routes
- Integrates seamlessly with IDEs for real-time feedback

## Implementation Status (2025-08-06)

### Completed
- ✅ Created TimeWarp.Nuru.Analyzers project structure
- ✅ Targeted .NET 9 instead of .NET Standard 2.0 (decision: trade IDE support for cleaner implementation)
- ✅ Set up incremental generator foundation with IIncrementalGenerator
- ✅ Defined all 9 diagnostic descriptors (NURU001-NURU009)
- ✅ Added analyzer release tracking files
- ✅ Configured proper NuGet packaging
- ✅ Suppressed RS1041 warning (targeting .NET 9 is intentional)
- ✅ Updated Microsoft.CodeAnalysis packages to 4.14.0
- ✅ All tests pass after package updates (44/44 for all implementations)

### Key Decisions Made
1. **Target .NET 9 instead of .NET Standard 2.0**
   - Pros: Cleaner code, no polyfills needed, works with CLI and modern IDEs (Rider)
   - Cons: No VS/VS Code IntelliSense support (analyzer runs at build time only)
   - Rationale: Users can opt-in to analyzer package; main library unaffected

2. **Direct reference to TimeWarp.Nuru project**
   - No separate parser library needed since we're targeting .NET 9
   - Can use all modern C# features and parsing code directly

### Next Steps for Implementation
1. **Implement IsAddRouteInvocation predicate**
   - Check for InvocationExpressionSyntax
   - Match "AddRoute" method name
   - Consider extension method usage pattern

2. **Implement GetRouteInfo extraction**
   - Extract first string literal argument from AddRoute call
   - Create equatable RouteInfo record with pattern and Location
   - Handle edge cases (non-literal arguments, missing arguments)

3. **Implement AnalyzeRoutePattern validation**
   - Use TimeWarp.Nuru.Parsing.RoutePatternParser
   - Map ParseError results to appropriate diagnostic descriptors
   - Calculate precise Location spans for error squiggles

4. **Create RouteInfo equatable model**
   - Must implement proper equality for incremental caching
   - Include pattern string and source location
   - Consider adding parsed result caching

5. **Add code fix providers**
   - NURU001: Replace angle brackets with curly braces
   - NURU003: Add missing dash for options
   - NURU005: Move catch-all to end
   - Others as appropriate

### Technical Notes
- RoutePatternParser is in TimeWarp.Nuru.Parsing namespace
- Parser returns ParseResult with Errors collection
- Need to map parser error types to our diagnostic codes
- Consider caching parsed results in RouteInfo for performance

### Testing Approach
- Create test project using Microsoft.CodeAnalysis.Testing
- Test each diagnostic rule with valid/invalid examples
- Verify incremental behavior (unchanged files not re-analyzed)
- Test edge cases: non-string literals, concatenated strings, etc.