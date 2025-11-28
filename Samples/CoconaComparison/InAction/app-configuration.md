# App Configuration Comparison: Cocona vs Nuru

This example demonstrates how to integrate .NET Configuration with command-line applications.

## Cocona Implementation

```csharp
CoconaAppBuilder builder = CoconaApp.CreateBuilder(args);

CoconaApp app = builder.Build();
app.AddCommand("run", ([FromService]IConfiguration configuration) =>
{
    bool configValue1 = configuration.GetValue<bool>("ConfigValue1");
    string? configValue2 = configuration.GetValue<string>("ConfigValue2");

    Console.WriteLine($"ConfigValue1: {configValue1}");
    Console.WriteLine($"ConfigValue2: {configValue2}");
});

app.Run();
```

## Nuru Implementation (Delegate)

```csharp
// Build configuration manually for delegate approach
IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Access configuration values
bool configValue1 = configuration.GetValue<bool>("ConfigValue1");
string? configValue2 = configuration.GetValue<string>("ConfigValue2");

// Create app with route that uses configuration
NuruApp app = new NuruAppBuilder()
    .Map("run", () =>
    {
        WriteLine($"ConfigValue1: {configValue1}");
        WriteLine($"ConfigValue2: {configValue2}");
    },
    description: "Run the application and display configuration values")
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);
```

## Nuru Implementation (DI/Class-based)

```csharp
// Create app with DI and automatic configuration setup
NuruApp app = new NuruAppBuilder()
    .AddDependencyInjection(config =>
    {
        config.RegisterServicesFromAssembly(typeof(RunCommand).Assembly);
    })
    .AddConfiguration(args)  // Automatically sets up standard configuration sources
    .Map<RunCommand>("run")
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);

// Command that receives configuration through DI
public sealed class RunCommand : IRequest
{
    internal sealed class Handler : IRequestHandler<RunCommand>
    {
        private readonly IConfiguration _configuration;

        public Handler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Handle(RunCommand request, CancellationToken cancellationToken)
        {
            bool configValue1 = _configuration.GetValue<bool>("ConfigValue1");
            string? configValue2 = _configuration.GetValue<string>("ConfigValue2");

            WriteLine($"ConfigValue1: {configValue1}");
            WriteLine($"ConfigValue2: {configValue2}");

            await Task.CompletedTask;
        }
    }
}
```

## Key Differences

### Configuration Setup
- **Cocona**: Uses Host Builder pattern, configuration is automatically set up
- **Nuru (Delegate)**: Manual configuration building with full control
- **Nuru (DI)**: Uses `.AddConfiguration(args)` for automatic setup (similar to Cocona)

### Dependency Injection
- **Cocona**: Uses `[FromService]` attribute for configuration injection
- **Nuru (Delegate)**: Configuration values captured in closure
- **Nuru (DI)**: Configuration injected through constructor

### Code Organization
- **Cocona**: More integrated with ASP.NET Core hosting model
- **Nuru**: More explicit configuration handling, giving developers full control

## Configuration Files

Both frameworks use standard .NET configuration files:

**appsettings.json**:
```json
{
  "ConfigValue1": false,
  "ConfigValue2": "this is a configuration file (Production)"
}
```

**appsettings.Development.json**:
```json
{
  "ConfigValue1": true,
  "ConfigValue2": "this is a configuration file (Development)"
}
```

## Usage

All implementations support environment-specific configuration:

```bash
# Uses Development configuration
DOTNET_ENVIRONMENT=Development ./app-configuration run

# Uses Production configuration (default)
./app-configuration run
```

## Evaluation

With the addition of the `.AddConfiguration()` extension method, Nuru now offers the best of both worlds:

1. **Delegate Approach**: Full control over configuration for simple scenarios or when you need custom configuration sources
2. **DI Approach with AddConfiguration**: Matches Cocona's convenience for standard configuration scenarios

The `.AddConfiguration()` method automatically sets up:
- `appsettings.json` (optional)
- Environment-specific configuration files
- Environment variables
- Command line arguments

This brings Nuru to feature parity with Cocona's Host Builder pattern while maintaining the flexibility to manually configure when needed. The performance characteristics remain unchanged - the delegate approach still offers minimal overhead while the DI approach provides enterprise patterns.