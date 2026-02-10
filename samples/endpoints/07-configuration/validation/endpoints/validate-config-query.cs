using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("validate", Description = "Validate current configuration")]
public sealed class ValidateConfigQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<ValidatedSettings> settings) : IQueryHandler<ValidateConfigQuery, Unit>
  {
    public ValueTask<Unit> Handle(ValidateConfigQuery query, CancellationToken ct)
    {
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
