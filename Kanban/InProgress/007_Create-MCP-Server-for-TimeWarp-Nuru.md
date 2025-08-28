# Create Simple MCP Server for TimeWarp.Nuru Examples and Validation

## Description

Build a simple Model Context Protocol (MCP) server that serves TimeWarp.Nuru examples, documentation, and route validation. No LLM required - just serving existing content and using the existing route parser for validation. This will help AI agents quickly access working examples and validate route patterns.

## Requirements

- Implement using stdio transport for simplicity
- Follow the .NET MCP server quickstart guide  
- Serve existing samples from the repository
- Use existing RoutePatternParser for validation
- No code generation or LLM needed

## Reference Implementation

Follow the Microsoft quickstart guide: https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/

## MCP Server Features

### Tools to Implement (Simple, No LLM)

1. **get-example**
   - Input: example name or category
   - Returns: Complete working code from Samples folder
   - Categories: basic, mediator, async, logging, calculator, catch-all

2. **validate-route**
   - Input: route pattern string
   - Returns: Valid/Invalid with specific NURU001-009 errors
   - Uses existing RoutePatternParser

3. **get-syntax**
   - Input: syntax element (parameters, options, catch-all, etc.)
   - Returns: Documentation from RoutePatternSyntax.md
   - Includes examples for each element

### Pseudo Source Generators (Phase 2 - Dynamic Code Generation)

4. **generate-handler**
   - Input: route pattern (e.g., `"deploy {env} --force"`)
   - Output: Handler function with correct signature
   - Generates both Direct approach lambdas and Mediator handlers
   - Example: `(string env, bool force) => { /* TODO */ }`

5. **generate-command**
   - Input: route pattern
   - Output: Complete Mediator pattern Command/Handler classes
   - Includes proper property types and attributes
   - Generates IRequest<T> implementation with Handler

6. **generate-tests**
   - Input: route pattern  
   - Output: Unit test cases for the route
   - Includes valid input tests
   - Tests for each NURU diagnostic violation
   - Edge cases and boundary conditions

7. **generate-migration**
   - Input: Code from other CLI framework
   - Output: Equivalent TimeWarp.Nuru implementation
   - Supports System.CommandLine, CommandLineParser, Cocona
   - Maps attributes and patterns to routes

### Resources to Provide (Static Content)

1. **glossary** - Terms from Glossary.md
2. **diagnostics** - NURU001-009 rules from UsingAnalyzers.md  
3. **patterns** - Common patterns from documentation

## Implementation Steps

### Phase 1: Project Setup
1. Create new .NET console project `TimeWarp.Nuru.MCP`
2. Add NuGet package reference to TimeWarp.Nuru for parser
3. Reference Microsoft's MCP quickstart for stdio setup
4. Copy or reference existing samples and docs

### Phase 2: Implement Tools
1. **GetExampleTool.cs**
   - Read from Samples folder
   - Return complete working examples
   - Support category-based lookup

2. **ValidateRouteTool.cs**  
   - Use RoutePatternParser.TryParse()
   - Map errors to NURU diagnostic codes
   - Return structured validation results

3. **GetSyntaxTool.cs**
   - Read from RoutePatternSyntax.md
   - Extract relevant sections
   - Include examples

### Phase 3: Add Resources
1. Copy static documentation content
2. Format as MCP resources
3. Test with Claude or other MCP clients

## Why Pseudo Source Generators?

Traditional source generators run at compile-time to generate C# code. Our "pseudo source generators" serve a different purpose - they generate code that LLMs can use to understand patterns and create consistent implementations. Benefits:

1. **Teaching Tool** - LLMs learn correct patterns by seeing generated examples
2. **Consistency** - Always generates idiomatic TimeWarp.Nuru code  
3. **Type Safety** - Generated signatures match route patterns exactly
4. **Error Prevention** - No manual translation mistakes
5. **Immediate Feedback** - LLMs get working code instantly

Unlike real source generators that modify the build, these are on-demand code generators that help LLMs write better TimeWarp.Nuru applications.

## Technical Implementation

