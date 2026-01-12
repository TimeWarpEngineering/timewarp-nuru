#!/usr/bin/dotnet --

// Generator: Method Reference Handler Tests
// Tests that .WithHandler(MethodName) works with method references, not just lambdas
// Reference: kanban/in-progress/320-generator-does-not-support-method-reference-handlers.md

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.MethodReference
{
  /// <summary>
  /// Tests for method reference handlers in the generator.
  /// These tests verify that .WithHandler(MethodName) works correctly
  /// when passing a method reference instead of a lambda expression.
  /// </summary>
  [TestTag("Generator")]
  public class MethodReferenceHandlerTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MethodReferenceHandlerTests>();

    /// <summary>
    /// Test that a simple method reference handler works.
    /// This is the most basic case: .WithHandler(Greet)
    /// </summary>
    public static async Task Should_invoke_method_reference_handler()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler(Greet)
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["greet", "World"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello, World!").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that a method reference with no parameters works.
    /// </summary>
    public static async Task Should_invoke_parameterless_method_reference()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("version")
          .WithHandler(GetVersion)
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["version"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("1.0.0").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that a method reference from a static class works.
    /// Example: .WithHandler(Handlers.Deploy)
    /// </summary>
    public static async Task Should_invoke_static_class_method_reference()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("deploy {env}")
          .WithHandler(Handlers.Deploy)
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["deploy", "production"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Deploying to production").ShouldBeTrue();

      await Task.CompletedTask;
    }

    /// <summary>
    /// Test that a method reference with ITerminal injection works.
    /// </summary>
    public static async Task Should_inject_terminal_into_method_reference()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruCoreApp app = NuruApp.CreateBuilder([])
        .UseTerminal(terminal)
        .Map("status")
          .WithHandler(PrintStatus)
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["status"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Status: OK").ShouldBeTrue();

      await Task.CompletedTask;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // HANDLER METHODS
    // ═══════════════════════════════════════════════════════════════════════════════

    internal static void Greet(string name, ITerminal terminal)
      => terminal.WriteLine($"Hello, {name}!");

    internal static string GetVersion()
      => "1.0.0";

    internal static void PrintStatus(ITerminal terminal)
      => terminal.WriteLine("Status: OK");
  }

  /// <summary>
  /// Static class with handler methods for testing .WithHandler(Handlers.Method) pattern.
  /// </summary>
  internal static class Handlers
  {
    internal static void Deploy(string env, ITerminal terminal)
      => terminal.WriteLine($"Deploying to {env}...");
  }
}
