# Update MCP Server for New Features

## Description

Review and update the TimeWarp.Nuru.Mcp server to document and expose all new features added since the MCP server was created. The MCP server helps AI assistants understand and generate correct Nuru code, so keeping it current is critical.

## Requirements

- Update examples.json manifest with new samples
- Add new MCP tools for recently added features
- Update existing tools to reflect API changes
- Ensure documentation matches implementation

## Checklist

### Review Current State
- [ ] Audit all samples in `/Samples/` against `examples.json`
- [ ] Review MCP tools against recent feature additions
- [ ] Check GenerateHandlerTool uses new `NuruApp.CreateBuilder()` pattern

### Update examples.json Manifest
- [ ] Add missing samples: CreateBuilder pattern (`calc-createbuilder.cs`)
- [ ] Add REPL demos (`repl-basic-demo.cs`, `repl-custom-keybindings.cs`, etc.)
- [ ] Add Shell Completion example (`ShellCompletionExample.cs`)
- [ ] Add Dynamic Completion example (`DynamicCompletionExample.cs`)
- [ ] Add Configuration samples (validation, command-line overrides, user secrets)
- [ ] Add CoconaComparison samples if appropriate

### Update MCP Tools
- [ ] Update `GenerateHandlerTool` to generate `NuruApp.CreateBuilder()` pattern
- [ ] Add documentation for `CreateBuilder`, `CreateSlimBuilder`, `CreateEmptyBuilder`
- [ ] Add REPL-related syntax/examples to `GetSyntaxTool`
- [ ] Add Tab Completion documentation
- [ ] Consider adding tool for feature comparison (CreateBuilder vs CreateSlimBuilder)

### Update GetSyntaxTool
- [ ] Ensure `SyntaxExamples.cs` has all route pattern features
- [ ] Add examples showing new API patterns
- [ ] Document `Map()` alias for `AddRoute()`

### Update README.md
- [ ] Add sample prompts for new tools/features
- [ ] Document REPL-related tools if added
- [ ] Update use cases section with new scenarios
- [ ] Update "Future Enhancements" to remove completed items

### Testing
- [ ] Run MCP test suite to verify tools work
- [ ] Verify examples.json is valid JSON
- [ ] Test example fetching for new entries

## Notes

### Missing from examples.json (identified during review):
- `calc-createbuilder.cs` - New static factory pattern
- All REPL demos (5 files in ReplDemo/)
- ShellCompletionExample
- DynamicCompletionExample
- Configuration validation sample
- Command-line overrides sample
- User secrets sample

### New features since MCP creation:
- Static factory methods: `NuruApp.CreateBuilder()`, `CreateSlimBuilder()`, `CreateEmptyBuilder()`
- `NuruApplicationOptions` for builder configuration
- `Map()` method alias for ASP.NET Core familiarity
- REPL support with custom key bindings
- Shell tab completion
- Dynamic completion providers
- Configuration with .settings.json files
- Command-line configuration overrides

### GenerateHandlerTool currently generates:
```csharp
.Map("pattern", () => { ... })
```

### Should also show new pattern:
```csharp
var builder = NuruApp.CreateBuilder(args);
builder.Map("pattern", () => { ... });
await builder.Build().RunAsync();
```
