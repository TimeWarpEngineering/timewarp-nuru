#region Purpose
// Entry point of the search tool: wires logging, the search index and capabilities client, and discovers
// endpoints.
#endregion

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru.Search.Endpoints;
using TimeWarp.Nuru.Search.Services;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    // Warnings and errors go to stderr so stdout stays results-only (kanban 545)
    services.AddLogging(builder => builder
      .SetMinimumLevel(LogLevel.Warning)
      .AddProvider(new StandardErrorLoggerProvider()));
    services.AddSingleton<SearchIndex>();
    services.AddSingleton<CapabilitiesClient>();
  })
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args).ConfigureAwait(false);
