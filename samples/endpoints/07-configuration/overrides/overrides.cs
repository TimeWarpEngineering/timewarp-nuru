#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - COMMAND-LINE CONFIGURATION OVERRIDES
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates ASP.NET Core-style command-line configuration overrides
// using --Section:Key=Value syntax with Endpoint DSL.
//
// DSL: Endpoint with IConfiguration injection
//
// Usage:
//   ./overrides.cs --Database:Host=prod-db --Api:TimeoutSeconds=60
// ═══════════════════════════════════════════════════════════════════════════════════════
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
