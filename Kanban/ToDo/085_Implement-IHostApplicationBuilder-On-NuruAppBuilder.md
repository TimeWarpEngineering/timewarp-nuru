# Implement IHostApplicationBuilder on NuruAppBuilder

## Description

Implement `IHostApplicationBuilder` interface on `NuruAppBuilder` to enable seamless integration with Aspire and other .NET ecosystem extensions that target this interface.

This is a lightweight change - just implementing an interface from `Microsoft.Extensions.Hosting.Abstractions` (not the heavy hosting runtime).

## Requirements

- Add `Microsoft.Extensions.Hosting.Abstractions` package to TimeWarp.Nuru.Core
- Implement all `IHostApplicationBuilder` interface members on `NuruAppBuilder`:
  - `Configuration` (IConfigurationManager) - we have IConfiguration, may need adapter
  - `Environment` (IHostEnvironment) - need to create/expose
  - `Logging` (ILoggingBuilder) - need to expose
  - `Metrics` (IMetricsBuilder) - need to create/expose
  - `Properties` (IDictionary<object, object>) - simple property bag
  - `Services` (IServiceCollection) - already have this
  - `ConfigureContainer<TContainerBuilder>()` method
- Ensure AOT compatibility is maintained
- Verify Aspire's `AddAppDefaults()` extension works with NuruAppBuilder

## Checklist

### Implementation
- [ ] Add Microsoft.Extensions.Hosting.Abstractions package reference
- [ ] Add Microsoft.Extensions.Diagnostics.Abstractions if needed for IMetricsBuilder
- [ ] Implement IHostEnvironment (or use existing implementation)
- [ ] Expose ILoggingBuilder
- [ ] Expose IMetricsBuilder
- [ ] Add Properties dictionary
- [ ] Implement ConfigureContainer method
- [ ] Update NuruAppBuilder to implement IHostApplicationBuilder
- [ ] Verify AOT compatibility

### Testing
- [ ] Test that Aspire AddAppDefaults() works
- [ ] Update AspireHostOtel sample to use the new integration
- [ ] Verify telemetry flows to Aspire dashboard

## Notes

The interface requires these properties (from `Microsoft.Extensions.Hosting.Abstractions`):

```csharp
public interface IHostApplicationBuilder
{
    IConfigurationManager Configuration { get; }
    IHostEnvironment Environment { get; }
    ILoggingBuilder Logging { get; }
    IMetricsBuilder Metrics { get; }
    IDictionary<object, object> Properties { get; }
    IServiceCollection Services { get; }

    void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null);
}
```

Dependencies of `Microsoft.Extensions.Hosting.Abstractions` (for net10.0) are all abstractions packages:
- Microsoft.Extensions.Configuration.Abstractions
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Diagnostics.Abstractions
- Microsoft.Extensions.FileProviders.Abstractions
- Microsoft.Extensions.Logging.Abstractions

We already reference most of these through our existing dependencies.
