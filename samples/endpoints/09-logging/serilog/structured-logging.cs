#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - SERILOG STRUCTURED LOGGING
// ═══════════════════════════════════════════════════════════════════════════════════════
//
// Serilog integration with structured logging and multiple sinks.
//
// DSL: Endpoint with Serilog logging via Microsoft.Extensions.Logging
//
// FEATURES:
//   - Structured JSON logging
//   - Multiple output formats
//   - Context enrichment
//   - Log level control
// ═══════════════════════════════════════════════════════════════════════════════════════
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Serilog
#:package Serilog.Extensions.Logging
#:package Serilog.Sinks.Console

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TimeWarp.Nuru;
using static System.Console;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Debug()
  .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
  .Enrich.FromLogContext()
  .CreateLogger();

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddLogging(builder => builder.AddSerilog());
  })
  .DiscoverEndpoints()
  .Build();

try
{
  return await app.RunAsync(args);
}
finally
{
  Log.CloseAndFlush();
}
