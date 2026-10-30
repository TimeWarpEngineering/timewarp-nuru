#region Purpose
// Minimal consumer dev CLI for the DevCli consumer check (kanban 562). It wires the
// shipped DevCli endpoints and services the way tools/dev-cli/dev.cs does, so the
// build compiles that content as a net11 consumer would. It is built, not run.
#endregion

NuruApp app = NuruApp.CreateBuilder()
  .WithName("devcli-consumer-check")
  .ConfigureServices(services =>
  {
    services.AddSingleton<IRepoCleanService, RepoCleanService>();
    services.AddSingleton<NuGetVersionService>();
    services.AddSingleton<IRepoConfigService, RepoConfigService>();
    services.AddSingleton<IPackableProjectService, PackableProjectService>();
  })
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
