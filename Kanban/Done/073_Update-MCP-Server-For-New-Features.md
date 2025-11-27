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
- [x] Audit all samples in `/Samples/` against `examples.json`
- [x] Review MCP tools against recent feature additions
- [x] Check GenerateHandlerTool uses new `NuruApp.CreateBuilder()` pattern

### Update examples.json Manifest
- [x] Add missing samples: CreateBuilder pattern (`calc-createbuilder.cs`)
- [x] Add REPL demos (`repl-basic-demo.cs`, `repl-custom-keybindings.cs`, etc.)
- [x] Add Shell Completion example (`ShellCompletionExample.cs`)
- [x] Add Dynamic Completion example (`DynamicCompletionExample.cs`)
- [x] Add Configuration samples (validation, command-line overrides)
- [ ] Add CoconaComparison samples if appropriate (skipped - too many, not stable yet)

### Update MCP Tools
- [x] Update `GenerateHandlerTool` to generate `NuruApp.CreateBuilder()` pattern
- [ ] Add documentation for `CreateBuilder`, `CreateSlimBuilder`, `CreateEmptyBuilder` (future task)
- [ ] Add REPL-related syntax/examples to `GetSyntaxTool` (future task - requires new regions)
- [ ] Add Tab Completion documentation (future task - requires new regions)
- [ ] Consider adding tool for feature comparison (CreateBuilder vs CreateSlimBuilder) (future task)

### Update GetSyntaxTool
- [x] Ensure `SyntaxExamples.cs` has all route pattern features (verified - comprehensive)
- [ ] Add examples showing new API patterns (future task - requires SyntaxExamples.cs update)
- [x] Document `Map()` alias for `AddRoute()` (already in syntax examples)

### Update README.md
- [x] Add sample prompts for new tools/features
- [x] Document REPL-related tools if added (via get_example prompts)
- [x] Update use cases section with new scenarios
- [x] Update "Future Enhancements" to remove completed items

### Testing
- [x] Run MCP test suite to verify tools work (60/60 tests pass)
- [x] Verify examples.json is valid JSON
- [ ] Test example fetching for new entries (requires GitHub push first)

## Implementation Notes

### Changes Made:

**examples.json (Samples/examples.json)**
- Bumped version from 1.0 to 2.0
- Added 14 new examples (from 8 to 22 total):
  - `createbuilder` - ASP.NET Core-style CreateBuilder API
  - `hello-world` - Simplest possible app
  - `configuration-validation` - Fail-fast validation with FluentValidation
  - `command-line-overrides` - ASP.NET Core-style --Section:Key=Value overrides
  - `repl-basic` - Interactive REPL mode
  - `repl-keybindings` - Custom key binding profiles
  - `repl-interactive` - CLI with --interactive flag
  - `repl-options` - All REPL configuration options
  - `shell-completion` - Static shell tab completion
  - `dynamic-completion` - Dynamic completion with custom sources
  - `syntax-examples` - Route pattern syntax reference
  - `builtin-types` - Built-in type converters
  - `custom-type-converter` - Custom type converter example

**GenerateHandlerTool.cs**
- Updated to generate `NuruApp.CreateBuilder(args)` pattern as recommended approach
- Shows fluent builder pattern as alternative
- Updated mediator pattern to use correct TimeWarp.Mediator signatures
- Added full app setup code including `ConfigureServices` for mediator handlers

**README.md**
- Updated `get_example` tool documentation with full list of available examples
- Enhanced `generate_handler` documentation to describe CreateBuilder pattern
- Added new use case sections: "Adding Interactive Features" and "Configuration"
- Updated "Future Enhancements" - removed completed items (dynamic discovery, syntax docs)

### Tests Verified:
- mcp-02-syntax-documentation.cs: 26/26 passed
- mcp-03-route-validation.cs: 19/19 passed
- mcp-04-handler-generation.cs: 15/15 passed
