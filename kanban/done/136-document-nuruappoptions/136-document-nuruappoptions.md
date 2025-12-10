# Document NuruAppOptions Configuration

## Description

Create reference documentation for `NuruAppOptions` which configures all auto-wired extensions when using `NuruApp.CreateBuilder()`.

## Requirements

- Document all properties with descriptions and examples
- Show common configuration patterns
- Explain when to use NuruAppOptions vs raw builder methods

## Checklist

- [x] Create new page: documentation/user/reference/nuru-app-options.md
- [x] Document ConfigureRepl property with example
- [x] Document ConfigureTelemetry property with example
- [x] Document ConfigureCompletion property with example
- [x] Document ConfigureHelp property with example
- [x] Document InteractiveRoutePatterns property
- [x] Document DisableVersionRoute and DisableCheckUpdatesRoute
- [x] Add link from getting-started.md and features/overview.md

## Notes

Properties to document:
```csharp
NuruAppOptions
{
  ConfigureRepl          // Action<ReplOptions>
  ConfigureTelemetry     // Action<NuruTelemetryOptions>
  ConfigureCompletion    // Action<CompletionSourceRegistry>
  ConfigureHelp          // Action<HelpOptions>
  InteractiveRoutePatterns // default: "--interactive,-i"
  DisableVersionRoute    // bool (default: false)
  DisableCheckUpdatesRoute // bool (default: false)
}
```

Source: `source/timewarp-nuru/nuru-app-options.cs`
