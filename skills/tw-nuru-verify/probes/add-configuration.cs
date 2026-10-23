// Probe: AddConfiguration loads environment variables into an injected IConfiguration.
using Microsoft.Extensions.Configuration;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .AddConfiguration()
  .Map("setting")
    .WithHandler((IConfiguration config) => $"greeting={config["Probe:Greeting"] ?? "unset"}")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
