#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// FLUENT DSL - CONFIGURATION BASICS
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates IOptions<T> parameter injection with Fluent DSL:
// - Options are bound from configuration sections at compile time
// - Convention: DatabaseOptions class → "Database" config section (strips "Options" suffix)
// - Override convention with [ConfigurationKey("CustomSection")] attribute
//
// DSL: Fluent API (Map().WithHandler().AsQuery().Done())
//
// Settings file: fluent-configuration-basics.settings.json
//
// Run: dotnet run samples/fluent/07-configuration/fluent-configuration-basics.cs -- config show
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
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

  internal static async Task ConnectToDatabaseAsync(IOptions<DatabaseOptions> dbOptions)
  {
    DatabaseOptions db = dbOptions.Value;
    WriteLine("Connecting to database...");
    WriteLine($"  Server: {db.Host}:{db.Port}");
    WriteLine($"  Database: {db.DatabaseName}");
    WriteLine($"  Timeout: {db.Timeout}s");
    WriteLine("✓ Connected successfully (simulated)");

    await Task.CompletedTask;
  }

  internal static async Task CallApiAsync(string endpoint, IOptions<ApiSettings> apiOptions)
  {
    ApiSettings api = apiOptions.Value;
    string fullUrl = $"{api.BaseUrl}/{endpoint}";

    WriteLine("Calling API endpoint...");
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

public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
  public string DatabaseName { get; set; } = "myapp";
  public int Timeout { get; set; } = 30;
}

[TimeWarp.Nuru.ConfigurationKey("Api")]
public class ApiSettings
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public int TimeoutSeconds { get; set; } = 30;
  public int RetryCount { get; set; } = 3;
}
