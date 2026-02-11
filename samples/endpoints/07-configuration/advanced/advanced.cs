#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - ADVANCED CONFIGURATION PATTERNS
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates advanced configuration patterns:
// - Nested configuration objects
// - Collection binding
// - Dictionary binding
// - Post-configuration callbacks
//
// DSL: Endpoint with complex IOptions<T> structures
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using TimeWarp.Terminal;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
