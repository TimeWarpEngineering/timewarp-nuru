using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

[NuruRoute("show", Description = "Show all application configuration")]
public sealed class ShowConfigQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<AppConfiguration> config) : IQueryHandler<ShowConfigQuery, Unit>
  {
    public ValueTask<Unit> Handle(ShowConfigQuery query, CancellationToken ct)
    {
      AppConfiguration app = config.Value;

      WriteLine("=== Application Configuration ===\n");

      WriteLine("Server:");
      WriteLine($"  Host: {app.Server.Host}");
      WriteLine($"  Port: {app.Server.Port}");
      WriteLine($"  Ssl: {app.Server.UseSsl}");

      WriteLine("\nFeatures:");
      foreach (FeatureConfig feature in app.Features)
      {
        WriteLine($"  {feature.Name}: {(feature.Enabled ? "✓" : "✗")} (Weight: {feature.Weight})");
      }

      WriteLine("\nEndpoints:");
      foreach (KeyValuePair<string, EndpointConfig> ep in app.Endpoints)
      {
        WriteLine($"  {ep.Key}: {ep.Value.Url} (Timeout: {ep.Value.Timeout}s)");
      }

      WriteLine("\nMetadata:");
      foreach (KeyValuePair<string, string> meta in app.Metadata)
      {
        WriteLine($"  {meta.Key}: {meta.Value}");
      }

      return default;
    }
  }
}
