# Configuration and Dependency Injection

TimeWarp.Nuru integrates with Microsoft.Extensions.Configuration and Microsoft.Extensions.DependencyInjection, providing familiar patterns for .NET developers.

## Enabling Configuration

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Build();
```

Configuration is automatically loaded from these sources (in order of precedence):

1. `appsettings.json` - Shared configuration for all apps in the directory
2. `appsettings.{Environment}.json` - Environment-specific shared configuration
3. `{appname}.settings.json` - Application-specific configuration (detected from entry assembly)
4. `{appname}.settings.{Environment}.json` - Environment-specific application configuration
5. Environment variables
6. Command-line arguments (highest precedence)

All configuration files are optional. Later sources override earlier ones.

## Strongly-Typed Options

Define options classes that map to configuration sections:

```csharp
public class DatabaseOptions
{
  public string ConnectionString { get; set; } = string.Empty;
  public int TimeoutSeconds { get; set; } = 30;
}
```

**Convention**: A class named `DatabaseOptions` automatically binds to the `"Database"` configuration section (the "Options" suffix is stripped).

**Attribute override**: Use `[ConfigurationKey("SectionName")]` to specify a custom section:

```csharp
[TimeWarp.Nuru.ConfigurationKey("Api")]
public class ApiSettings
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public int TimeoutSeconds { get; set; } = 30;
}
```

## Using Options in Handlers

Inject `IOptions<T>` or `IConfiguration` directly into handler parameters:

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("db connect")
    .WithHandler(ConnectToDatabaseAsync)
    .AsCommand()
    .Done()
  .Build();

async Task ConnectToDatabaseAsync(IOptions<DatabaseOptions> dbOptions)
{
  DatabaseOptions db = dbOptions.Value;
  Console.WriteLine($"Server: {db.Host}:{db.Port}");
  Console.WriteLine($"Database: {db.DatabaseName}");
}
```

Mix route parameters with injected options:

```csharp
async Task CallApiAsync(string endpoint, IOptions<ApiSettings> apiOptions)
{
  ApiSettings api = apiOptions.Value;
  string fullUrl = $"{api.BaseUrl}/{endpoint}";
  Console.WriteLine($"Calling: {fullUrl}");
}
```

## Command-Line Overrides

Override any configuration value from the command line using colon-separated syntax:

```bash
# Override single value
./myapp connect --Database:ConnectionString="Server=prod;Database=main"

# Override multiple values
./myapp run \
  --Database:Host=prod-server \
  --Database:Port=5433 \
  --Api:TimeoutSeconds=60
```

Command-line arguments have the highest precedence and override all other configuration sources.

## Configuration Validation

### Using IValidateOptions<T>

Create validators that implement `IValidateOptions<T>` for AOT-compatible validation:

```csharp
public class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
  public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
  {
    List<string> failures = [];

    if (string.IsNullOrWhiteSpace(options.ConnectionString))
      failures.Add("Connection string is required");

    if (options.TimeoutSeconds < 1 || options.TimeoutSeconds > 300)
      failures.Add("Timeout must be between 1 and 300 seconds");

    return failures.Count > 0
      ? ValidateOptionsResult.Fail(failures)
      : ValidateOptionsResult.Success;
  }
}
```

The generator automatically detects validators and runs them at startup. Invalid configuration throws `OptionsValidationException` during `Build()` (fail-fast behavior).

### Using DataAnnotations

For simpler validation, use DataAnnotations with `ConfigureServices`:

```csharp
public class DatabaseOptions
{
  [Required]
  public string ConnectionString { get; set; } = string.Empty;
  
  [Range(1, 300)]
  public int TimeoutSeconds { get; set; } = 30;
}

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices((services, config) =>
  {
    services.AddOptions<DatabaseOptions>()
      .Bind(config.GetSection("Database"))
      .ValidateDataAnnotations()
      .ValidateOnStart();
  })
  .Build();
```

## Dependency Injection

### ConfigureServices Overloads

Register services using `ConfigureServices`:

```csharp
// When you don't need configuration access
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(services =>
  {
    services.AddSingleton<IMyService, MyService>();
    services.AddScoped<IRepository, Repository>();
  })
  .Build();

// When you need access to configuration
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices((services, config) =>
  {
    services.Configure<DatabaseOptions>(config.GetSection("Database"));
    
    string? environment = config["Environment"];
    if (environment == "Development")
      services.AddSingleton<INotificationService, ConsoleNotificationService>();
    else
      services.AddSingleton<INotificationService, EmailNotificationService>();
  })
  .Build();
```

### Service Lifetimes

Standard .NET service lifetimes are supported:

- `AddSingleton<T>()` - One instance for the application lifetime
- `AddScoped<T>()` - One instance per command execution
- `AddTransient<T>()` - New instance every time

## User Secrets

For development secrets (not committed to source control):

```bash
# Initialize user secrets for a project
dotnet user-secrets init

# Set a secret
dotnet user-secrets set "Database:ConnectionString" "Server=dev;..."

# List secrets
dotnet user-secrets list
```

User secrets are automatically loaded when running in the Development environment.

## Configuration File Structure

Example `myapp.settings.json`:

```json
{
  "AppName": "My CLI Application",
  "Environment": "Development",
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "DatabaseName": "myapp_dev",
    "ConnectionString": "Host=localhost;Database=myapp_dev",
    "TimeoutSeconds": 30
  },
  "Api": {
    "BaseUrl": "https://api.example.com",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  }
}
```

## See Also

- [samples/09-configuration/](../../../samples/09-configuration/) - Complete working examples
- [Builder API](../reference/builder-api.md) - ConfigureServices reference
