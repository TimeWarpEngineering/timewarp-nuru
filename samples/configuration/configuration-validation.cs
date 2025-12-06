#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:package TimeWarp.OptionsValidation
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION VALIDATION - FAIL-FAST WITH VALIDATEONSTART (AOT-COMPATIBLE)
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with configuration validation:
// - FluentValidation (enterprise-grade, AOT-compatible)
// - Custom validation with .Validate() (AOT-compatible)
//
// AOT COMPATIBILITY:
//   ✅ Uses manual property binding in Action<TOptions> overloads (no reflection)
//   ✅ Uses FluentValidation instead of DataAnnotations (no reflection)
//   ✅ All validation runs at compile-time or startup
//   ❌ Avoid: .Bind(), .ValidateDataAnnotations(), IConfiguration overloads - these use reflection
//
// Settings file: configuration-validation.settings.json
//
// REQUIRED PACKAGES:
//   #:package Mediator.Abstractions    - Required by NuruApp.CreateBuilder
//   #:package Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
// ═══════════════════════════════════════════════════════════════════════════════

using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using TimeWarp.OptionsValidation;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  .Map("validate", ShowValidationStatusAsync, "Show all validated configuration")
  .Map("server info", ShowServerInfoAsync, "Show server configuration")
  .Map("db info", ShowDatabaseInfoAsync, "Show database configuration")
  .Map("api info", ShowApiInfoAsync, "Show API configuration")
  .Build();
// ↑ If any validation fails, OptionsValidationException is thrown HERE (fail-fast)

static void ConfigureServices(IServiceCollection services)
{
  // Get configuration from the service provider
  ServiceProvider sp = services.BuildServiceProvider();
  IConfiguration? config = sp.GetService<IConfiguration>();

  if (config != null)
  {
    // 1. FluentValidation with AOT-compatible manual binding
    // ✅ The Action<TOptions> overload avoids IConfiguration.Bind() reflection
    // ✅ Manual property assignment is fully AOT-compatible
    // ✅ FluentValidation performs validation without reflection
    IConfigurationSection serverSection = config.GetSection("Server");
    services
      .AddFluentValidatedOptions<ServerOptions, ServerOptionsValidator>(options =>
      {
        // Manual binding is fully AOT-compatible (no reflection)
        options.Host = serverSection["Host"] ?? options.Host;
        options.Port = int.TryParse(serverSection["Port"], out int port) ? port : options.Port;
        options.MaxConnections = int.TryParse(serverSection["MaxConnections"], out int max) ? max : options.MaxConnections;
        options.Timeout = int.TryParse(serverSection["Timeout"], out int timeout) ? timeout : options.Timeout;
      })
      .ValidateOnStart(); // ← Validates during Build(), not on first access

    // 2. Custom validation logic with AOT-compatible manual binding
    // ✅ Manual binding avoids reflection-based Configure(section)
    IConfigurationSection dbSection = config.GetSection("Database");
    services.AddOptions<DatabaseOptions>()
      .Configure(options =>
      {
        // Manual binding is fully AOT-compatible (no reflection)
        options.Type = dbSection["Type"] ?? options.Type;
        options.ConnectionString = dbSection["ConnectionString"] ?? options.ConnectionString;
      })
      .Validate(opts =>
      {
        // Custom business rule: connection string must match database type
        if (opts.Type == "PostgreSQL" && !opts.ConnectionString.Contains("Host="))
        {
          return false;
        }
        if (opts.Type == "SqlServer" && !opts.ConnectionString.Contains("Server="))
        {
          return false;
        }
        return true;
      }, "Connection string format must match database type")
      .ValidateOnStart();

    // 3. FluentValidation with AOT-compatible manual binding (enterprise-grade)
    // ✅ The Action<TOptions> overload is fully AOT-compatible
    // ✅ Using TimeWarp.OptionsValidation for automatic FluentValidation integration
    IConfigurationSection apiSection = config.GetSection("Api");
    services
      .AddFluentValidatedOptions<ApiOptions, ApiOptionsValidator>(options =>
      {
        // Manual binding is fully AOT-compatible (no reflection)
        options.BaseUrl = apiSection["BaseUrl"] ?? options.BaseUrl;
        options.ApiKey = apiSection["ApiKey"] ?? options.ApiKey;
        options.TimeoutSeconds = int.TryParse(apiSection["TimeoutSeconds"], out int timeout) ? timeout : options.TimeoutSeconds;
        options.RetryCount = int.TryParse(apiSection["RetryCount"], out int retry) ? retry : options.RetryCount;
        options.RateLimitPerMinute = int.TryParse(apiSection["RateLimitPerMinute"], out int rate) ? rate : options.RateLimitPerMinute;
      })
      .ValidateOnStart(); // ✅ Validates when app starts, throws on error
  }

  // Register Mediator - required by NuruApp.CreateBuilder
  services.AddMediator();
}
// ↑ If any validation fails, OptionsValidationException is thrown HERE (fail-fast)

