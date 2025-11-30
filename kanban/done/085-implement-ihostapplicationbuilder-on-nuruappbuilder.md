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
- [x] Add Microsoft.Extensions.Hosting.Abstractions package reference
- [x] Add Microsoft.Extensions.Diagnostics.Abstractions if needed for IMetricsBuilder
- [x] Implement IHostEnvironment (or use existing implementation)
- [x] Expose ILoggingBuilder
- [x] Expose IMetricsBuilder
- [x] Add Properties dictionary
- [x] Implement ConfigureContainer method
- [x] Update NuruAppBuilder to implement IHostApplicationBuilder
- [x] Verify AOT compatibility (solution builds)

### Testing
- [x] Test that Aspire AddAppDefaults() style extensions work
- [x] Update AspireHostOtel sample to use the new integration
- [ ] Verify telemetry flows to Aspire dashboard (requires human testing)

## Results

Successfully implemented `IHostApplicationBuilder` on `NuruAppBuilder`:

### Files Modified
- `Directory.Packages.props` - Added `Microsoft.Extensions.Hosting.Abstractions` package
- `Source/TimeWarp.Nuru.Core/TimeWarp.Nuru.Core.csproj` - Added package reference
- `Source/TimeWarp.Nuru.Core/GlobalUsings.cs` - Added required using statements
- `Source/TimeWarp.Nuru.Core/NuruAppBuilder.HostApplicationBuilder.cs` - **New file** with interface implementation

### Implementation Details

Created a new partial class file `NuruAppBuilder.HostApplicationBuilder.cs` that:
1. Implements `IHostApplicationBuilder` interface on `NuruAppBuilder`
2. Provides public properties: `HostConfiguration`, `HostEnvironment`, `Logging`, `Metrics`, `Properties`
3. Uses explicit interface implementations for `Configuration` and `Environment` (to avoid naming conflicts)
4. Includes helper classes: `NuruHostEnvironment`, `NuruLoggingBuilder`, `NuruMetricsBuilder`

### Sample Updated
- `samples/aspire-host-otel/AspireHostOtel.NuruClient/Program.cs` - Demonstrates Aspire-style extension methods
- `samples/aspire-host-otel/Overview.md` - Documents the new feature

### Benefit
Any extension method targeting `IHostApplicationBuilder` (like Aspire's `AddAppDefaults()`) now works with NuruAppBuilder:

```csharp
NuruAppBuilder builder = NuruApp.CreateBuilder(args);
builder.AddNuruClientDefaults();  // Uses builder.Logging, builder.Services, etc.
```

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
