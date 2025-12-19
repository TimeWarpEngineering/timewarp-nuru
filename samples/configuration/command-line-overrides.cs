#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator
#:package Microsoft.Extensions.Options
#:package Microsoft.Extensions.Options.ConfigurationExtensions
#:property EnableConfigurationBindingGenerator=true

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND-LINE OVERRIDES - ASP.NET CORE STYLE CONFIGURATION
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with command-line overrides.
// Answers GitHub Issue #75: https://github.com/TimeWarpEngineering/timewarp-nuru/issues/75
//
// Use ASP.NET Core-style configuration overrides: --Section:Key=Value
// This allows you to override any configuration value from appsettings.json,
// environment variables, or other configuration sources.
//
// Settings file: command-line-overrides.settings.json
//
// REQUIRED PACKAGES:
//   #:package Mediator.Abstractions    - Required by NuruApp.CreateBuilder
//   #:package Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;
using static System.Console;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  .Map("run")
    .WithHandler(RunApplicationAsync)
    .WithDescription("Run application with current configuration")
    .AsCommand()
    .Done()
  .Map("show")
    .WithHandler(ShowConfigurationAsync)
    .WithDescription("Show all configuration values and their sources")
    .AsQuery()
    .Done()
  .Map("demo")
    .WithHandler(RunDemonstrationAsync)
    .WithDescription("Run interactive demonstration of override scenarios")
    .AsQuery()
    .Done()
  .Build();

static void ConfigureServices(IServiceCollection services)
{
  // Get configuration from the service provider
  ServiceProvider sp = services.BuildServiceProvider();
  IConfiguration? config = sp.GetService<IConfiguration>();

  if (config != null)
  {
    // Bind configuration sections to strongly-typed options
    services.AddOptions<FooOptions>().Bind(config.GetSection("FooOptions"));
    services.AddOptions<DatabaseOptions>().Bind(config.GetSection("Database"));
  }

  // Register Mediator - required by NuruApp.CreateBuilder
  services.AddMediator();
}

return await app.RunAsync(args);

// Route handlers

async Task RunApplicationAsync(IOptions<FooOptions> fooOptions)
{
  FooOptions foo = fooOptions.Value;

  WriteLine("\n╔════════════════════════════════════════╗");
  WriteLine("║     Application Running with Config    ║");
  WriteLine("╚════════════════════════════════════════╝\n");

  WriteLine($"URL:       {foo.Url}");
  WriteLine($"MaxItems:  {foo.MaxItems}");
  WriteLine($"Timeout:   {foo.Timeout}s");
  WriteLine($"Enabled:   {foo.Enabled}");

  WriteLine("\n✓ Application started successfully\n");

  await Task.CompletedTask;
}

async Task ShowConfigurationAsync
(
  IOptions<FooOptions> fooOptions,
  IOptions<DatabaseOptions> dbOptions,
  IConfiguration config
)
{
  WriteLine("\n╔════════════════════════════════════════╗");
  WriteLine("║       Configuration Values             ║");
  WriteLine("╚════════════════════════════════════════╝\n");

  WriteLine("FooOptions:");
  WriteLine($"  URL:       {fooOptions.Value.Url}");
  WriteLine($"  MaxItems:  {fooOptions.Value.MaxItems}");
  WriteLine($"  Timeout:   {fooOptions.Value.Timeout}s");
  WriteLine($"  Enabled:   {fooOptions.Value.Enabled}");

  WriteLine("\nDatabase:");
  WriteLine($"  Host:      {dbOptions.Value.Host}");
  WriteLine($"  Port:      {dbOptions.Value.Port}");
  WriteLine($"  Name:      {dbOptions.Value.DatabaseName}");

  WriteLine("\nRaw Configuration Access:");
  WriteLine($"  FooOptions:Url = {config["FooOptions:Url"]}");
  WriteLine($"  Database:Host  = {config["Database:Host"]}");

  WriteLine("\n");

  await Task.CompletedTask;
}

async Task RunDemonstrationAsync()
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

  WriteLine("  $ ./command-line-overrides.cs run --FooOptions:Url=https://override.example.com\n");
  WriteLine("  Result: Uses override URL, other values from settings file\n");

  WriteLine("═══════════════════════════════════════════════════════════════");
  WriteLine("Example 2: Override multiple values");
  WriteLine("═══════════════════════════════════════════════════════════════\n");

  WriteLine("  $ ./command-line-overrides.cs run \\");
  WriteLine("      --FooOptions:Url=https://prod.example.com \\");
  WriteLine("      --FooOptions:MaxItems=100 \\");
  WriteLine("      --FooOptions:Timeout=60\n");
  WriteLine("  Result: All specified values overridden\n");

  WriteLine("═══════════════════════════════════════════════════════════════");
  WriteLine("Example 3: Override nested configuration");
  WriteLine("═══════════════════════════════════════════════════════════════\n");

  WriteLine("  $ ./command-line-overrides.cs run \\");
  WriteLine("      --Database:Host=prod-db.example.com \\");
  WriteLine("      --Database:Port=3306\n");
  WriteLine("  Result: Database settings overridden, FooOptions unchanged\n");

  WriteLine("═══════════════════════════════════════════════════════════════");
  WriteLine("Example 4: View current configuration");
  WriteLine("═══════════════════════════════════════════════════════════════\n");

  WriteLine("  $ ./command-line-overrides.cs show\n");
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
  WriteLine("  ./command-line-overrides.cs run\n");

  WriteLine("  # Override URL");
  WriteLine("  ./command-line-overrides.cs run --FooOptions:Url=https://test.example.com\n");

  WriteLine("  # See all current values");
  WriteLine("  ./command-line-overrides.cs show\n");

  await Task.CompletedTask;
}

// Configuration option classes

/// <summary>
/// Example options class matching GitHub Issue #75
/// </summary>
public class FooOptions
{
  public string Url { get; set; } = "https://default.example.com";
  public int MaxItems { get; set; } = 10;
  public int Timeout { get; set; } = 30;
  public bool Enabled { get; set; } = true;
}

/// <summary>
/// Additional options to demonstrate multiple configuration sections
/// </summary>
public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
  public string DatabaseName { get; set; } = "myapp";
}
