// Probe: EnableCompletion adds --generate-completion and the __complete callback.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .EnableCompletion()
  .Map("deploy {env}")
    .WithHandler((string env) => $"deploy {env}")
    .AsCommand()
    .Done()
  .Map("status")
    .WithHandler(() => "ok")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
