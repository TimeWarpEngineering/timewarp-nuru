#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// Options classes at global scope for generator discovery

/// <summary>
/// Database options. Convention: "Cfg01DatabaseOptions" -> "Cfg01Database" section.
/// </summary>
public class Cfg01DatabaseOptions
{
  public string ConnectionString { get; set; } = "";
  public int MaxConnections { get; set; }
}

/// <summary>
/// Server options. Convention: "Cfg01ServerOptions" -> "Cfg01Server" section.
/// </summary>
public class Cfg01ServerOptions
{
  public string Host { get; set; } = "";
  public int Port { get; set; }
}

namespace TimeWarp.Nuru.Tests.Configuration
{

/// <summary>
/// Tests for lazy evaluation of IOptions&lt;T&gt; via source generator.
/// Options are bound lazily when handler accesses them, not at startup.
/// This is by design - ValidateOnStart() is not supported because there's no DI container.
/// </summary>
[TestTag("Configuration")]
public class LazyOptionsEvaluationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<LazyOptionsEvaluationTests>();

  // Test that IOptions<T> is lazily bound from configuration when handler accesses it
  public static async Task Should_bind_options_lazily_when_handler_accesses_them()
  {
    // Arrange - use CLI args to set configuration values
    using TestTerminal terminal = new();
    string[] testArgs = ["db", "info", "--Cfg01Database:ConnectionString=Server=localhost;Database=test", "--Cfg01Database:MaxConnections=10"];

    NuruApp app = NuruApp.CreateBuilder(testArgs)
      .UseTerminal(terminal)
      .AddConfiguration()
      .Map("db info").WithHandler((IOptions<Cfg01DatabaseOptions> options) =>
        $"conn:{options.Value.ConnectionString},max:{options.Value.MaxConnections}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(testArgs);

    // Assert - options are bound lazily when handler runs
    exitCode.ShouldBe(0);
    terminal.OutputContains("conn:Server=localhost;Database=test").ShouldBeTrue();
    terminal.OutputContains("max:10").ShouldBeTrue();
  }

  // Test that options use convention-based section key (strip "Options" suffix)
  public static async Task Should_use_convention_based_section_key()
  {
    // Arrange
    using TestTerminal terminal = new();
    string[] testArgs = ["server", "info", "--Cfg01Server:Host=api.example.com", "--Cfg01Server:Port=443"];

    NuruApp app = NuruApp.CreateBuilder(testArgs)
      .UseTerminal(terminal)
      .AddConfiguration()
      .Map("server info").WithHandler((IOptions<Cfg01ServerOptions> options) =>
        $"host:{options.Value.Host},port:{options.Value.Port}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(testArgs);

    // Assert - Cfg01ServerOptions binds to "Cfg01Server" section (convention)
    exitCode.ShouldBe(0);
    terminal.OutputContains("host:api.example.com").ShouldBeTrue();
    terminal.OutputContains("port:443").ShouldBeTrue();
  }

  // Test that missing configuration results in default values
  public static async Task Should_use_default_values_when_section_missing()
  {
    // Arrange - no config args, options should use defaults
    using TestTerminal terminal = new();
    string[] testArgs = ["db", "info"];

    NuruApp app = NuruApp.CreateBuilder(testArgs)
      .UseTerminal(terminal)
      .AddConfiguration()
      .Map("db info").WithHandler((IOptions<Cfg01DatabaseOptions> options) =>
        $"conn:{options.Value.ConnectionString},max:{options.Value.MaxConnections}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(testArgs);

    // Assert - defaults from class definition
    exitCode.ShouldBe(0);
    terminal.OutputContains("conn:,max:0").ShouldBeTrue();
  }

  // Test that handlers without IOptions don't require configuration
  public static async Task Should_work_without_options_injection()
  {
    // Arrange
    using TestTerminal terminal = new();
    string[] testArgs = ["ping"];

    NuruApp app = NuruApp.CreateBuilder(testArgs)
      .UseTerminal(terminal)
      .Map("ping").WithHandler(() => "pong")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(testArgs);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("pong").ShouldBeTrue();
  }

  // Test that multiple IOptions<T> parameters work
  public static async Task Should_support_multiple_options_parameters()
  {
    // Arrange
    using TestTerminal terminal = new();
    string[] testArgs = ["config", "show", "--Cfg01Database:ConnectionString=db-conn", "--Cfg01Server:Host=server-host"];

    NuruApp app = NuruApp.CreateBuilder(testArgs)
      .UseTerminal(terminal)
      .AddConfiguration()
      .Map("config show").WithHandler((IOptions<Cfg01DatabaseOptions> db, IOptions<Cfg01ServerOptions> server) =>
        $"db:{db.Value.ConnectionString},server:{server.Value.Host}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(testArgs);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("db:db-conn").ShouldBeTrue();
    terminal.OutputContains("server:server-host").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Configuration
