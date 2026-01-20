#!/usr/bin/dotnet --

// ===============================================================================
// CONFIGURATION TEST: CLI Configuration Overrides (#353)
// ===============================================================================
//
// PURPOSE: Verify CLI configuration overrides work correctly with IConfiguration
// and IOptions<T> injection.
//
// WHAT THIS TESTS:
// - Flat key overrides: Key=value format
// - Hierarchical key overrides: --Section:Key=value format
// - IConfiguration injection with CLI overrides
// - CLI args take precedence over settings file values
// ===============================================================================

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ===============================================================================
// OPTIONS CLASS (global scope for generator discovery)
// ===============================================================================

/// <summary>
/// Test options class. Convention: "TestConfigOptions" -> "TestConfig" section.
/// </summary>
public class TestConfigOptions
{
  public string Value { get; set; } = "default";
  public int Number { get; set; }
}

// ===============================================================================
// JARIBU TESTS
// ===============================================================================

namespace TimeWarp.Nuru.Tests.Configuration
{
  /// <summary>
  /// Tests that verify CLI configuration overrides work correctly.
  /// </summary>
  [TestTag("Configuration")]
  [TestTag("CliOverrides")]
  public class CliOverrideTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<CliOverrideTests>();

    /// <summary>
    /// Verify flat key override (--Key=value format without section prefix).
    /// </summary>
    public static async Task Should_override_flat_key_from_cli()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = ["cfg02-flat", "--FlatKey=overridden"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("cfg02-flat")
          .WithHandler((IConfiguration config) =>
            $"FlatKey: {config["FlatKey"] ?? "(not set)"}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("FlatKey: overridden").ShouldBeTrue();
    }

    /// <summary>
    /// Verify hierarchical key override (--Section:Key=value format).
    /// Uses IOptions&lt;T&gt; binding with convention-based section name.
    /// </summary>
    public static async Task Should_override_hierarchical_key_from_cli()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = [
        "cfg02-hierarchical",
        "--TestConfig:Value=cli-override",
        "--TestConfig:Number=42"
      ];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("cfg02-hierarchical")
          .WithHandler((IOptions<TestConfigOptions> opts) =>
            $"Value: {opts.Value.Value}, Number: {opts.Value.Number}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Value: cli-override").ShouldBeTrue();
      terminal.OutputContains("Number: 42").ShouldBeTrue();
    }

    /// <summary>
    /// Verify IConfiguration injection with CLI overrides taking precedence
    /// over values from the settings file.
    /// </summary>
    public static async Task Should_inject_configuration_with_cli_overrides()
    {
      // Arrange
      // Settings file has: TestConfig:Value = "from-settings-file", Number = 10
      // CLI overrides: TestConfig:Value = "cli-wins"
      using TestTerminal terminal = new();
      string[] testArgs = [
        "cfg02-precedence",
        "--TestConfig:Value=cli-wins"
      ];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("cfg02-precedence")
          .WithHandler((IOptions<TestConfigOptions> opts, IConfiguration config) =>
          {
            // Both IOptions<T> binding and direct IConfiguration access
            // should show CLI value taking precedence
            string optionsValue = opts.Value.Value;
            string configValue = config["TestConfig:Value"] ?? "(not set)";
            int optionsNumber = opts.Value.Number; // Should be 10 from file (not overridden)
            return $"IOptions: {optionsValue}, IConfig: {configValue}, Number: {optionsNumber}";
          })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      // CLI override should win for Value
      terminal.OutputContains("IOptions: cli-wins").ShouldBeTrue();
      terminal.OutputContains("IConfig: cli-wins").ShouldBeTrue();
      // Number was not overridden, so it should use file value (10) or default (0)
      // depending on whether settings file is loaded
    }

    /// <summary>
    /// Verify default values are used when no CLI override is provided.
    /// </summary>
    public static async Task Should_use_defaults_when_no_cli_override()
    {
      // Arrange - no CLI overrides, should use class defaults
      using TestTerminal terminal = new();
      string[] testArgs = ["cfg02-defaults"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("cfg02-defaults")
          .WithHandler((IOptions<TestConfigOptions> opts) =>
            $"Value: {opts.Value.Value}, Number: {opts.Value.Number}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      // Should use defaults from TestConfigOptions class
      terminal.OutputContains("Value: default").ShouldBeTrue();
      terminal.OutputContains("Number: 0").ShouldBeTrue();
    }

    /// <summary>
    /// Verify forward slash format (/Key=value) is filtered from route matching.
    /// This is an alternative .NET CLI config format.
    /// </summary>
    public static async Task Should_override_with_forward_slash_format()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = ["cfg02-slash", "/SlashKey=slash-value"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("cfg02-slash")
          .WithHandler((IConfiguration config) =>
            $"SlashKey: {config["SlashKey"] ?? "(not set)"}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("SlashKey: slash-value").ShouldBeTrue();
    }

    /// <summary>
    /// Verify forward slash hierarchical format (/Section:Key=value) is filtered.
    /// </summary>
    public static async Task Should_override_with_forward_slash_hierarchical()
    {
      // Arrange
      using TestTerminal terminal = new();
      string[] testArgs = ["cfg02-slash-hier", "/TestConfig:Value=slash-hier"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("cfg02-slash-hier")
          .WithHandler((IOptions<TestConfigOptions> opts) =>
            $"Value: {opts.Value.Value}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Value: slash-hier").ShouldBeTrue();
    }

    /// <summary>
    /// Verify that space-separated options (--key value) are NOT filtered.
    /// Only --key=value format should be filtered as config.
    /// </summary>
    public static async Task Should_not_filter_space_separated_option()
    {
      // Arrange
      using TestTerminal terminal = new();
      // --output followed by space and value should be treated as route option, not config
      string[] testArgs = ["cfg02-option", "--output", "myfile.txt"];

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .Map("cfg02-option --output {file}")
          .WithHandler((string file) => $"Output: {file}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(testArgs);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Output: myfile.txt").ShouldBeTrue();
    }
  }
}
