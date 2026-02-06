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
