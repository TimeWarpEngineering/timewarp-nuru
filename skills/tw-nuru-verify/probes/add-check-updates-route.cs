// Probe: AddCheckUpdatesRoute (source/timewarp-nuru/nuru-app-builder-extensions.updates.cs,
// NuruAppBuilderExtensions) is a public builder extension. A consumer must be able to call it and
// still run their own routes. Pins the CS0122 the package gave on 4.0.0-beta.5.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()
  .AddCheckUpdatesRoute()
  .Build();

return await app.RunAsync(args);
