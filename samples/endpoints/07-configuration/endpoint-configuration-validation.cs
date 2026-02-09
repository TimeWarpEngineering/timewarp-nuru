#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:package System.ComponentModel.DataAnnotations
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CONFIGURATION VALIDATION ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates fail-fast configuration validation using DataAnnotations
// and custom validation with Endpoint DSL.
//
// DSL: Endpoint with validated IOptions<T>
//
// Validators:
//   - DataAnnotations (Required, Range, StringLength, etc.)
//   - Custom validation attributes
//   - ValidateOnStart() for fail-fast behavior
// ═══════════════════════════════════════════════════════════════════════════════

using System.ComponentModel.DataAnnotations;
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

[NuruRoute("validate", Description = "Validate current configuration")]
public sealed class ValidateConfigQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<ValidatedSettings> settings) : IQueryHandler<ValidateConfigQuery, Unit>
  {
    public ValueTask<Unit> Handle(ValidateConfigQuery query, CancellationToken ct)
    {
      // If we get here, validation passed
      ValidatedSettings s = settings.Value;

      WriteLine("✓ Configuration validation passed!");
      WriteLine();
      WriteLine("Validated Settings:");
      WriteLine($"  ApiKey: {MaskApiKey(s.ApiKey)}");
      WriteLine($"  Timeout: {s.TimeoutMs}ms (Range: 100-60000)");
      WriteLine($"  Retries: {s.MaxRetries} (Range: 0-10)");
      WriteLine($"  Endpoint: {s.EndpointUrl}");
      WriteLine($"  Tags: {string.Join(", ", s.Tags)}");

      return default;
    }

    private static string MaskApiKey(string apiKey)
    {
      if (apiKey.Length <= 8) return "***";
      return apiKey[..4] + "..." + apiKey[^4..];
    }
  }
}

[NuruRoute("settings", Description = "Show all current settings")]
public sealed class ShowSettingsQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<ValidatedSettings> settings, IConfiguration config) : IQueryHandler<ShowSettingsQuery, Unit>
  {
    public ValueTask<Unit> Handle(ShowSettingsQuery query, CancellationToken ct)
    {
      ValidatedSettings s = settings.Value;

      WriteLine("Current Settings:");
      WriteLine($"  ApiKey: {s.ApiKey}");
      WriteLine($"  TimeoutMs: {s.TimeoutMs}");
      WriteLine($"  MaxRetries: {s.MaxRetries}");
      WriteLine($"  EndpointUrl: {s.EndpointUrl}");
      WriteLine($"  Environment: {s.Environment}");
      WriteLine($"  Tags: [{string.Join(", ", s.Tags)}]");

      return default;
    }
  }
}

[NuruRoute("test-connection", Description = "Test connection using validated settings")]
public sealed class TestConnectionCommand : ICommand<Unit>
{
  public sealed class Handler(IOptions<ValidatedSettings> settings) : ICommandHandler<TestConnectionCommand, Unit>
  {
    public async ValueTask<Unit> Handle(TestConnectionCommand command, CancellationToken ct)
    {
      ValidatedSettings s = settings.Value;

      WriteLine($"Connecting to {s.EndpointUrl}...");
      WriteLine($"  Timeout: {s.TimeoutMs}ms");
      WriteLine($"  Retries: {s.MaxRetries}");

      await Task.Delay(100, ct); // Simulate connection

      WriteLine("✓ Connection successful!");
      return default;
    }
  }
}

// =============================================================================
// VALIDATED SETTINGS with DataAnnotations
// =============================================================================

public class ValidatedSettings
{
  [Required(ErrorMessage = "ApiKey is required")]
  [StringLength(100, MinimumLength = 10, ErrorMessage = "ApiKey must be 10-100 characters")]
  public string ApiKey { get; set; } = "default-key-12345";

  [Range(100, 60000, ErrorMessage = "Timeout must be between 100ms and 60000ms")]
  public int TimeoutMs { get; set; } = 5000;

  [Range(0, 10, ErrorMessage = "MaxRetries must be between 0 and 10")]
  public int MaxRetries { get; set; } = 3;

  [Required]
  [Url(ErrorMessage = "EndpointUrl must be a valid URL")]
  public string EndpointUrl { get; set; } = "https://api.example.com";

  [Required]
  [AllowedValues("Development", "Staging", "Production")]
  public string Environment { get; set; } = "Development";

  [MaxLength(5, ErrorMessage = "Maximum 5 tags allowed")]
  public string[] Tags { get; set; } = ["cli", "api"];
}
