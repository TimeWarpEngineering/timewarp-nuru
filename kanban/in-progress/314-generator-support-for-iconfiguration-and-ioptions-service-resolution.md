# Generator support for IConfiguration and IOptions service resolution

## Description

Enable handlers to use `IConfiguration` and `IOptions<T>` parameters with static resolution (no runtime DI container).

**Status:** `IConfiguration` is now working. `IOptions<T>` implementation in progress.

## Problem

Handlers with `IOptions<T>` parameters fail because the generator doesn't know how to resolve them:
```csharp
// ERROR: Service IOptions<DatabaseOptions> not registered
global::Microsoft.Extensions.Options.IOptions<global::DatabaseOptions> dbOptions = default!;
```

## Solution

### IConfiguration (DONE)
Set `HasConfiguration = true` unconditionally in generator so `configuration` variable is always emitted.

### IOptions<T> (IN PROGRESS)
When handler has `IOptions<T>` parameter, emit static configuration binding:

```csharp
// Generated code:
global::DatabaseOptions __dbOptionsValue = 
  configuration.GetSection("Database").Get<global::DatabaseOptions>() ?? new();
global::Microsoft.Extensions.Options.IOptions<global::DatabaseOptions> dbOptions = 
  global::Microsoft.Extensions.Options.Options.Create(__dbOptionsValue);
```

### Section Key Convention
1. Strip "Options" suffix from class name: `DatabaseOptions` → `"Database"`
2. `[ConfigurationKey("CustomSection")]` attribute overrides (future enhancement)

## Checklist

- [x] Set `HasConfiguration = true` unconditionally in `nuru-generator.cs`
- [x] Verify `IConfiguration` parameter resolution works
- [ ] Create `ConfigurationKeyAttribute` in `timewarp-nuru` (for future attribute override)
- [ ] Modify `ServiceResolverEmitter` to detect `IOptions<T>` pattern
- [ ] Extract inner type from `IOptions<T>` (e.g., `DatabaseOptions`)
- [ ] Implement section key convention (strip "Options" suffix)
- [ ] Emit `configuration.GetSection().Get<T>()` binding code
- [ ] Emit `Options.Create()` wrapper
- [ ] Test with `samples/_configuration/configuration-basics.cs`

## Files to Modify

- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` - Add IOptions detection and binding emission
- `source/timewarp-nuru/configuration/configuration-key-attribute.cs` - New file (future use)

## Generated Output Example

**Input handler:**
```csharp
Task ShowConfigAsync(IOptions<DatabaseOptions> dbOptions, IOptions<ApiOptions> apiOptions)
```

**Generated code:**
```csharp
// Resolve IOptions<DatabaseOptions>
global::DatabaseOptions __dbOptionsValue = 
  configuration.GetSection("Database").Get<global::DatabaseOptions>() ?? new();
global::Microsoft.Extensions.Options.IOptions<global::DatabaseOptions> dbOptions = 
  global::Microsoft.Extensions.Options.Options.Create(__dbOptionsValue);

// Resolve IOptions<ApiOptions>
global::ApiOptions __apiOptionsValue = 
  configuration.GetSection("Api").Get<global::ApiOptions>() ?? new();
global::Microsoft.Extensions.Options.IOptions<global::ApiOptions> apiOptions = 
  global::Microsoft.Extensions.Options.Options.Create(__apiOptionsValue);

await global::Handlers.ShowConfigAsync(dbOptions, apiOptions);
```

## Notes

- `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` are NOT supported - they're not useful in CLI apps
- Section key convention aligns with `timewarp-options-validation` package
- Attribute-based override deferred to future task (convention handles most cases)

## Related

- Task #321: Optimize Generator to Auto-Detect Configuration Usage
