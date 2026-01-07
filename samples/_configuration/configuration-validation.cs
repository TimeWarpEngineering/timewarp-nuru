#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION VALIDATION - FAIL-FAST WITH IVALIDATEOPTIONS<T>
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with configuration validation
// using Microsoft.Extensions.Options.IValidateOptions<T>.
//
// HOW IT WORKS:
//   1. Define options classes (e.g., ServerOptions, ApiOptions)
//   2. Create validator classes implementing IValidateOptions<T>
//   3. The generator automatically detects validators and runs them at startup
//   4. If validation fails, OptionsValidationException is thrown during Build()
//
// AOT COMPATIBILITY:
//   ✅ Uses IValidateOptions<T> interface (no reflection)
//   ✅ All validation runs at startup (fail-fast)
//   ✅ No external dependencies beyond Microsoft.Extensions.Options
//
// Settings file: configuration-validation.settings.json
//
// Examples:
//   dotnet run samples/_configuration/configuration-validation.cs -- validate
//   dotnet run samples/_configuration/configuration-validation.cs -- server info
//   dotnet run samples/_configuration/configuration-validation.cs -- db info
//   dotnet run samples/_configuration/configuration-validation.cs -- api info
//
// To see validation failure:
//   dotnet run samples/_configuration/configuration-validation.cs -- server info --Server:Port=0
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .Map("validate")
    .WithHandler(Handlers.ShowValidationStatus)
    .WithDescription("Show all validated configuration")
    .AsQuery()
    .Done()
  .Map("server info")
    .WithHandler(Handlers.ShowServerInfo)
    .WithDescription("Show server configuration")
    .AsQuery()
    .Done()
  .Map("db info")
    .WithHandler(Handlers.ShowDatabaseInfo)
    .WithDescription("Show database configuration")
    .AsQuery()
    .Done()
  .Map("api info")
    .WithHandler(Handlers.ShowApiInfo)
    .WithDescription("Show API configuration")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

internal static class Handlers
{
  internal static void ShowValidationStatus()
  {
    WriteLine("\n✓ All configuration validated successfully at startup!");
    WriteLine("\nThis demonstrates fail-fast behavior:");
    WriteLine("  • Invalid configuration throws OptionsValidationException during Build()");
    WriteLine("  • No need to wait until first access to discover configuration errors");
    WriteLine("  • Validators are auto-detected by implementing IValidateOptions<T>");
  }

  internal static void ShowServerInfo(IOptions<ServerOptions> serverOptions)
  {
    ServerOptions server = serverOptions.Value;
    WriteLine("\n=== Server Configuration ===");
    WriteLine($"Host: {server.Host}");
    WriteLine($"Port: {server.Port}");
    WriteLine($"Max Connections: {server.MaxConnections}");
    WriteLine($"Timeout: {server.Timeout}s");
    WriteLine("\n✓ Validated by ServerOptionsValidator");
  }

  internal static void ShowDatabaseInfo(IOptions<DatabaseOptions> dbOptions)
  {
    DatabaseOptions db = dbOptions.Value;
    WriteLine("\n=== Database Configuration ===");
    WriteLine($"Type: {db.Type}");
    WriteLine($"Connection String: {db.ConnectionString}");
    WriteLine("\n✓ Validated by DatabaseOptionsValidator");
  }

