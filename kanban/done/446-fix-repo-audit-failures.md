# Fix repo audit failures

## Description

Fix the 5 failing checks from `ganda repo audit` to establish a clean repository baseline.

## Checklist

- [ ] Add BannedSymbols.txt file
- [ ] Add BannedApiAnalyzers configuration to Directory.Build.props
- [ ] Remove 8 orphaned packages from Directory.Packages.props
- [ ] Fix dev CLI capabilities description (expected 'Development CLI for timewarp-nuru')
- [ ] Add #region Purpose to 10 dev-cli files

## Notes

### Audit Results (2026-03-17)

```
Passed: 5 | Failed: 5

Errors:
- baseline-banned-symbols: BannedSymbols.txt is missing
- baseline-banned-api-analyzers: Directory.Build.props missing BannedApiAnalyzers config
- baseline-dev-cli-capabilities: Description mismatch

Warnings:
- baseline-cpm-consistency: 8 orphaned packages in Directory.Packages.props
- baseline-region-annotations: 10 files missing #region Purpose
```

### Orphaned Packages to Remove

- FluentValidation (12.1.1)
- Microsoft.Extensions.Hosting.Abstractions (10.0.3)
- TimeWarp.OptionsValidation (1.0.0-beta.4)
- TimeWarp.Nuru.Parsing ($(Version))
- TimeWarp.Nuru.Analyzers ($(Version))
- Serilog.Sinks.Seq (9.0.0)
- Serilog.Sinks.File (7.0.0)
- CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector (13.0.0)

### Files Missing #region Purpose

- tools/dev-cli/dev.cs
- tools/dev-cli/endpoints/ci-command.cs
- tools/dev-cli/endpoints/verify-samples-command.cs
- tools/dev-cli/endpoints/clean-command.cs
- tools/dev-cli/endpoints/analyze-command.cs
- tools/dev-cli/endpoints/test-command.cs
- tools/dev-cli/endpoints/build-command.cs
- tools/dev-cli/endpoints/self-install-command.cs
- tools/dev-cli/endpoints/check-version-command.cs
- tools/dev-cli/endpoints/format-command.cs
