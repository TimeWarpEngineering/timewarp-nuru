# Migrate MCP Tests to Jaribu Multi-Mode (6 Files)

## Description

Migrate the 6 MCP test files in `tests/timewarp-nuru-mcp-tests/` to support Jaribu multi-mode pattern.

## Files to Migrate

- [x] `mcp-01-example-retrieval.cs`
- [x] `mcp-02-syntax-documentation.cs`
- [x] `mcp-03-route-validation.cs`
- [x] `mcp-04-handler-generation.cs`
- [x] `mcp-05-error-documentation.cs`
- [x] `mcp-06-version-info.cs`

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block at top
2. Wrap types in namespace `TimeWarp.Nuru.Tests.Mcp`
3. Add `[ModuleInitializer]` registration method to each test class
4. Remove `[ClearRunfileCache]` attribute if present

## Checklist

- [x] Migrate all 6 MCP test files with Jaribu multi-mode pattern
- [x] Update ci-tests/Directory.Build.props to include MCP tests glob pattern
- [x] Test standalone mode for each file
- [x] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [x] Regenerate InternalsVisibleTo if file names change (not needed - names unchanged)
- [ ] Commit changes

## Notes

- Added project reference to `timewarp-nuru-mcp.csproj` in ci-tests/Directory.Build.props
- Added global using for `TimeWarp.Nuru.Mcp.Tools` namespace
- Use namespace `TimeWarp.Nuru.Tests.Mcp` for all files
- Tests stay in `timewarp-nuru-mcp-tests/` (not moved)
- CI test count increased from 1218 to 1315 (+97 tests)
