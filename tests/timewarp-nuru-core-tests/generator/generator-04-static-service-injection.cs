#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Static Service Injection (#292)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly emits static service instantiation
// for services registered via ConfigureServices().
//
// HOW IT WORKS:
// 1. Top-level NuruApp with ConfigureServices triggers generator at compile time
// 2. If it compiles and runs, the generated code is valid
// 3. Jaribu tests verify the generated file content
//
// WHAT THIS TESTS:
// - Singleton services: Lazy<T> static field + .Value access
// - Transient services: new T() each invocation
// - ITerminal: app.Terminal (built-in service)
//
// IMPORTANT: This test must be run in isolation (not via JARIBU_MULTI) because
// it reads the generated file from a path based on the runfile name.
// To run: dotnet run tests/timewarp-nuru-core-tests/generator/generator-04-static-service-injection.cs
// ═══════════════════════════════════════════════════════════════════════════════

#if JARIBU_MULTI
#error This test must be run in isolation. Run: dotnet run tests/timewarp-nuru-core-tests/generator/generator-04-static-service-injection.cs
#endif

using TimeWarp.Nuru;
using TimeWarp.Terminal;
using Microsoft.Extensions.DependencyInjection;

// Top-level NuruApp - triggers generator. If this compiles, the service injection works!
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .AddConfiguration()
  .ConfigureServices(services =>
  {
    // Singleton - should emit Lazy<T> static field
    services.AddSingleton<IGreeter, Greeter>();
    // Transient - should emit new T() each time
    services.AddTransient<IFormatter, Formatter>();
  })
  // Test: Handler with singleton service
  .Map("greet {name}")
    .WithHandler((string name, IGreeter greeter) => greeter.Greet(name))
    .WithDescription("Greet using singleton service")
    .Done()
  // Test: Handler with transient service
  .Map("format {text}")
    .WithHandler((string text, IFormatter formatter) => formatter.Format(text))
    .WithDescription("Format using transient service")
    .Done()
  // Test: Handler with built-in ITerminal service
  .Map("terminal-test")
    .WithHandler((ITerminal terminal) => terminal.WriteLine("Terminal injection works!"))
    .WithDescription("Test ITerminal injection")
    .Done()
  // Test: Handler with multiple services
  .Map("greet-formatted {name}")
    .WithHandler((string name, IGreeter greeter, IFormatter formatter) =>
      formatter.Format(greeter.Greet(name)))
    .WithDescription("Greet and format using both services")
    .Done()
  .Build();

// Run the app to verify generated code executes
await app.RunAsync(["greet", "World"]);

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE INTERFACES AND IMPLEMENTATIONS
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGreeter
{
  string Greet(string name);
}

public interface IFormatter
{
  string Format(string text);
}

public class Greeter : IGreeter
{
  public string Greet(string name) => $"Hello, {name}!";
}

public class Formatter : IFormatter
{
  public string Format(string text) => text?.ToUpperInvariant() ?? string.Empty;
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.StaticServiceInjection
{
  /// <summary>
  /// Tests that verify the generated file content for static service injection.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("Task292")]
  public class StaticServiceInjectionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<StaticServiceInjectionTests>();

    /// <summary>
    /// Verify singleton service gets a Lazy&lt;T&gt; static field.
    /// </summary>
    public static async Task Should_emit_lazy_field_for_singleton_service()
    {
      string content = ReadGeneratedFile();

      // Should have Lazy<Greeter> field
      content.ShouldContain("global::System.Lazy<global::Greeter>");
      content.ShouldContain("__svc_Greeter");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify singleton service field has correct initialization.
    /// </summary>
    public static async Task Should_emit_lazy_initialization_for_singleton_service()
    {
      string content = ReadGeneratedFile();

      // Should have: new(() => new global::Greeter())
      content.ShouldContain("new(() => new global::Greeter())");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify singleton service uses .Value accessor in handler.
    /// </summary>
    public static async Task Should_emit_value_access_for_singleton_service()
    {
      string content = ReadGeneratedFile();

      // Handler should use: __svc_Greeter.Value
      content.ShouldContain("__svc_Greeter.Value");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify transient service uses new T() directly.
    /// </summary>
    public static async Task Should_emit_new_for_transient_service()
    {
      string content = ReadGeneratedFile();

      // Should have: new global::Formatter()
      content.ShouldContain("new global::Formatter()");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify transient service does NOT get a Lazy&lt;T&gt; field.
    /// </summary>
    public static async Task Should_not_emit_lazy_field_for_transient_service()
    {
      string content = ReadGeneratedFile();

      // Should NOT have Lazy<Formatter> or __svc_Formatter
      content.ShouldNotContain("Lazy<global::Formatter>");
      content.ShouldNotContain("__svc_Formatter");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify ITerminal parameter uses app.Terminal (built-in service).
    /// </summary>
    public static async Task Should_emit_app_terminal_for_iterminal_parameter()
    {
      string content = ReadGeneratedFile();

      // Should have: = app.Terminal
      content.ShouldContain("= app.Terminal");

      await Task.CompletedTask;
    }

    /// <summary>
    /// Verify static service fields section header comment is emitted.
    /// </summary>
    public static async Task Should_emit_service_fields_comment()
    {
      string content = ReadGeneratedFile();

      // Should have the comment header
      content.ShouldContain("// Static service fields");

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
        "generator-04-static-service-injection",
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
