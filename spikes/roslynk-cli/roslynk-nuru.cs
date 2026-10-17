#!/usr/bin/env -S dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#region Purpose
// Entry point for the hand-written Roslynk-on-Nuru spike.
#endregion

#region Design
// Runtime Microsoft DI, because AddRoslynk is a hand-written registration extension.
// The generator does not walk if statements, so this entry has one top-level RunAsync.
// timing/warm-pair.cs is the same host with two top-level RunAsync calls.
#endregion

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .UseMicrosoftDependencyInjection()
  .ConfigureServices(services =>
  {
    // Fully qualified: the generator pastes this block into its own file, without these usings.
    global::Morris.Roslynk.ServicesRegistration.AddRoslynk(services);
    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
      .AddSingleton<global::Morris.Roslynk.Features.Diagnostics.GetDiagnostics.GetDiagnosticsTool>(services);
    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions
      .AddSingleton<global::Morris.Roslynk.Features.References.FindReferences.FindReferencesTool>(services);
  })
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
