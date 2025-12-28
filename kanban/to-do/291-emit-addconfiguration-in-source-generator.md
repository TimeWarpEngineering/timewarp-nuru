# Emit AddConfiguration in source generator

## Parent

#265 Epic: V2 Source Generator Implementation

## Description

When user calls `AddConfiguration()` in DSL, the source generator should emit configuration loading code that runs at startup:
- Loads `appsettings.json` and environment-specific variants
- Loads user secrets (in Development)
- Adds environment variables
- Adds command line arguments

This is a native builder method, not an extension.

## Example DSL

```csharp
NuruApp.CreateBuilder(args)
  .AddConfiguration()
  .Map("greet {name}")
    .WithHandler((string name, IConfiguration config) => 
      $"Hello {name} from {config["AppName"]}")
    .Done()
  .Build();
```

## Generated Code

```csharp
// At start of RunAsync_Intercepted (when AddConfiguration was called):
string basePath = AppContext.BaseDirectory;
string env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
  ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
  ?? "Production";

IConfigurationRoot configuration = new ConfigurationBuilder()
  .SetBasePath(basePath)
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
  .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
  .AddEnvironmentVariables()
  .AddCommandLine(args)
  .Build();
```

## Checklist

### Interpreter
- [ ] Add `AddConfiguration()` to DSL dispatcher (native method, not extension)
- [ ] Set `AppModel.HasConfiguration = true` flag
- [ ] Optionally capture args parameter if provided

### Emitter
- [ ] Emit configuration building code at start of `RunAsync_Intercepted`
- [ ] Create `IConfiguration` local variable for handler injection
- [ ] Handle user secrets for Development environment

### Handler Integration
- [ ] Support `IConfiguration` parameter injection in handlers
- [ ] Pass configuration to handlers that request it

### Cleanup
- [ ] Remove runtime `AddConfiguration()` implementation from builder
- [ ] Keep `AddConfiguration()` as no-op stub that returns builder

## Reference Implementation

See: `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.configuration.cs`

## Notes

Configuration is runtime data (file contents, env vars), but the **code to load it** is deterministic and can be emitted by the source generator. This follows the same pattern as `--check-updates` (#290).
