// Probe: WithGroupPrefix, including a nested group (builder-api.md "Route Groups").
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .WithGroupPrefix("admin")
    .Map("status")
      .WithHandler(() => "admin status ok")
      .AsQuery()
      .Done()
    .WithGroupPrefix("config")
      .Map("get {key}")
        .WithHandler((string key) => $"value: {key}")
        .AsQuery()
        .Done()
      .Done()
    .Done()
  .Build();

return await app.RunAsync(args);
