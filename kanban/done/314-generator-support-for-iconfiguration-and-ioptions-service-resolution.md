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

### Section Key Resolution
1. **Convention**: Strip "Options" suffix from class name: `DatabaseOptions` → `"Database"`
2. **Attribute**: `[ConfigurationKey("CustomSection")]` overrides convention

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
- [x] Implement `[ConfigurationKey]` attribute detection in handler extraction
- [x] Add generator test `generator-13-ioptions-parameter-injection.cs`

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` - Added IOptions detection and binding emission
- `source/timewarp-nuru-analyzers/generators/extractors/handler-extractor.cs` - Added [ConfigurationKey] attribute detection
- `source/timewarp-nuru-analyzers/generators/models/parameter-binding.cs` - Extended FromService to carry configuration key
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Set HasConfiguration and HasHelp to true by default
- `source/timewarp-nuru/configuration/configuration-key-attribute.cs` - New attribute for section key override
- `samples/_configuration/configuration-basics.cs` - Demonstrates IOptions<T> with convention and attribute
- `tests/timewarp-nuru-core-tests/generator/generator-13-ioptions-parameter-injection.cs` - New test file

## Notes

- `IOptionsSnapshot<T>` and `IOptionsMonitor<T>` are NOT supported - they're not useful in CLI apps
- Section key convention aligns with `timewarp-options-validation` package

## Related

- Task #321: Optimize Generator to Auto-Detect Configuration Usage

## Results

**Sample `configuration-basics.cs`:**
- `config show` - Shows IConfiguration and IOptions<DatabaseOptions>, IOptions<ApiSettings>
- `db connect` - Uses IOptions<DatabaseOptions> (convention)
- `api call {endpoint}` - Uses IOptions<ApiSettings> with [ConfigurationKey("Api")] (attribute)

**Generator tests:**
- All 13 generator tests pass
- New test `generator-13-ioptions-parameter-injection.cs` covers:
  - Convention-based section key (strips "Options" suffix)
  - `[ConfigurationKey]` attribute override
  - `IConfiguration` parameter injection
  - `Options.Create()` wrapper
  - Comment with section name
  - Default fallback (`?? new()`)
