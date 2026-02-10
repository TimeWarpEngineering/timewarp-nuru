#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// FLUENT DSL - CONFIGURATION VALIDATION
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates configuration validation using Fluent DSL
// with Microsoft.Extensions.Options.IValidateOptions<T>.
//
// DSL: Fluent API with IValidateOptions<T> validators
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
//
// Examples:
//   dotnet run fluent-configuration-validation.cs -- validate
//   dotnet run fluent-configuration-validation.cs -- server info
//   dotnet run fluent-configuration-validation.cs -- api info
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
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

  internal static void ShowApiInfo(IOptions<ApiOptions> apiOptions)
  {
    ApiOptions api = apiOptions.Value;
    WriteLine("\n=== API Configuration ===");
    WriteLine($"Base URL: {api.BaseUrl}");
    WriteLine($"API Key: {new string('*', Math.Min(api.ApiKey.Length, 8))}... (masked)");
    WriteLine($"Timeout: {api.TimeoutSeconds}s");
    WriteLine($"Retry Count: {api.RetryCount}");
    WriteLine("\n✓ Validated by ApiOptionsValidator");
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION OPTIONS
// ═══════════════════════════════════════════════════════════════════════════════

[TimeWarp.Nuru.ConfigurationKey("Server")]
public class ServerOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 8080;
  public int MaxConnections { get; set; } = 100;
  public int Timeout { get; set; } = 30;
}

[TimeWarp.Nuru.ConfigurationKey("Api")]
public class ApiOptions
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public string ApiKey { get; set; } = "default-api-key-for-development-only-32chars";
  public int TimeoutSeconds { get; set; } = 30;
  public int RetryCount { get; set; } = 3;
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALIDATORS - Implement IValidateOptions<T>
// ═══════════════════════════════════════════════════════════════════════════════

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

    return failures.Count > 0
      ? ValidateOptionsResult.Fail(failures)
      : ValidateOptionsResult.Success;
  }
}
