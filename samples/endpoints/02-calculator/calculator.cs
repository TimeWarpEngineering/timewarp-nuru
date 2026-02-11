#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// CALCULATOR - ENDPOINT DSL
// ═══════════════════════════════════════════════════════════════════════════════
// Full-featured calculator using Endpoint DSL pattern.
// Demonstrates: Commands with parameters, dependency injection, testable handlers
// DSL: Endpoint (class-based with [NuruRoute], nested Handler classes)
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using EndpointCalculator.Services;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(ConfigureServices)
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}
