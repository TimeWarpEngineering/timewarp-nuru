// Probe (nuru#242): ConfigureHelp with the per-command and completion rows turned on.
// Paired with help-hide-rows, it shows the options change the output.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .EnableCompletion()
  .ConfigureHelp(options =>
  {
    options.ShowPerCommandHelpRoutes = true;
    options.ShowCompletionRoutes = true;
  })
  .Map("status")
    .WithDescription("Show status")
    .WithHandler(() => "ok")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
