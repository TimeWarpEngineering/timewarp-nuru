# Update NuGet packages to latest versions

## Description

Update NuGet packages to latest stable versions. May require code changes due to API updates.

## Packages to Update

| Package | Current Version | Target Version |
|---------|---------------|----------------|
| OpenTelemetry | 1.14.0 | 1.15.0 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.14.0 | 1.15.0 |
| OpenTelemetry.Extensions.Hosting | 1.14.0 | 1.15.0 |
| TimeWarp.Terminal | 1.0.0-beta.2 | 1.0.0-beta.4 |
| ModelContextProtocol | 0.6.0-preview.1 | 0.8.0-preview.1 |

## Checklist

- [ ] Update OpenTelemetry packages to 1.15.0
- [ ] Update TimeWarp.Terminal to 1.0.0-beta.4
- [ ] Update ModelContextProtocol to 0.8.0-preview.1
- [ ] Update Directory.Packages.props with new versions
- [ ] Run `dotnet restore` to update lock files
- [ ] Build solution to verify compatibility
- [ ] Fix any compilation errors from API changes
- [ ] Run tests to ensure functionality is preserved
- [ ] Address any Nuru-specific changes required due to updates

## Notes

"TimeWarp.Nuru usage will require other changes as well" - verify MCP server and terminal integration work correctly after updates. Check for breaking changes in TimeWarp.Terminal and ModelContextProtocol APIs.

**Blocked by #408:** Cannot update TimeWarp.Terminal to beta.4 until TimeWarp.Jaribu is updated for compatibility (MissingMethodException for WriteTable).

## Implementation Plan

### Phase 1: Version Updates
1. Update Directory.Packages.props with new versions
2. Run `dotnet restore` to fetch new packages
3. Build solution to identify compilation errors

### Phase 2: Fix Compilation Errors
1. Address any TimeWarp.Terminal API compatibility issues
2. Address any ModelContextProtocol API changes (highest risk: 0.6.0 -> 0.8.0)
3. Address any OpenTelemetry API issues
4. Rebuild to verify all errors resolved

### Phase 3: Testing
1. Run CI test suite: `dotnet run tests/ci-tests/run-ci-tests.cs`
2. Verify MCP server builds and tools are registered
3. Run sample applications to verify functionality

### Files to Modify
- Directory.Packages.props (primary config file)
- Any files with compilation errors from API changes

### Key Risk Areas
1. **ModelContextProtocol 0.6.0 -> 0.8.0**: Preview versions, significant version jump
   - Check: `AddMcpServer()`, `WithStdioServerTransport()`, `WithTools<>()`, `[McpServerTool]`
   - Files: source/timewarp-nuru-mcp/program.cs, source/timewarp-nuru-mcp/tools/*.cs

2. **TimeWarp.Terminal beta.2 -> beta.4**: Minor update, ITerminal interface
   - Files: source/timewarp-nuru/nuru-app.cs, global-usings.cs

3. **OpenTelemetry 1.14.0 -> 1.15.0**: Stable, low risk
   - Files: source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs

### Build Commands
```bash
dotnet restore
dotnet build --no-restore
dotnet run tests/ci-tests/run-ci-tests.cs
```
