# Update NuGet packages to latest versions

## Description

Update all outdated NuGet packages to their latest versions after fixing audit issues.

## Checklist

- [ ] Update Microsoft.Extensions.* packages (10.0.3 → 10.0.5)
- [ ] Update TimeWarp.Terminal (1.0.0-beta.5 → 1.0.0-beta.7)
- [ ] Update Serilog (4.2.0 → 4.3.1)
- [ ] Update Serilog.Extensions.Logging (9.0.2 → 10.0.0)
- [ ] Update CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector (13.0.0 → 13.1.1)
- [ ] Update McMaster.Extensions.CommandLineUtils (4.1.1 → 5.0.1)
- [ ] Update System.CommandLine (2.0.1 → 2.0.5)
- [ ] Update ModelContextProtocol (0.8.0-preview.1 → 1.0.0-rc.1)
- [ ] Update Microsoft.CodeAnalysis.NetAnalyzers (10.0.103 → 10.0.201)
- [ ] Update Microsoft.CodeAnalysis.CSharp.CodeStyle (5.0.0 → 5.3.0)
- [ ] Run tests to verify no breaking changes
- [ ] Commit changes

## Notes

### Prerequisite

Task 446 (fix repo audit) should be completed first to establish a clean baseline.

### Packages to Update

| Package | Current | Latest |
|---------|---------|--------|
| Microsoft.Extensions.DependencyInjection | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Configuration | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Configuration.Binder | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Configuration.CommandLine | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Configuration.Json | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Configuration.EnvironmentVariables | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Hosting | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Logging | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Logging.Console | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Options | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Options.DataAnnotations | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Configuration.UserSecrets | 10.0.3 | 10.0.5 |
| Microsoft.Extensions.Http | 10.0.3 | 10.0.5 |
| TimeWarp.Terminal | 1.0.0-beta.5 | 1.0.0-beta.7 |
| Serilog | 4.2.0 | 4.3.1 |
| Serilog.Extensions.Logging | 9.0.2 | 10.0.0 |
| CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector | 13.0.0 | 13.1.1 |
| McMaster.Extensions.CommandLineUtils | 4.1.1 | 5.0.1 |
| System.CommandLine | 2.0.1 | 2.0.5 |
| ModelContextProtocol | 0.8.0-preview.1 | 1.0.0-rc.1 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.103 | 10.0.201 |
| Microsoft.CodeAnalysis.CSharp.CodeStyle | 5.0.0 | 5.3.0 |

### Command

```bash
ganda nuget outdated --update --force
```
