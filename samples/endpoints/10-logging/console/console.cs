#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CONSOLE LOGGING ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// Console logging integration with Microsoft.Extensions.Logging using Endpoint DSL.
//
// DSL: Endpoint with ILogger<T> constructor injection
//
// PATTERN:
//   - Add logging via ConfigureServices()
//   - Inject ILogger<T> into handlers
//   - Log at different levels (Debug, Info, Warning, Error)
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddLogging(builder => builder.AddConsole());
  })
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
