#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// FLUENT DSL - COMMAND-LINE OVERRIDES
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates command-line configuration overrides with Fluent DSL.
// Answers GitHub Issue #75.
//
// DSL: Fluent API with catch-all parameter for configuration args
//
// Use ASP.NET Core-style configuration overrides: --Section:Key=Value
// This allows you to override any configuration value from appsettings.json,
// environment variables, or other configuration sources.
//
// Examples:
//   dotnet run fluent-configuration-overrides.cs -- run
//   dotnet run fluent-configuration-overrides.cs -- run --Foo:Endpoint=https://override.example.com
//   dotnet run fluent-configuration-overrides.cs -- show
//   dotnet run fluent-configuration-overrides.cs -- demo
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  // Note: {*configArgs} captures any --Section:Key=Value args, which are processed by
  // the configuration system via AddCommandLine(args) before route matching.
  .Map("run {*configArgs}")
    .WithHandler(Handlers.RunApplicationAsync)
    .WithDescription("Run application with current configuration")
    .AsCommand()
    .Done()
  .Map("show {*configArgs}")
    .WithHandler(Handlers.ShowConfigurationAsync)
    .WithDescription("Show all configuration values and their sources")
    .AsQuery()
    .Done()
  .Map("demo")
    .WithHandler(Handlers.RunDemonstrationAsync)
    .WithDescription("Run interactive demonstration of override scenarios")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);

// ═══════════════════════════════════════════════════════════════════════════════
// HANDLERS
// ═══════════════════════════════════════════════════════════════════════════════

internal static class Handlers
{
  internal static async Task RunApplicationAsync(IOptions<FooOptions> fooOptions)
  {
    FooOptions foo = fooOptions.Value;

    WriteLine("\n╔════════════════════════════════════════╗");
    WriteLine("║     Application Running with Config    ║");
    WriteLine("╚════════════════════════════════════════╝\n");

    WriteLine($"Endpoint: {foo.Endpoint}");
    WriteLine($"MaxItems: {foo.MaxItems}");
    WriteLine($"Timeout:  {foo.Timeout}s");
    WriteLine($"Enabled:  {foo.Enabled}");

    WriteLine("\n✓ Application started successfully\n");

    await Task.CompletedTask;
  }

  internal static async Task ShowConfigurationAsync(
    IOptions<FooOptions> fooOptions,
    IOptions<DatabaseOptions> dbOptions,
    IConfiguration config)
  {
    WriteLine("\n╔════════════════════════════════════════╗");
    WriteLine("║       Configuration Values             ║");
    WriteLine("╚════════════════════════════════════════╝\n");

    WriteLine("FooOptions (section: \"Foo\"):");
    WriteLine($"  Endpoint: {fooOptions.Value.Endpoint}");
    WriteLine($"  MaxItems: {fooOptions.Value.MaxItems}");
    WriteLine($"  Timeout:  {fooOptions.Value.Timeout}s");
    WriteLine($"  Enabled:  {fooOptions.Value.Enabled}");

    WriteLine("\nDatabaseOptions (section: \"Database\"):");
    WriteLine($"  Host:     {dbOptions.Value.Host}");
    WriteLine($"  Port:     {dbOptions.Value.Port}");
    WriteLine($"  Name:     {dbOptions.Value.DatabaseName}");

    WriteLine("\nRaw Configuration Access:");
    WriteLine($"  Foo:Endpoint    = {config["Foo:Endpoint"]}");
    WriteLine($"  Database:Host   = {config["Database:Host"]}");

    WriteLine();

    await Task.CompletedTask;
  }

  internal static async Task RunDemonstrationAsync()
  {
    WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
    WriteLine("║          Command-Line Override Demonstration                   ║");
    WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

    WriteLine("This example demonstrates ASP.NET Core-style configuration overrides.");
    WriteLine("Configuration sources are loaded in this order (later overrides earlier):\n");

    WriteLine("  1. appsettings.json (optional, shared)");
    WriteLine("  2. appsettings.{Environment}.json (optional)");
    WriteLine("  3. command-line-overrides.settings.json (application-specific)");
    WriteLine("  4. Environment variables");
    WriteLine("  5. Command-line arguments ← Highest precedence\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Example 1: Override a single value");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  $ dotnet run fluent-configuration-overrides.cs -- run --Foo:Endpoint=https://override.example.com\n");
    WriteLine("  Result: Uses override Endpoint, other values from settings file\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Example 2: Override multiple values");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  $ dotnet run fluent-configuration-overrides.cs -- run \\\n");
    WriteLine("      --Foo:Endpoint=https://prod.example.com \\\n");
    WriteLine("      --Foo:MaxItems=100 \\\n");
    WriteLine("      --Foo:Timeout=60\n");
    WriteLine("  Result: All specified values overridden\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Example 3: View current configuration");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  $ dotnet run fluent-configuration-overrides.cs -- show\n");
    WriteLine("  Result: Displays all configuration values\n");

    await Task.CompletedTask;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION OPTIONS
// ═══════════════════════════════════════════════════════════════════════════════

[TimeWarp.Nuru.ConfigurationKey("Foo")]
public class FooOptions
{
  public string Endpoint { get; set; } = "https://default.example.com";
  public int MaxItems { get; set; } = 10;
  public int Timeout { get; set; } = 30;
  public bool Enabled { get; set; } = true;
}

public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
  public string DatabaseName { get; set; } = "myapp";
}