return await app.RunAsync(args);

// Route handlers

void ShowValidationStatusAsync()
{
  WriteLine("\n✓ All configuration validated successfully at startup!");
  WriteLine("\nThis demonstrates fail-fast behavior:");
  WriteLine("  • Invalid configuration would have thrown OptionsValidationException during Build()");
  WriteLine("  • No need to wait until first access to discover configuration errors");
  WriteLine("  • Matches ASP.NET Core and Hosted Services behavior");
}

void ShowServerInfoAsync(IOptions<ServerOptions> serverOptions)
{
  ServerOptions server = serverOptions.Value;
  WriteLine("\n=== Server Configuration (FluentValidation) ===");
  WriteLine($"Host: {server.Host}");
  WriteLine($"Port: {server.Port}");
  WriteLine($"Max Connections: {server.MaxConnections}");
  WriteLine($"Timeout: {server.Timeout}s");
}

void ShowDatabaseInfoAsync(IOptions<DatabaseOptions> dbOptions)
{
  DatabaseOptions db = dbOptions.Value;
  WriteLine("\n=== Database Configuration (Custom Validation) ===");
  WriteLine($"Type: {db.Type}");
  WriteLine($"Connection String: {db.ConnectionString}");
  WriteLine("✓ Connection string format matches database type");
}

void ShowApiInfoAsync(IOptions<ApiOptions> apiOptions)
{
  ApiOptions api = apiOptions.Value;
  WriteLine("\n=== API Configuration (FluentValidation) ===");
  WriteLine($"Base URL: {api.BaseUrl}");
  WriteLine($"API Key: {new string('*', api.ApiKey.Length)} (masked)");
  WriteLine($"Timeout: {api.TimeoutSeconds}s");
  WriteLine($"Retry Count: {api.RetryCount}");
  WriteLine($"Rate Limit: {api.RateLimitPerMinute} requests/minute");
}

// Configuration option classes

[ConfigurationKey("Server")]
public class ServerOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 8080;
  public int MaxConnections { get; set; } = 100;
  public int Timeout { get; set; } = 30;
}

public class DatabaseOptions
{
  public string Type { get; set; } = "PostgreSQL";
  public string ConnectionString { get; set; } = "Host=localhost;Database=myapp";
}

[ConfigurationKey("Api")]
public class ApiOptions
{
  public string BaseUrl { get; set; } = "https://api.example.com";
  public string ApiKey { get; set; } = "";
  public int TimeoutSeconds { get; set; } = 30;
  public int RetryCount { get; set; } = 3;
  public int RateLimitPerMinute { get; set; } = 60;
}

// FluentValidation validators

public class ServerOptionsValidator : AbstractValidator<ServerOptions>
{
  public ServerOptionsValidator()
  {
    RuleFor(x => x.Host)
      .NotEmpty().WithMessage("Host is required");

    RuleFor(x => x.Port)
      .InclusiveBetween(1, 65535).WithMessage("Port must be between 1 and 65535");

    RuleFor(x => x.MaxConnections)
      .InclusiveBetween(1, 10000).WithMessage("MaxConnections must be between 1 and 10000");

    RuleFor(x => x.Timeout)
      .InclusiveBetween(1, 300).WithMessage("Timeout must be between 1 and 300 seconds");
  }
}

public class ApiOptionsValidator : AbstractValidator<ApiOptions>
{
  public ApiOptionsValidator()
  {
    RuleFor(x => x.BaseUrl)
      .NotEmpty().WithMessage("Base URL is required")
      .Must(BeValidUrl).WithMessage("Base URL must be a valid HTTPS URL");

    RuleFor(x => x.ApiKey)
      .NotEmpty().WithMessage("API Key is required")
      .MinimumLength(32).WithMessage("API Key must be at least 32 characters for security");

    RuleFor(x => x.TimeoutSeconds)
      .InclusiveBetween(1, 300).WithMessage("Timeout must be between 1 and 300 seconds");

    RuleFor(x => x.RetryCount)
      .InclusiveBetween(0, 10).WithMessage("Retry count must be between 0 and 10");

    RuleFor(x => x.RateLimitPerMinute)
      .GreaterThan(0).WithMessage("Rate limit must be greater than 0")
      .LessThanOrEqualTo(1000).WithMessage("Rate limit cannot exceed 1000 requests/minute");
  }

  private static bool BeValidUrl(string url)
  {
    return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
      && uri.Scheme == Uri.UriSchemeHttps;
  }
}
