#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: IOptions<T> Parameter Injection (#314)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles IOptions<T> parameter
// injection using configuration binding.
//
// WHAT THIS TESTS:
// - Convention: DatabaseOptions → "Database" section (strips "Options" suffix)
// - Attribute: [ConfigurationKey("Api")] ApiSettings → "Api" section
// - IConfiguration parameter injection
// - Default values when section is missing
// - Multiple IOptions<T> and IConfiguration in one handler
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// OPTIONS CLASSES (global scope for generator discovery)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Uses convention: "DatabaseOptions" → "Database" section (strips "Options" suffix).
/// </summary>
public class DatabaseOptions
{
  public string Host { get; set; } = "localhost";
  public int Port { get; set; } = 5432;
}

/// <summary>
/// Uses attribute: [ConfigurationKey("Api")] → "Api" section.
/// Without the attribute, convention would use "ApiSettings" section.
/// </summary>
[TimeWarp.Nuru.ConfigurationKey("Api")]
public class ApiSettings
{
  public string Endpoint { get; set; } = "https://api.example.com";
  public int TimeoutSeconds { get; set; } = 30;
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.IOptionsInjection
{
  /// <summary>
  /// Tests that verify IOptions&lt;T&gt; and IConfiguration parameter injection functionality.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("IOptions")]
  [TestTag("Task314")]
  public class IOptionsParameterInjectionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<IOptionsParameterInjectionTests>();

    /// <summary>
    /// Verify convention-based section key: DatabaseOptions → "Database".
    /// The "Options" suffix is stripped to determine the configuration section name.
    /// </summary>
    public static async Task Should_bind_options_from_convention_section()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = ["opts13-show-db", "--Database:Host=myhost", "--Database:Port=3306"];

      NuruApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("opts13-show-db")
          .WithHandler((IOptions<DatabaseOptions> opts) =>
            $"Host: {opts.Value.Host}, Port: {opts.Value.Port}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Host: myhost").ShouldBeTrue();
      terminal.OutputContains("Port: 3306").ShouldBeTrue();
    }

    /// <summary>
    /// Verify attribute-based section key: [ConfigurationKey("Api")] ApiSettings → "Api".
    /// The attribute takes precedence over the convention.
    /// </summary>
    public static async Task Should_bind_options_from_attribute_section()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = ["opts13-show-api", "--Api:Endpoint=https://test.com", "--Api:TimeoutSeconds=60"];

      NuruApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("opts13-show-api")
          .WithHandler((IOptions<ApiSettings> opts) =>
            $"Endpoint: {opts.Value.Endpoint}, Timeout: {opts.Value.TimeoutSeconds}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Endpoint: https://test.com").ShouldBeTrue();
      terminal.OutputContains("Timeout: 60").ShouldBeTrue();
    }

    /// <summary>
    /// Verify IConfiguration parameter can be injected and used to read arbitrary config values.
    /// </summary>
    public static async Task Should_inject_iconfiguration()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = ["opts13-show-config", "MyKey", "--MyKey=MyValue"];

      NuruApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("opts13-show-config {key}")
          .WithHandler((string key, IConfiguration config) =>
            $"Value: {config[key] ?? "(not set)"}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode;
      try
      {
        exitCode = await app.RunAsync(testArgs);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Exception: {ex}");
        throw;
      }

      // Assert - Debug output
      Console.WriteLine($"Exit code: {exitCode}");
      Console.WriteLine($"Output: {terminal.Output}");

      exitCode.ShouldBe(0);
      terminal.OutputContains("Value: MyValue").ShouldBeTrue();
    }

    /// <summary>
    /// Verify default values are used when configuration section is missing.
    /// </summary>
    public static async Task Should_use_default_when_section_missing()
    {
      // Arrange - NO Database config passed
      using TestTerminal terminal = new();
      string[] testArgs = ["opts13-defaults"];

      NuruApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("opts13-defaults")
          .WithHandler((IOptions<DatabaseOptions> opts) =>
            $"Host: {opts.Value.Host}, Port: {opts.Value.Port}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert - should use default values from DatabaseOptions class
      exitCode.ShouldBe(0);
      terminal.OutputContains("Host: localhost").ShouldBeTrue();
      terminal.OutputContains("Port: 5432").ShouldBeTrue();
    }

    /// <summary>
    /// Verify multiple IOptions&lt;T&gt; and IConfiguration can be injected into a single handler.
    /// </summary>
    public static async Task Should_inject_multiple_options_and_configuration()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = [
        "opts13-show-all",
        "--Database:Host=dbhost",
        "--Api:Endpoint=https://multi.com",
        "--Environment=Production"
      ];

      NuruApp app = NuruApp.CreateBuilder(testArgs)
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("opts13-show-all")
          .WithHandler((IOptions<DatabaseOptions> dbOpts, IOptions<ApiSettings> apiOpts, IConfiguration config) =>
            $"DB: {dbOpts.Value.Host}, API: {apiOpts.Value.Endpoint}, Env: {config["Environment"] ?? "unknown"}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("DB: dbhost").ShouldBeTrue();
      terminal.OutputContains("API: https://multi.com").ShouldBeTrue();
      terminal.OutputContains("Env: Production").ShouldBeTrue();
    }
  }
}