  internal static void ShowApiInfo(IOptions<ApiOptions> apiOptions)
  {
    ApiOptions api = apiOptions.Value;
    WriteLine("\n=== API Configuration ===");
    WriteLine($"Base URL: {api.BaseUrl}");
    WriteLine($"API Key: {new string('*', Math.Min(api.ApiKey.Length, 8))}... (masked)");
    WriteLine($"Timeout: {api.TimeoutSeconds}s");
    WriteLine($"Retry Count: {api.RetryCount}");
    WriteLine($"Rate Limit: {api.RateLimitPerMinute} requests/minute");
    WriteLine("\n✓ Validated by ApiOptionsValidator");
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION OPTIONS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Server configuration options.
/// Section key: "Server" (via [ConfigurationKey] attribute)
/// </summary>
[TimeWarp.Nuru.ConfigurationKey("Server")]
public class ServerOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 8080;
  public int MaxConnections { get; set; } = 100;
  public int Timeout { get; set; } = 30;
}

/// <summary>
/// Database configuration options.
/// Section key: "Database" (by convention, strips "Options" suffix)
/// </summary>
public class DatabaseOptions
{
  public string Type { get; set; } = "PostgreSQL";
  public string ConnectionString { get; set; } = "Host=localhost;Database=myapp";
}

/// <summary>
/// API configuration options.
/// Section key: "Api" (via [ConfigurationKey] attribute)
/// </summary>
[TimeWarp.Nuru.ConfigurationKey("Api")]
public class ApiOptions
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public string ApiKey { get; set; } = "default-api-key-for-development-only-32chars";
  public int TimeoutSeconds { get; set; } = 30;
  public int RetryCount { get; set; } = 3;
  public int RateLimitPerMinute { get; set; } = 60;
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALIDATORS - Implement IValidateOptions<T>
// ═══════════════════════════════════════════════════════════════════════════════
//
// The generator automatically detects these validators and runs validation
// when IOptions<T> parameters are bound from configuration.

/// <summary>
/// Validates ServerOptions configuration.
/// </summary>
public class ServerOptionsValidator : IValidateOptions<ServerOptions>
{
  public ValidateOptionsResult Validate(string? name, ServerOptions options)
  {
    List<string> failures = [];

    if (string.IsNullOrWhiteSpace(options.Host))
      failures.Add("Host is required");

    if (options.Port < 1 || options.Port > 65535)
      failures.Add("Port must be between 1 and 65535");

    if (options.MaxConnections < 1 || options.MaxConnections > 10000)
      failures.Add("MaxConnections must be between 1 and 10000");

    if (options.Timeout < 1 || options.Timeout > 300)
      failures.Add("Timeout must be between 1 and 300 seconds");

    return failures.Count > 0
      ? ValidateOptionsResult.Fail(failures)
      : ValidateOptionsResult.Success;
  }
}

/// <summary>
/// Validates DatabaseOptions configuration.
/// </summary>
public class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
  public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
  {
    List<string> failures = [];

    if (string.IsNullOrWhiteSpace(options.Type))
      failures.Add("Database type is required");

    if (string.IsNullOrWhiteSpace(options.ConnectionString))
      failures.Add("Connection string is required");

    // Custom business rule: connection string must match database type
    if (options.Type == "PostgreSQL" && !options.ConnectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
      failures.Add("PostgreSQL connection string must contain 'Host='");

    if (options.Type == "SqlServer" && !options.ConnectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
      failures.Add("SqlServer connection string must contain 'Server='");

    return failures.Count > 0
      ? ValidateOptionsResult.Fail(failures)
      : ValidateOptionsResult.Success;
  }
}

/// <summary>
/// Validates ApiOptions configuration.
/// </summary>
public class ApiOptionsValidator : IValidateOptions<ApiOptions>
{
  public ValidateOptionsResult Validate(string? name, ApiOptions options)
  {
    List<string> failures = [];

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
      failures.Add("Base URL is required");
    else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps)
      failures.Add("Base URL must be a valid HTTPS URL");

    if (string.IsNullOrWhiteSpace(options.ApiKey))
      failures.Add("API Key is required");
    else if (options.ApiKey.Length < 32)
      failures.Add("API Key must be at least 32 characters for security");

    if (options.TimeoutSeconds < 1 || options.TimeoutSeconds > 300)
      failures.Add("Timeout must be between 1 and 300 seconds");

    if (options.RetryCount < 0 || options.RetryCount > 10)
      failures.Add("Retry count must be between 0 and 10");

    if (options.RateLimitPerMinute < 1 || options.RateLimitPerMinute > 1000)
      failures.Add("Rate limit must be between 1 and 1000 requests/minute");

    return failures.Count > 0
      ? ValidateOptionsResult.Fail(failures)
      : ValidateOptionsResult.Success;
  }
}
