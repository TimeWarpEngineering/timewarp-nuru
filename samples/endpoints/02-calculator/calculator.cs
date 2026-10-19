#!/usr/bin/env -S dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// CALCULATOR - ENDPOINT DSL
// ═══════════════════════════════════════════════════════════════════════════════
// Full-featured calculator using Endpoint DSL pattern.
// Demonstrates: Commands with parameters, dependency injection, testable handlers,
//               the built-in `mcp` route (TimeWarp.Nuru.McpHost: every route as an MCP tool)
// DSL: Endpoint (class-based with [NuruRoute], nested Handler classes)
// ═══════════════════════════════════════════════════════════════════════════════
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

using EndpointCalculator.Services;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(ConfigureServices)
  .DiscoverEndpoints()
  // `calculator mcp` serves every route as an MCP tool over stdio
  .AddMcpRoute(instructions: "Each tool takes the same arguments as the CLI route of the same name.")
  .AddRepl(options =>
  {
    options.AutoStartWhenEmpty = true;
  })
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}
