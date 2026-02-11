#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - RUNTIME DI BASICS
// ═══════════════════════════════════════════════════════════════════════════════════════════════
//
// Runtime DI with full Microsoft DI container for complex dependency chains.
//
// DSL: Endpoint with constructor injection and runtime DI
//
// PATTERN:
//   - Use .UseMicrosoftDependencyInjection() for runtime DI
//   - Supports complex service graphs
//   - Opt-in when source-gen DI is insufficient
// ═══════════════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.DependencyInjection

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<IConfigService, ConfigService>();
    services.AddScoped<IDataService, DataService>();
    services.AddTransient<IProcessingService, ProcessingService>();
  })
  .UseMicrosoftDependencyInjection()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
