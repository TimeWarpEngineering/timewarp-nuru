#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CONFIGURATION BASICS
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates IOptions<T> with Endpoint DSL:
// - Options are bound from configuration sections at compile time
// - Convention: DatabaseOptions class → "Database" config section
// - Override convention with [ConfigurationKey("CustomSection")] attribute
//
// DSL: Endpoint with IOptions<T> constructor injection
//
// Settings file: basics.settings.json
// ═══════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);
