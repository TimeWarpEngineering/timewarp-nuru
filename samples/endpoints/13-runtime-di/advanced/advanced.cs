#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - RUNTIME DI ADVANCED ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════════════════════
//
// Advanced runtime DI scenarios: decorators, factories, keyed services.
//
// DSL: Endpoint with advanced DI patterns
//
// PATTERNS:
//   - Decorator pattern with manual decoration
//   - Multiple concrete type injection
//   - Factory-based service creation
//   - Scoped lifetime management
// ═══════════════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.DependencyInjection

using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<ILogger, ConsoleLogger>();

    services.AddScoped<IRepository>(provider =>
      new CachedRepository(
        new DatabaseRepository(),
        provider.GetRequiredService<ILogger>()));

    services.AddSingleton<FastProcessor>();
    services.AddSingleton<ThoroughProcessor>();

    services.AddTransient<Func<string, IAnalyzer>>(provider =>
      name => new DataAnalyzer(name, provider.GetRequiredService<ILogger>()));

    services.AddScoped<ISession, UserSession>();
  })
  .UseMicrosoftDependencyInjection()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
