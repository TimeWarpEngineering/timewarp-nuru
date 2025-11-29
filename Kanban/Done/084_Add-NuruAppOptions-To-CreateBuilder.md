# Add NuruAppOptions to CreateBuilder

## Description

`NuruApp.CreateBuilder()` auto-wires all extensions (telemetry, REPL, interactive routes) but provides no way to configure them without causing duplicate route warnings. Users who want to customize REPL options or telemetry options must currently use `CreateSlimBuilder()` and manually wire everything.

Add a `NuruAppOptions` parameter to `CreateBuilder()` that allows configuration of all auto-wired extensions.

## Requirements

- Create `NuruAppOptions` class with configuration callbacks for REPL and Telemetry
- Update `CreateBuilder` signature to accept `NuruAppOptions`
- Update `UseAllExtensions` to use the provided options
- Maintain backward compatibility (options parameter is optional)

## Checklist

- [x] Create `NuruAppOptions` class in `Source/TimeWarp.Nuru/`
- [x] Add `ConfigureRepl` property (`Action<ReplOptions>?`)
- [x] Add `ConfigureTelemetry` property (`Action<NuruTelemetryOptions>?`)
- [x] Add `InteractiveRoutePatterns` property (default: "--interactive,-i")
- [x] Update `NuruApp.CreateBuilder` to accept `NuruAppOptions?`
- [x] Update `UseAllExtensions` to accept and use `NuruAppOptions`
- [x] Update AspireHostOtel sample to use new API
- [x] Verify no duplicate route warnings

## Notes

### Current Problem
```csharp
// This causes duplicate route warnings:
NuruApp.CreateBuilder(args)  // Already calls AddReplSupport()
  .AddReplSupport(options => { ... })  // Duplicate!
```

### Proposed Solution
```csharp
NuruApp.CreateBuilder(args, new NuruAppOptions
{
  ConfigureRepl = options =>
  {
    options.Prompt = "otel> ";
    options.WelcomeMessage = "Custom welcome...";
  },
  ConfigureTelemetry = options =>
  {
    options.ServiceName = "my-app";
  }
})
.ConfigureServices(services => { ... })
.Build();
```

### Implementation Details

1. Create `NuruAppOptions` class:
```csharp
public class NuruAppOptions
{
  public Action<ReplOptions>? ConfigureRepl { get; set; }
  public Action<NuruTelemetryOptions>? ConfigureTelemetry { get; set; }
  public string InteractiveRoutePatterns { get; set; } = "--interactive,-i";
}
```

2. Update `UseAllExtensions`:
```csharp
public static NuruAppBuilder UseAllExtensions(
  this NuruAppBuilder builder,
  NuruAppOptions? options = null)
{
  options ??= new NuruAppOptions();

  builder.UseTelemetry(options.ConfigureTelemetry ?? (_ => { }));
  builder.AddReplSupport(options.ConfigureRepl);
  builder.AddInteractiveRoute(options.InteractiveRoutePatterns);

  return builder;
}
```
