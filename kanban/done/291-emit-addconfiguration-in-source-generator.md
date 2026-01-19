# Emit AddConfiguration in source generator

## Parent

#265 Epic: V2 Source Generator Implementation

## Description

When user calls `AddConfiguration()` in DSL, the source generator should emit configuration loading code that runs at startup:
- Loads `appsettings.json` and environment-specific variants
- Loads user secrets (in DEBUG builds only, via `#if DEBUG`)
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
string __basePath = global::System.AppContext.BaseDirectory;
string __env = global::System.Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
  ?? global::System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
  ?? "Production";

string? __appName = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
if (!string.IsNullOrEmpty(__appName))
{
  __appName = __appName
    .Replace(global::System.IO.Path.DirectorySeparatorChar, '_')
    .Replace(global::System.IO.Path.AltDirectorySeparatorChar, '_');
}

global::Microsoft.Extensions.Configuration.IConfigurationBuilder __configBuilder = 
  new global::Microsoft.Extensions.Configuration.ConfigurationBuilder()
    .SetBasePath(__basePath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{__env}.json", optional: true, reloadOnChange: false);

if (!string.IsNullOrEmpty(__appName))
{
  __configBuilder
    .AddJsonFile($"{__appName}.settings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"{__appName}.settings.{__env}.json", optional: true, reloadOnChange: false);
}

#if DEBUG
__configBuilder.AddUserSecrets(global::System.Reflection.Assembly.GetEntryAssembly()!, optional: true, reloadOnChange: false);
#endif

__configBuilder.AddEnvironmentVariables();
__configBuilder.AddCommandLine(args);

global::Microsoft.Extensions.Configuration.IConfigurationRoot configuration = __configBuilder.Build();
```

## Files to Modify

| Action | File |
|--------|------|
| Create | `source/timewarp-nuru-analyzers/generators/emitters/configuration-emitter.cs` |
| Modify | `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` |
| Modify | `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` |
| Modify | `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.configuration.cs` |

## Checklist

### Pre-existing (Already Done)
- [x] `AddConfiguration()` recognized in DSL dispatcher
- [x] `AppModel.HasConfiguration` flag exists
- [x] `IrAppBuilder.AddConfiguration()` sets flag

### Phase 1: Create ConfigurationEmitter
- [ ] Create `configuration-emitter.cs`
- [ ] Emit environment detection (`DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` / "Production")
- [ ] Emit sanitized application name extraction from entry assembly
- [ ] Emit `ConfigurationBuilder` initialization with `SetBasePath(AppContext.BaseDirectory)`
- [ ] Emit `appsettings.json` and `appsettings.{env}.json`
- [ ] Emit `{appName}.settings.json` and `{appName}.settings.{env}.json` (conditional)
- [ ] Emit `#if DEBUG` block with `AddUserSecrets()`
- [ ] Emit `AddEnvironmentVariables()`
- [ ] Emit `AddCommandLine(args)`
- [ ] Emit `IConfigurationRoot configuration` local variable

### Phase 2: Update InterceptorEmitter
- [ ] Add `Microsoft.Extensions.Configuration` using
- [ ] Add `Microsoft.Extensions.Configuration.Json` using
- [ ] Add `Microsoft.Extensions.Configuration.EnvironmentVariables` using
- [ ] Add `#if DEBUG` using for `Microsoft.Extensions.Configuration.UserSecrets`
- [ ] Call `ConfigurationEmitter.Emit()` at start of `EmitMethodBody()` when `HasConfiguration`

### Phase 3: Update ServiceResolverEmitter
- [ ] Detect `IConfiguration` parameter type (full name check)
- [ ] Detect `IConfigurationRoot` parameter type (full name check)
- [ ] Emit `configuration` local variable reference instead of `GetRequiredService` for these types

### Phase 4: Runtime Cleanup
- [ ] Convert `AddConfiguration()` to no-op stub (keep method signature for API compatibility)
- [ ] Remove `DetermineConfigurationBasePath()` method
- [ ] Remove `GetSanitizedApplicationName()` method
- [ ] Keep `ConfigureServices()` methods (they remain runtime)
- [ ] Keep `UseLogging()` method (remains runtime)
- [ ] Keep `UseTerminal()` method (remains runtime)

### Phase 5: Verification
- [ ] Build solution successfully
- [ ] Test with sample app using `AddConfiguration()` and `IConfiguration` parameter

## Design Decisions

1. **Configuration Base Path**: Use `AppContext.BaseDirectory` only (simplified from runtime's complex fallback chain)

2. **User Secrets**: Emit with `#if DEBUG` preprocessor directive
   - Debug builds: Include user secrets, require `Microsoft.Extensions.Configuration.UserSecrets` package
   - Release builds: Exclude user secrets completely, no package dependency needed
   - Rationale: User secrets are a dev-only concept; use env vars for secrets in release testing

3. **IConfiguration Injection**: Detect `IConfiguration`/`IConfigurationRoot` parameter types and use the local `configuration` variable directly instead of DI resolution
   - Reason: Configuration is built in generated code before the service provider exists

4. **Variable Naming**: Use `__` prefix for generated local variables to avoid conflicts with user code

## Reference Implementation

See: `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.configuration.cs`

## Notes

- Configuration is runtime data (file contents, env vars), but the **code to load it** is deterministic and can be emitted by the source generator
- This follows the same pattern as `--check-updates` (#290)
- The interpreter already recognizes `AddConfiguration()` - only emitter work is needed
