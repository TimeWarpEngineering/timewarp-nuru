// Probe (nuru#242): ConfigureHelp ExcludePatterns, assigned as HelpOptions (NuruAppBuilder.ConfigureHelp in nuru-app-builder.cs) declares it.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureHelp(options => options.ExcludePatterns = ["secret-*"])
  .Map("visible")
    .WithDescription("Listed in help")
    .WithHandler(() => "visible")
    .AsQuery()
    .Done()
  .Map("secret-debug")
    .WithDescription("Hidden from help")
    .WithHandler(() => "secret")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
