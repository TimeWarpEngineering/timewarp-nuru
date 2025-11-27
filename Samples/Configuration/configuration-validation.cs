#!/usr/bin/dotnet --
// configuration-validation - Demonstrates ValidateOnStart() for fail-fast configuration validation
// Settings file: configuration-validation.settings.json
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:package Microsoft.Extensions.Options.DataAnnotations
#:package TimeWarp.OptionsValidation
#:property EnableConfigurationBindingGenerator=true

using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using TimeWarp.OptionsValidation;
using static System.Console;

// This sample demonstrates three validation approaches:
// 1. DataAnnotations (built-in .NET attributes)
// 2. Custom validation with .Validate()
// 3. FluentValidation (enterprise-grade validation library)

NuruApp app =
  new NuruAppBuilder()
  .AddDependencyInjection()
  .AddConfiguration(args)
  .ConfigureServices((services, config) =>
  {
    if (config != null)
    {
      // 1. DataAnnotations validation (built-in)
      services.AddOptions<ServerOptions>()
        .Bind(config.GetSection("Server"))
        .ValidateDataAnnotations()  // Validates [Required], [Range], etc.
        .ValidateOnStart();          // ← Validates during Build(), not on first access

      // 2. Custom validation logic
      services.AddOptions<DatabaseOptions>()
        .Bind(config.GetSection("Database"))
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

      // 3. FluentValidation (most powerful, enterprise-grade)
      // Using TimeWarp.OptionsValidation for automatic FluentValidation integration
      services
        .AddFluentValidatedOptions<ApiOptions, ApiOptionsValidator>(config)
        .ValidateOnStart(); // ✅ Validates when app starts, throws on error
    }
  })
  .AddAutoHelp()
  .Map("validate", ShowValidationStatusAsync, "Show all validated configuration")
  .Map("server info", ShowServerInfoAsync, "Show server configuration")
  .Map("db info", ShowDatabaseInfoAsync, "Show database configuration")
  .Map("api info", ShowApiInfoAsync, "Show API configuration")
  .Build();
// ↑ If any validation fails, OptionsValidationException is thrown HERE (fail-fast)

return await app.RunAsync(args);

// Route handlers

async Task ShowValidationStatusAsync()
{
  WriteLine("\n✓ All configuration validated successfully at startup!");
  WriteLine("\nThis demonstrates fail-fast behavior:");
  WriteLine("  • Invalid configuration would have thrown OptionsValidationException during Build()");
  WriteLine("  • No need to wait until first access to discover configuration errors");
  WriteLine("  • Matches ASP.NET Core and Hosted Services behavior");
  await Task.CompletedTask;
}

async Task ShowServerInfoAsync(IOptions<ServerOptions> serverOptions)
{
  ServerOptions server = serverOptions.Value;
  WriteLine("\n=== Server Configuration (DataAnnotations Validation) ===");
  WriteLine($"Host: {server.Host}");
  WriteLine($"Port: {server.Port}");
  WriteLine($"Max Connections: {server.MaxConnections}");
  WriteLine($"Timeout: {server.Timeout}s");
  await Task.CompletedTask;
}

async Task ShowDatabaseInfoAsync(IOptions<DatabaseOptions> dbOptions)
{
  DatabaseOptions db = dbOptions.Value;
  WriteLine("\n=== Database Configuration (Custom Validation) ===");
  WriteLine($"Type: {db.Type}");
  WriteLine($"Connection String: {db.ConnectionString}");
  WriteLine("✓ Connection string format matches database type");
  await Task.CompletedTask;
}

async Task ShowApiInfoAsync(IOptions<ApiOptions> apiOptions)
{
  ApiOptions api = apiOptions.Value;
  WriteLine("\n=== API Configuration (FluentValidation) ===");
  WriteLine($"Base URL: {api.BaseUrl}");
  WriteLine($"API Key: {new string('*', api.ApiKey.Length)} (masked)");
  WriteLine($"Timeout: {api.TimeoutSeconds}s");
  WriteLine($"Retry Count: {api.RetryCount}");
  WriteLine($"Rate Limit: {api.RateLimitPerMinute} requests/minute");
  await Task.CompletedTask;
}

// Configuration option classes

public class ServerOptions
{
  [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Host is required")]
  public string Host { get; set; } = "localhost";

  [System.ComponentModel.DataAnnotations.Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
  public int Port { get; set; } = 8080;

  [System.ComponentModel.DataAnnotations.Range(1, 10000, ErrorMessage = "MaxConnections must be between 1 and 10000")]
  public int MaxConnections { get; set; } = 100;

  [System.ComponentModel.DataAnnotations.Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
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

// FluentValidation validator

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
