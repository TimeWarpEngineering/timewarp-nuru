// Probe (nuru#242): ConfigureHelp with the per-command and completion rows turned off.
// When ConfigureHelp is a stub, --help still lists "status --help" and "__complete".
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .EnableCompletion()
  .ConfigureHelp(options =>
  {
    options.ShowPerCommandHelpRoutes = false;
    options.ShowCompletionRoutes = false;
  })
  .Map("status")
    .WithDescription("Show status")
    .WithHandler(() => "ok")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
