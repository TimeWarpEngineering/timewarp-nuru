#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: IOptions<T> Parameter Injection (#314)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly emits IOptions<T> parameter
// resolution using configuration binding instead of runtime DI.
//
// WHAT THIS TESTS:
// - Convention: DatabaseOptions → "Database" section (strips "Options" suffix)
// - Attribute: [ConfigurationKey("Api")] ApiSettings → "Api" section
// - IConfiguration parameter injection
// - Generated code uses Options.Create() wrapper
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name.
// To run: dotnet run tests/timewarp-nuru-core-tests/generator/generator-13-ioptions-parameter-injection.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: dotnet run tests/timewarp-nuru-core-tests/generator/generator-13-ioptions-parameter-injection.cs
#endif

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TimeWarp.Nuru;

// Top-level NuruApp - triggers generator. If this compiles, the IOptions injection works!
NuruCoreApp app = NuruApp.CreateBuilder(args)
  // Test: Convention-based section key (DatabaseOptions → "Database")
  .Map("show-db")
    .WithHandler((IOptions<DatabaseOptions> opts) => $"Host: {opts.Value.Host}")
    .WithDescription("Show database config using convention")
    .AsQuery()
    .Done()
  // Test: Attribute-based section key ([ConfigurationKey("Api")] → "Api")
  .Map("show-api")
    .WithHandler((IOptions<ApiSettings> opts) => $"Endpoint: {opts.Value.Endpoint}")
    .WithDescription("Show API config using attribute")
    .AsQuery()
    .Done()
  // Test: IConfiguration parameter injection
  .Map("show-config {key}")
    .WithHandler((string key, IConfiguration config) => $"Value: {config[key] ?? "(not set)"}")
    .WithDescription("Show config value by key")
    .AsQuery()
    .Done()
  // Test: Combined IOptions<T> and IConfiguration
  .Map("show-all")
    .WithHandler((IOptions<DatabaseOptions> dbOpts, IOptions<ApiSettings> apiOpts, IConfiguration config) =>
      $"DB: {dbOpts.Value.Host}, API: {apiOpts.Value.Endpoint}, Env: {config["Environment"] ?? "unknown"}")
    .WithDescription("Show all config sources")
    .AsQuery()
    .Done()
  .Build();

// Run a test command to verify generated code executes
await app.RunAsync(["show-db"]);

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// OPTIONS CLASSES
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
  /// Tests that verify the generated file content for IOptions&lt;T&gt; parameter injection.
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
    /// </summary>
    public static async Task Should_use_convention_for_options_suffix()
    {
      string content = ReadGeneratedFile();

      // Should bind from "Database" section (not "DatabaseOptions")
      content.ShouldContain("GetSection(\"Database\")");
      content.ShouldContain("Get<global::DatabaseOptions>");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify attribute-based section key: [ConfigurationKey("Api")] ApiSettings → "Api".
    /// </summary>
    public static async Task Should_use_attribute_for_configuration_key()
    {
      string content = ReadGeneratedFile();

      // Should bind from "Api" section (from attribute, not "ApiSettings" from convention)
      content.ShouldContain("GetSection(\"Api\")");
      content.ShouldContain("Get<global::ApiSettings>");

      // Should NOT use "ApiSettings" section (that would be the convention without attribute)
      content.ShouldNotContain("GetSection(\"ApiSettings\")");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify Options.Create() wrapper is used for IOptions&lt;T&gt;.
    /// </summary>
    public static async Task Should_wrap_with_options_create()
    {
      string content = ReadGeneratedFile();

      // Should wrap bound options with Options.Create()
      content.ShouldContain("Options.Create(");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify IConfiguration parameter uses configuration variable.
    /// </summary>
    public static async Task Should_inject_iconfiguration()
    {
      string content = ReadGeneratedFile();

      // Should assign configuration to IConfiguration parameter
      content.ShouldContain("IConfiguration");
      content.ShouldContain("= configuration;");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify generated code contains comment with section name.
    /// </summary>
    public static async Task Should_emit_comment_with_section_name()
    {
      string content = ReadGeneratedFile();

      // Should have helpful comment showing which section is used
      content.ShouldContain("from configuration section \"Database\"");
      content.ShouldContain("from configuration section \"Api\"");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify default value fallback for options binding.
    /// </summary>
    public static async Task Should_emit_default_fallback()
    {
      string content = ReadGeneratedFile();

      // Should have ?? new() fallback in case section is missing
      content.ShouldContain("?? new()");

      await Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════════════════════

    private static string ReadGeneratedFile()
    {
      string repoRoot = FindRepoRoot();
      string generatedFile = Path.Combine(
        repoRoot,
        "artifacts",
        "generated",
        "generator-13-ioptions-parameter-injection",
        "TimeWarp.Nuru.Analyzers",
        "TimeWarp.Nuru.Generators.NuruGenerator",
        "NuruGenerated.g.cs");

      if (!File.Exists(generatedFile))
      {
        throw new FileNotFoundException(
          $"Generated file not found at: {generatedFile}\n" +
          "This may indicate the generator did not run or the path has changed.");
      }

      return File.ReadAllText(generatedFile);
    }

    private static string FindRepoRoot()
    {
      string? dir = Environment.CurrentDirectory;

      while (dir is not null)
      {
        if (File.Exists(Path.Combine(dir, "timewarp-nuru.slnx")))
          return dir;

        dir = Path.GetDirectoryName(dir);
      }

      throw new InvalidOperationException(
        $"Could not find repository root from {Environment.CurrentDirectory}");
    }
  }
}
