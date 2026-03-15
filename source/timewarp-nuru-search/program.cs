using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru.Search.Endpoints;
using TimeWarp.Nuru.Search.Services;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<SearchIndex>();
    services.AddSingleton<CapabilitiesClient>();
  })
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args).ConfigureAwait(false);
