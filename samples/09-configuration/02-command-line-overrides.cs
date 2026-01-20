#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND-LINE OVERRIDES - ASP.NET CORE STYLE CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates command-line configuration overrides with IOptions<T>.
// Answers GitHub Issue #75: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/75
//
// Use ASP.NET Core-style configuration overrides: --Section:Key=Value
// This allows you to override any configuration value from appsettings.json,
// environment variables, or other configuration sources.
//
// Settings file: 02-command-line-overrides.settings.json
//
// Examples:
//   dotnet run samples/09-configuration/02-command-line-overrides.cs -- run
//   dotnet run samples/09-configuration/02-command-line-overrides.cs -- run --Foo:Endpoint=https://override.example.com
//   dotnet run samples/09-configuration/02-command-line-overrides.cs -- show
//   dotnet run samples/09-configuration/02-command-line-overrides.cs -- demo
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder(args)
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
  /// <summary>
  /// Runs the application with current configuration.
  /// Demonstrates IOptions&lt;T&gt; parameter injection.
  /// </summary>
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

  /// <summary>
  /// Shows all configuration values from multiple sources.
  /// Demonstrates multiple IOptions&lt;T&gt; and IConfiguration injection.
  /// </summary>
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

  /// <summary>
  /// Runs an interactive demonstration showing override examples.
  /// </summary>
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

    WriteLine("  $ dotnet run command-line-overrides.cs -- run --Foo:Endpoint=https://override.example.com\n");
    WriteLine("  Result: Uses override Endpoint, other values from settings file\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Example 2: Override multiple values");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  $ dotnet run command-line-overrides.cs -- run \\");
    WriteLine("      --Foo:Endpoint=https://prod.example.com \\");
    WriteLine("      --Foo:MaxItems=100 \\");
    WriteLine("      --Foo:Timeout=60\n");
    WriteLine("  Result: All specified values overridden\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Example 3: Override nested configuration");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  $ dotnet run command-line-overrides.cs -- run \\");
    WriteLine("      --Database:Host=prod-db.example.com \\");
    WriteLine("      --Database:Port=3306\n");
    WriteLine("  Result: Database settings overridden, FooOptions unchanged\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Example 4: View current configuration");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  $ dotnet run command-line-overrides.cs -- show\n");
    WriteLine("  Result: Displays all configuration values\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Key Points");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  • Use colon separator for hierarchical keys: --Section:Key=Value");
    WriteLine("  • Values can also use space separator: --Section:Key Value");
    WriteLine("  • Windows style forward slash also works: /Section:Key=Value");
    WriteLine("  • Command-line args have highest precedence");
    WriteLine("  • Type conversion happens automatically (int, bool, etc.)");
    WriteLine("  • Works identically to ASP.NET Core configuration\n");

    WriteLine("═══════════════════════════════════════════════════════════════");
    WriteLine("Try It Now!");
    WriteLine("═══════════════════════════════════════════════════════════════\n");

    WriteLine("  # Use defaults from settings file");
    WriteLine("  dotnet run command-line-overrides.cs -- run\n");

    WriteLine("  # Override Endpoint");
    WriteLine("  dotnet run command-line-overrides.cs -- run --Foo:Endpoint=https://test.example.com\n");

    WriteLine("  # See all current values");
    WriteLine("  dotnet run command-line-overrides.cs -- show\n");

    await Task.CompletedTask;
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURATION OPTIONS
// ═══════════════════════════════════════════════════════════════════════════════
//
// Convention: "FooOptions" → "Foo" section (strips "Options" suffix)
// Convention: "DatabaseOptions" → "Database" section
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Example options class matching GitHub Issue #75.
/// Section key: "Foo" (convention strips "Options" suffix)
/// </summary>
public class FooOptions
{
  public string Endpoint { get; set; } = "https://default.example.com";
  public int MaxItems { get; set; } = 10;
  public int Timeout { get; set; } = 30;
  public bool Enabled { get; set; } = true;
}

/// <summary>
/// Additional options to demonstrate multiple configuration sections.
/// Section key: "Database" (convention strips "Options" suffix)
/// </summary>
public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
  public string DatabaseName { get; set; } = "myapp";
}