### Server Structure (Updated with Pseudo Generators)
```csharp
using System.Text;
using TimeWarp.Nuru.Parsing;

public class NuruMcpServer
{
    private readonly RoutePatternParser _parser = new();
    
    // Tool: Generate handler from route pattern
    public string GenerateHandler(string pattern, bool useMediatorPattern = false)
    {
        var result = _parser.TryParse(pattern);
        if (!result.Success) 
            return $"// Error: {string.Join(", ", result.Errors)}";
            
        var route = result.CompiledRoute;
        var parameters = ExtractParameters(route);
        
        if (useMediatorPattern)
            return GenerateMediatorCode(pattern, parameters);
        else
            return GenerateDirectCode(pattern, parameters);
    }
    
    private string GenerateDirectCode(string pattern, List<ParameterInfo> parameters)
    {
        var signature = BuildSignature(parameters);
        var sb = new StringBuilder();
        
        sb.AppendLine($"// Route: {pattern}");
        sb.AppendLine($"builder.AddRoute(\"{pattern}\", {signature} =>");
        sb.AppendLine("{");
        sb.AppendLine("    // TODO: Implement handler logic");
        
        foreach (var param in parameters)
        {
            sb.AppendLine($"    Console.WriteLine(\"{param.Name}: {{{param.Name}}}\");");
        }
        
        sb.AppendLine("});");
        return sb.ToString();
    }
    
    private string GenerateMediatorCode(string pattern, List<ParameterInfo> parameters) 
    {
        // Generates full Command and Handler classes
        // Properties match route parameters with correct types
        // Includes IRequest<T> implementation
    }
}
```

### Usage Example
```bash
# Install the MCP server globally
dotnet tool install -g TimeWarp.Nuru.MCP

# Run the server
timewarp-nuru-mcp

# Configure in Claude or other MCP clients
claude mcp add nuru-helper timewarp-nuru-mcp
```

## Benefits for AI Agents

1. **Accurate Code Generation**: Validate patterns before generating code
2. **Context Awareness**: Understand existing project structure
3. **Best Practices**: Generate idiomatic TimeWarp.Nuru code
4. **Error Prevention**: Catch issues before code execution
5. **Learning Support**: Access to examples and documentation

## Success Criteria

- [ ] MCP server successfully validates route patterns
- [ ] Can generate new CLI applications with proper structure
- [ ] Provides helpful resources for TimeWarp.Nuru development
- [ ] Works seamlessly with Claude and other MCP clients
- [ ] Reduces errors in AI-generated TimeWarp.Nuru code
- [ ] Includes comprehensive documentation
- [ ] Published as dotnet tool for easy installation

## Testing Strategy

1. **Unit Tests**
   - Test each tool independently
   - Verify resource generation
   - Test error handling

2. **Integration Tests**
   - Test with actual MCP clients
   - Verify stdio communication
   - Test tool chaining scenarios

3. **End-to-End Tests**
   - Generate complete applications
   - Validate generated code compiles
   - Test with real TimeWarp.Nuru projects

## Documentation Requirements

1. **User Guide**
   - Installation instructions
   - Configuration for different clients
   - Tool usage examples

2. **Developer Guide**
   - Architecture overview
   - Extension points
   - Contributing guidelines

3. **API Reference**
   - All tools and parameters
   - Resource schemas
   - Error codes and messages

## Future Enhancements

- **Project Analysis**: Analyze entire TimeWarp.Nuru projects
- **Performance Tools**: Benchmark route matching
- **Migration Wizards**: Interactive migration from other frameworks
- **Code Refactoring**: Suggest improvements to existing routes
- **Testing Tools**: Generate unit tests for handlers
- **Documentation Generation**: Create docs from route definitions

## Related Links

- [MCP .NET SDK Quickstart](https://devblogs.microsoft.com/dotnet/mcp-server-dotnet-nuget-quickstart/)
- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [TimeWarp.Nuru Documentation](../Documentation/Developer/Reference/)
- [Route Pattern Syntax](../Documentation/Developer/Reference/RoutePatternSyntax.md)