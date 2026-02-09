#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - COMMAND-LINE CONFIGURATION OVERRIDES ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates ASP.NET Core-style command-line configuration overrides
// using --Section:Key=Value syntax with Endpoint DSL.
//
// DSL: Endpoint with IConfiguration injection
//
// Usage:
//   ./endpoint-configuration-overrides.cs --Database:Host=prod-db --Api:TimeoutSeconds=60
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("config-show", Description = "Show effective configuration after overrides")]
public sealed class ConfigShowQuery : IQuery<Unit>
{
  public sealed class Handler(IConfiguration config) : IQueryHandler<ConfigShowQuery, Unit>
  {
    public ValueTask<Unit> Handle(ConfigShowQuery query, CancellationToken ct)
    {
      WriteLine("\n=== Effective Configuration ===");
      WriteLine("(Values may be overridden via command line)\n");

      WriteLine("Database Settings:");
      WriteLine($"  Host: {config["Database:Host"] ?? "(not set)"}");
      WriteLine($"  Port: {config["Database:Port"] ?? "(not set)"}");
      WriteLine($"  Name: {config["Database:DatabaseName"] ?? "(not set)"}");

      WriteLine("\nAPI Settings:");
      WriteLine($"  BaseUrl: {config["Api:BaseUrl"] ?? "(not set)"}");
      WriteLine($"  TimeoutSeconds: {config["Api:TimeoutSeconds"] ?? "(not set)"}");
      WriteLine($"  RetryCount: {config["Api:RetryCount"] ?? "(not set)"}");

      WriteLine("\nLogging Settings:");
      WriteLine($"  Level: {config["Logging:LogLevel:Default"] ?? "(not set)"}");

      return default;
    }
  }
}

[NuruRoute("db-test", Description = "Test database connection with config")]
public sealed class DbTestCommand : ICommand<Unit>
{
  public sealed class Handler(IConfiguration config) : ICommandHandler<DbTestCommand, Unit>
  {
    public ValueTask<Unit> Handle(DbTestCommand command, CancellationToken ct)
    {
      string host = config["Database:Host"] ?? "localhost";
      string port = config["Database:Port"] ?? "5432";

      WriteLine($"Testing connection to {host}:{port}...");
      WriteLine("✓ Connection successful (simulated)");

      return default;
    }
  }
}

[NuruRoute("help-overrides", Description = "Show how to use command-line overrides")]
public sealed class HelpOverridesQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<HelpOverridesQuery, Unit>
  {
    public ValueTask<Unit> Handle(HelpOverridesQuery query, CancellationToken ct)
    {
      WriteLine(@"Command-Line Configuration Overrides
=====================================

Override any configuration value using --Section:Key=Value syntax:

Examples:
  --Database:Host=prod-db.example.com
  --Database:Port=5433
  --Api:TimeoutSeconds=60
  --Logging:LogLevel:Default=Debug

Override Files:
  You can also use a .overrides.json file with the same structure.

Try it:
  ./endpoint-configuration-overrides.cs --Database:Host=prod-db config-show
");

      return default;
    }
  }
}
