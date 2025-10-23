# Configuration Example

This example demonstrates how to use `AddConfiguration()` with `ConfigureServices` to access configuration values.

## Files

- `configuration-basics.cs` - Main example script
- `configuration-basics.settings.json` - Script-specific configuration (automatically loaded)
- `appsettings.json` - (Optional) Shared configuration file for all apps in the directory

## Configuration Loading

TimeWarp.Nuru follows the .NET 10 configuration convention (from [runtime PR #116987](https://github.com/dotnet/runtime/pull/116987)):

1. `appsettings.json` - (Optional) Shared configuration for all apps in the directory
2. `appsettings.{Environment}.json` - (Optional) Environment-specific shared configuration
3. `{ApplicationName}.settings.json` - **Application-specific configuration** (automatically detected from entry assembly)
4. `{ApplicationName}.settings.{Environment}.json` - Environment-specific application configuration
5. Environment variables - Override file-based configuration
6. Command line arguments - Highest precedence

Configuration files are loaded in order, with later sources overriding earlier ones. All files are optional.

## Key Features Demonstrated

### 1. ConfigureServices with IConfiguration Access

```csharp
.ConfigureServices((services, config) =>
{
    if (config != null)
    {
        // Bind configuration sections to strongly-typed options
        services.Configure<DatabaseOptions>(config.GetSection("Database"));

        // Access configuration values
        string? appName = config["AppName"];
    }
})
```

### 2. Strongly-Typed Options Pattern

```csharp
public class DatabaseOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    // ...
}

// Injected into handlers via IOptions<T>
async Task ConnectToDatabaseAsync(IOptions<DatabaseOptions> dbOptions)
{
    DatabaseOptions db = dbOptions.Value;
    Console.WriteLine($"Server: {db.Host}:{db.Port}");
}
```

### 3. Conditional Service Registration

```csharp
.ConfigureServices((services, config) =>
{
    string? environment = config["Environment"];
    if (environment == "Development")
    {
        services.AddSingleton<INotificationService, ConsoleNotificationService>();
    }
    else
    {
        services.AddSingleton<INotificationService, EmailNotificationService>();
    }
})
```

## Running the Example

```bash
cd Samples/Configuration

# Show all configuration values
./configuration-basics.cs config show

# Connect to database using configured settings
./configuration-basics.cs db connect

# Call API endpoint
./configuration-basics.cs api call users

# Send notification (uses environment-based service)
./configuration-basics.cs notify "Hello World"

# Show help
./configuration-basics.cs --help
```

## Configuration File Structure

```json
{
  "AppName": "Nuru Configuration Demo",
  "Environment": "Development",
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "DatabaseName": "myapp_dev",
    "Timeout": 30
  },
  "Api": {
    "BaseUrl": "https://api.example.com",
    "TimeoutSeconds": 30,
    "RetryCount": 3
  }
}
```

## Two Overloads of ConfigureServices

```csharp
// Overload 1: When you don't need configuration
.ConfigureServices(services =>
{
    services.AddSingleton<IMyService, MyService>();
})

// Overload 2: When you need access to configuration
.ConfigureServices((services, config) =>
{
    if (config != null)
    {
        services.Configure<MyOptions>(config.GetSection("MySection"));
    }
})
```
