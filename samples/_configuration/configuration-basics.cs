#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION BASICS - AOT-COMPATIBLE CONFIGURATION WITH DI
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) which provides:
// - Full DI container setup
// - Configuration from appsettings.json, environment variables, command line args
// - Auto-help generation
// - AOT-compatible configuration binding with source generators
//
// Settings file: configuration-basics.settings.json (automatically discovered by .NET 10)
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(Handlers.ConfigureServices)
  .Map("config show")
    .WithHandler(Handlers.ShowConfigurationAsync)
    .WithDescription("Show current configuration values")
    .AsQuery()
    .Done()
  .Map("db connect")
    .WithHandler(Handlers.ConnectToDatabaseAsync)
    .WithDescription("Connect to database using config")
    .AsCommand()
    .Done()
  .Map("api call {endpoint}")
    .WithHandler(Handlers.CallApiAsync)
    .WithDescription("Call API endpoint using config")
    .AsQuery()
    .Done()
  .Map("notify {message}")
    .WithHandler(Handlers.SendNotificationAsync)
    .WithDescription("Send notification (uses environment-based service)")
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

internal static class Handlers
{
  internal static void ConfigureServices(IServiceCollection services)
  {
    // Get configuration from the service provider
    ServiceProvider sp = services.BuildServiceProvider();
    IConfiguration? config = sp.GetService<IConfiguration>();

    if (config != null)
    {
      // Bind configuration sections to strongly-typed options (AOT-compatible with source generator)
      services.AddOptions<DatabaseOptions>().Bind(config.GetSection("Database"));
      services.AddOptions<ApiOptions>().Bind(config.GetSection("Api"));

      // Register services conditionally based on configuration
      string? environment = config["Environment"];
      if (environment == "Development")
      {
        services.AddSingleton<INotificationService, ConsoleNotificationService>();
      }
      else
      {
        services.AddSingleton<INotificationService, EmailNotificationService>();
      }

      // Access configuration values directly
      string? appName = config["AppName"];
      WriteLine($"Configuring application: {appName ?? "Unknown"}");
    }
  }

  internal static async Task ShowConfigurationAsync(IOptions<DatabaseOptions> dbOptions, IOptions<ApiOptions> apiOptions, IConfiguration config)
  {
    WriteLine("\n=== Configuration Values ===");
    WriteLine($"App Name: {config["AppName"]}");
    WriteLine($"Environment: {config["Environment"]}");

    WriteLine("\nDatabase Configuration:");
    WriteLine($"  Host: {dbOptions.Value.Host}");
    WriteLine($"  Port: {dbOptions.Value.Port}");
    WriteLine($"  Database: {dbOptions.Value.DatabaseName}");
    WriteLine($"  Timeout: {dbOptions.Value.Timeout}s");

    WriteLine("\nAPI Configuration:");
    WriteLine($"  Base URL: {apiOptions.Value.BaseUrl}");
    WriteLine($"  Timeout: {apiOptions.Value.TimeoutSeconds}s");
    WriteLine($"  Retry Count: {apiOptions.Value.RetryCount}");

    await Task.CompletedTask;
  }

  internal static async Task ConnectToDatabaseAsync(IOptions<DatabaseOptions> dbOptions)
  {
    DatabaseOptions db = dbOptions.Value;
    WriteLine($"Connecting to database...");
    WriteLine($"  Server: {db.Host}:{db.Port}");
    WriteLine($"  Database: {db.DatabaseName}");
    WriteLine($"  Timeout: {db.Timeout}s");
    WriteLine("✓ Connected successfully (simulated)");

    await Task.CompletedTask;
  }

  internal static async Task CallApiAsync(string endpoint, IOptions<ApiOptions> apiOptions)
  {
    ApiOptions api = apiOptions.Value;
    string fullUrl = $"{api.BaseUrl}/{endpoint}";

    WriteLine($"Calling API endpoint...");
    WriteLine($"  URL: {fullUrl}");
    WriteLine($"  Timeout: {api.TimeoutSeconds}s");
    WriteLine($"  Max Retries: {api.RetryCount}");
    WriteLine("✓ API call successful (simulated)");

    await Task.CompletedTask;
  }

  internal static async Task SendNotificationAsync(string message, INotificationService notificationService)
  {
    await notificationService.SendAsync(message);
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION OPTIONS (strongly-typed)
// ═══════════════════════════════════════════════════════════════════════════════

public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
  public string DatabaseName { get; set; } = "myapp";
  public int Timeout { get; set; } = 30;
}

public class ApiOptions
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public int TimeoutSeconds { get; set; } = 30;
  public int RetryCount { get; set; } = 3;
}

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICES
// ═══════════════════════════════════════════════════════════════════════════════

public interface INotificationService
{
  Task SendAsync(string message);
}

public class ConsoleNotificationService : INotificationService
{
  public Task SendAsync(string message)
  {
    WriteLine($"[Console] {message}");
    return Task.CompletedTask;
  }
}

public class EmailNotificationService : INotificationService
{
  public Task SendAsync(string message)
  {
    WriteLine($"[Email] Sending: {message}");
    WriteLine("✓ Email sent successfully (simulated)");
    return Task.CompletedTask;
  }
}
