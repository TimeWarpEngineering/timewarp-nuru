#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CONFIGURATION BASICS ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates IOptions<T> with Endpoint DSL:
// - Options are bound from configuration sections at compile time
// - Convention: DatabaseOptions class → "Database" config section
// - Override convention with [ConfigurationKey("CustomSection")] attribute
//
// DSL: Endpoint with IOptions<T> constructor injection
//
// Settings file: endpoint-configuration-basics.settings.json
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("config-show", Description = "Show current configuration values")]
public sealed class ConfigShowQuery : IQuery<Unit>
{
  public sealed class Handler(
    IOptions<DatabaseOptions> dbOptions,
    IOptions<ApiSettings> apiOptions,
    IConfiguration config) : IQueryHandler<ConfigShowQuery, Unit>
  {
    public ValueTask<Unit> Handle(ConfigShowQuery query, CancellationToken ct)
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

      return default;
    }
  }
}

[NuruRoute("db-connect", Description = "Connect to database using config")]
public sealed class DbConnectCommand : ICommand<Unit>
{
  public sealed class Handler(IOptions<DatabaseOptions> dbOptions) : ICommandHandler<DbConnectCommand, Unit>
  {
    public ValueTask<Unit> Handle(DbConnectCommand command, CancellationToken ct)
    {
      DatabaseOptions db = dbOptions.Value;
      WriteLine("Connecting to database...");
      WriteLine($"  Server: {db.Host}:{db.Port}");
      WriteLine($"  Database: {db.DatabaseName}");
      WriteLine($"  Timeout: {db.Timeout}s");
      WriteLine("✓ Connected successfully (simulated)");
      return default;
    }
  }
}

[NuruRoute("api-call", Description = "Call API endpoint using config")]
public sealed class ApiCallQuery : IQuery<Unit>
{
  [Parameter(Description = "API endpoint to call")]
  public string Endpoint { get; set; } = "";

  public sealed class Handler(IOptions<ApiSettings> apiOptions) : IQueryHandler<ApiCallQuery, Unit>
  {
    public ValueTask<Unit> Handle(ApiCallQuery query, CancellationToken ct)
    {
      ApiSettings api = apiOptions.Value;
      string fullUrl = $"{api.BaseUrl}/{query.Endpoint}";

      WriteLine("Calling API endpoint...");
      WriteLine($"  URL: {fullUrl}");
      WriteLine($"  Timeout: {api.TimeoutSeconds}s");
      WriteLine($"  Max Retries: {api.RetryCount}");
      WriteLine("✓ API call successful (simulated)");
      return default;
    }
  }
}

// =============================================================================
// CONFIGURATION OPTIONS (strongly-typed)
// =============================================================================

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
