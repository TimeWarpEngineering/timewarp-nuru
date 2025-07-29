# App Configuration Comparison: Cocona vs Nuru

This example demonstrates how to integrate .NET Configuration with command-line applications.

## Cocona Implementation

```csharp
var builder = CoconaApp.CreateBuilder(args);

var app = builder.Build();
app.AddCommand("run", ([FromService]IConfiguration configuration) =>
{
    var configValue1 = configuration.GetValue<bool>("ConfigValue1");
    var configValue2 = configuration.GetValue<string>("ConfigValue2");

    Console.WriteLine($"ConfigValue1: {configValue1}");
    Console.WriteLine($"ConfigValue2: {configValue2}");
});

app.Run();
```

## Nuru Implementation (Delegate)

```csharp
// Build configuration manually for delegate approach
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Access configuration values
var configValue1 = configuration.GetValue<bool>("ConfigValue1");
var configValue2 = configuration.GetValue<string>("ConfigValue2");

// Create app with route that uses configuration
var app = new NuruAppBuilder()
    .AddRoute("run", () =>
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
// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Create app with DI and configuration
var builder = new NuruAppBuilder()
    .AddDependencyInjection(config => 
    {
        config.RegisterServicesFromAssembly(typeof(RunCommand).Assembly);
    });

// Add configuration to DI container
builder.Services.AddSingleton<IConfiguration>(configuration);

var app = builder
    .AddRoute<RunCommand>("run")
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);

// Command that receives configuration through DI
public class RunCommand : IRequest
{
    public class Handler : IRequestHandler<RunCommand>
    {
        private readonly IConfiguration _configuration;
        
        public Handler(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public async Task Handle(RunCommand request, CancellationToken cancellationToken)
        {
            var configValue1 = _configuration.GetValue<bool>("ConfigValue1");
            var configValue2 = _configuration.GetValue<string>("ConfigValue2");
            
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
- **Nuru (DI)**: Manual configuration building, then added to DI container

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