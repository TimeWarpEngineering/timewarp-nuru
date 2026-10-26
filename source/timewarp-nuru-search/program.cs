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
