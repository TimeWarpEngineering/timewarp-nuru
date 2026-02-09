#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - ADVANCED CONFIGURATION PATTERNS ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates advanced configuration patterns:
// - Nested configuration objects
// - Collection binding
// - Dictionary binding
// - Post-configuration callbacks
//
// DSL: Endpoint with complex IOptions<T> structures
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

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

[NuruRoute("endpoints", Description = "List configured endpoints")]
public sealed class ListEndpointsQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<AppConfiguration> config) : IQueryHandler<ListEndpointsQuery, Unit>
  {
    public ValueTask<Unit> Handle(ListEndpointsQuery query, CancellationToken ct)
    {
      WriteLine("Configured Endpoints:");
      foreach (KeyValuePair<string, EndpointConfig> ep in config.Value.Endpoints)
      {
        WriteLine($"  {ep.Key,-10} -> {ep.Value.Url,-30} (timeout: {ep.Value.Timeout}s)");
      }
      return default;
    }
  }
}

[NuruRoute("features", Description = "List feature flags")]
public sealed class ListFeaturesQuery : IQuery<Unit>
{
  public sealed class Handler(IOptions<AppConfiguration> config) : IQueryHandler<ListFeaturesQuery, Unit>
  {
    public ValueTask<Unit> Handle(ListFeaturesQuery query, CancellationToken ct)
    {
      WriteLine("Feature Flags:");
      foreach (FeatureConfig f in config.Value.Features)
      {
        string status = f.Enabled ? "ENABLED ".Green() : "DISABLED".Red();
        WriteLine($"  {f.Name,-15} {status}");
      }
      return default;
    }
  }
}

// =============================================================================
// COMPLEX CONFIGURATION CLASSES
// =============================================================================

public class AppConfiguration
{
  public ServerConfig Server { get; set; } = new ServerConfig();
  public List<FeatureConfig> Features { get; set; } = [];
  public Dictionary<string, EndpointConfig> Endpoints { get; set; } = new Dictionary<string, EndpointConfig>();
  public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

public class ServerConfig
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 8080;
  public bool UseSsl { get; set; } = false;
}

public class FeatureConfig
{
  public string Name { get; set; } = "";
  public bool Enabled { get; set; } = false;
  public int Weight { get; set; } = 100;
}

public class EndpointConfig
{
  public string Url { get; set; } = "";
  public int Timeout { get; set; } = 30;
  public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}
