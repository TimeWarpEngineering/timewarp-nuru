#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION BASICS - STATIC OPTIONS BINDING
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates IOptions<T> parameter injection in handlers:
// - Options are bound from configuration sections at compile time
// - Convention: DatabaseOptions class → "Database" config section (strips "Options" suffix)
// - Override convention with [ConfigurationKey("CustomSection")] attribute
//
// Settings file: configuration-basics.settings.json
//
// Run: dotnet run samples/_configuration/configuration-basics.cs -- config show
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
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
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

internal static class Handlers
{
  /// <summary>
  /// Shows all configuration values.
  /// Demonstrates: IOptions&lt;T&gt; and IConfiguration parameter injection.
  /// </summary>
  internal static async Task ShowConfigurationAsync(
    IOptions<DatabaseOptions> dbOptions,
    IOptions<ApiSettings> apiOptions,
    IConfiguration config)
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

  /// <summary>
  /// Simulates connecting to a database using configured options.
  /// Demonstrates: IOptions&lt;T&gt; parameter injection.
  /// </summary>
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

  /// <summary>
  /// Simulates calling an API endpoint using configured options.
  /// Demonstrates: Route parameter + IOptions&lt;T&gt; parameter injection with [ConfigurationKey] attribute.
  /// </summary>
  internal static async Task CallApiAsync(string endpoint, IOptions<ApiSettings> apiOptions)
  {
    ApiSettings api = apiOptions.Value;
    string fullUrl = $"{api.BaseUrl}/{endpoint}";

    WriteLine($"Calling API endpoint...");
    WriteLine($"  URL: {fullUrl}");
    WriteLine($"  Timeout: {api.TimeoutSeconds}s");
    WriteLine($"  Max Retries: {api.RetryCount}");
    WriteLine("✓ API call successful (simulated)");

    await Task.CompletedTask;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION OPTIONS (strongly-typed)
// ═══════════════════════════════════════════════════════════════════════════════
//
// Convention: Class name ending in "Options" has suffix stripped for section key
// - DatabaseOptions → binds from "Database" section
//
// Attribute override: [ConfigurationKey("SectionName")] specifies the section
// - ApiSettings with [ConfigurationKey("Api")] → binds from "Api" section
// ═══════════════════════════════════════════════════════════════════════════════

public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
  public string DatabaseName { get; set; } = "myapp";
  public int Timeout { get; set; } = 30;
}

// Example of attribute-based override: ApiSettings class maps to "Api" section
[TimeWarp.Nuru.ConfigurationKey("Api")]
public class ApiSettings
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public int TimeoutSeconds { get; set; } = 30;
  public int RetryCount { get; set; } = 3;
}
