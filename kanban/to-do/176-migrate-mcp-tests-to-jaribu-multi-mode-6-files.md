# Migrate MCP Tests to Jaribu Multi-Mode (6 Files)

## Description

Migrate the 6 MCP test files in `tests/timewarp-nuru-mcp-tests/` to support Jaribu multi-mode pattern.

## Files to Migrate

- [ ] `mcp-01-example-retrieval.cs`
- [ ] `mcp-02-syntax-documentation.cs`
- [ ] `mcp-03-route-validation.cs`
- [ ] `mcp-04-handler-generation.cs`
- [ ] `mcp-05-error-documentation.cs`
- [ ] `mcp-06-version-info.cs`

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block at top
2. Wrap types in namespace `TimeWarp.Nuru.Tests.Mcp`
3. Add `[ModuleInitializer]` registration method to each test class
4. Remove `[ClearRunfileCache]` attribute if present

## Checklist

- [ ] Migrate all 6 MCP test files with Jaribu multi-mode pattern
- [ ] Update ci-tests/Directory.Build.props to include MCP tests glob pattern
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Regenerate InternalsVisibleTo if file names change
- [ ] Commit changes

## Notes

- MCP tests reference `timewarp-nuru-mcp.csproj` - may need to add project reference to ci-tests
- Use namespace `TimeWarp.Nuru.Tests.Mcp` for all files
- These tests stay in `timewarp-nuru-mcp-tests/` (not moved to core-tests)
- Refer to `tests/timewarp-nuru-core-tests/routing/` for migration examples
