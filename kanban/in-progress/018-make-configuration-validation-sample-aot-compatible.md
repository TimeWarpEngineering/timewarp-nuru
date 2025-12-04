# Make Configuration Validation Sample AOT-Compatible

**Created:** 2025-10-25
**Related:** Issue #71 (ValidateOnStart support)

## Problem

The `samples/configuration/configuration-validation.cs` sample demonstrates ValidateOnStart() functionality but currently produces AOT warnings when using configuration binding:

```
warning IL2026: Using member 'OptionsBuilderConfigurationExtensions.Bind<TOptions>' which has 'RequiresUnreferencedCodeAttribute'
warning IL3050: Using member 'OptionsBuilderConfigurationExtensions.Bind<TOptions>' which has 'RequiresDynamicCodeAttribute'
```

These warnings come from using reflection-based `.Bind()` methods for configuration binding, which are not AOT-friendly.

## Current Implementation

```csharp
services.AddOptions<ServerOptions>()
  .Bind(config.GetSection("Server"))  // ⚠️ Uses reflection
  .ValidateDataAnnotations()           // ⚠️ Uses reflection
  .ValidateOnStart();
```

## Goal

Since Nuru is designed with AOT in mind (even for runfiles), the configuration validation sample should demonstrate AOT-compatible patterns:

1. Use configuration binding source generators instead of reflection-based `.Bind()`
2. Show FluentValidation (already AOT-friendly) as the preferred approach
3. Document the tradeoffs between reflection-based and source generator approaches
4. Ensure the sample can be published with `PublishAot=true` without warnings

## Proposed Solution

### Approach 1: Configuration Binding Source Generator

```csharp
#:property EnableConfigurationBindingGenerator=true

// Instead of .Bind(), use:
services.Configure<ServerOptions>(config.GetSection("Server"));
// Source generator creates binding code at compile-time
```

### Approach 2: Manual Binding (Fully AOT-Safe)

```csharp
services.AddOptions<ServerOptions>()
  .Configure(options =>
  {
    var section = config.GetSection("Server");
    options.Host = section["Host"] ?? "localhost";
    options.Port = section.GetValue<int>("Port", 8080);
    // ... explicit binding
  })
  .ValidateOnStart();
```

### Approach 3: TimeWarp.OptionsValidation (Recommended)

```csharp
// Already uses source generators internally
services
  .AddFluentValidatedOptions<ApiOptions, ApiOptionsValidator>(config)
  .ValidateOnStart();
```

## Implementation Tasks

- [ ] Update sample to use `EnableConfigurationBindingGenerator=true`
- [ ] Replace `.Bind()` calls with generator-friendly alternatives
- [ ] Add comments explaining AOT compatibility
- [ ] Test with `dotnet publish -p:PublishAot=true`
- [ ] Verify zero IL2026/IL3050 warnings
- [ ] Update sample documentation to emphasize AOT compatibility
- [ ] Consider creating two versions:
  - `configuration-validation.cs` - AOT-compatible (recommended)
  - `configuration-validation-reflection.cs` - Reflection-based (simpler, JIT only)

## References

- [Microsoft Docs: AOT Configuration](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/configuration-binding)
- Configuration binding source generator documentation
- TimeWarp.OptionsValidation source generator approach
- Existing `configuration-basics.cs` sample (has same warnings)

## Success Criteria

✅ Sample compiles with zero AOT warnings
✅ Sample works when published with `PublishAot=true`
✅ Documentation clearly explains AOT considerations
✅ Demonstrates best practices for AOT-friendly configuration validation
