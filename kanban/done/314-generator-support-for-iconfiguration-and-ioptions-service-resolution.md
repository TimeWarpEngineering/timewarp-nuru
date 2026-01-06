# Generator support for IConfiguration and IOptions service resolution

## Description

Enable handlers to use `IConfiguration` and `IOptions<T>` parameters with static resolution (no runtime DI container).

## Solution

### IConfiguration
Set `HasConfiguration = true` unconditionally in generator so `configuration` variable is always emitted.

### IOptions<T>
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
2. `[ConfigurationKey("CustomSection")]` attribute can override (attribute created, detection not yet implemented)

## Checklist

- [x] Set `HasConfiguration = true` unconditionally in `nuru-generator.cs`
- [x] Verify `IConfiguration` parameter resolution works
- [x] Create `ConfigurationKeyAttribute` in `timewarp-nuru`
- [x] Modify `ServiceResolverEmitter` to detect `IOptions<T>` pattern
- [x] Extract inner type from `IOptions<T>` (e.g., `DatabaseOptions`)
- [x] Implement section key convention (strip "Options" suffix)
- [x] Emit `configuration.GetSection().Get<T>()` binding code
- [x] Emit `Options.Create()` wrapper
- [x] Test with `samples/_configuration/configuration-basics.cs`
- [x] Set `HasHelp = true` unconditionally (help should be automatic for CreateBuilder apps)

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` - Added IOptions detection and binding emission
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Set HasConfiguration and HasHelp to true by default
- `source/timewarp-nuru/configuration/configuration-key-attribute.cs` - New attribute for future override support
- `samples/_configuration/configuration-basics.cs` - Simplified to demonstrate IOptions<T> parameter injection

## Notes

- `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` are NOT supported - they're not useful in CLI apps
- Section key convention aligns with `timewarp-options-validation` package
- `[ConfigurationKey]` attribute detection not yet implemented (future task)

## Related

- Task #321: Optimize Generator to Auto-Detect Configuration Usage

## Results

All three commands in configuration-basics.cs now work:
- `config show` - Shows IConfiguration and IOptions<DatabaseOptions>, IOptions<ApiOptions>
- `db connect` - Uses IOptions<DatabaseOptions>
- `api call {endpoint}` - Uses route parameter + IOptions<ApiOptions>
