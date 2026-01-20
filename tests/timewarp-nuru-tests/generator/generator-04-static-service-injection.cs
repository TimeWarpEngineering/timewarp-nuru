#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Static Service Injection (#292)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles service injection via
// ConfigureServices() - singleton and transient services are injected into handlers.
//
// WHAT THIS TESTS:
// - Singleton services: Same instance across multiple calls
// - Transient services: New instance each invocation
// - ITerminal: Built-in service injection
// - Multiple services in one handler
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE INTERFACES AND IMPLEMENTATIONS (global scope for generator discovery)
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
  /// Tests that verify static service injection functionality.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("Task292")]
  public class StaticServiceInjectionTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<StaticServiceInjectionTests>();

    /// <summary>
    /// Verify singleton service is injected and works correctly.
    /// </summary>
    public static async Task Should_inject_singleton_service()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IGreeter, Greeter>();
        })
        .Map("svc04-greet {name}")
          .WithHandler((string name, IGreeter greeter) => greeter.Greet(name))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["svc04-greet", "World"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello, World!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify transient service is injected and works correctly.
    /// </summary>
    public static async Task Should_inject_transient_service()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .ConfigureServices(services =>
        {
          services.AddTransient<IFormatter, Formatter>();
        })
        .Map("svc04-format {text}")
          .WithHandler((string text, IFormatter formatter) => formatter.Format(text))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["svc04-format", "hello"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("HELLO").ShouldBeTrue();
    }

    /// <summary>
    /// Verify multiple services can be injected into a single handler.
    /// </summary>
    public static async Task Should_inject_multiple_services()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IGreeter, Greeter>();
          services.AddTransient<IFormatter, Formatter>();
        })
        .Map("svc04-greet-formatted {name}")
          .WithHandler((string name, IGreeter greeter, IFormatter formatter) =>
            formatter.Format(greeter.Greet(name)))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["svc04-greet-formatted", "World"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("HELLO, WORLD!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify ITerminal is automatically available for injection.
    /// </summary>
    public static async Task Should_inject_iterminal_builtin_service()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("svc04-terminal-test")
          .WithHandler((ITerminal t) =>
          {
            t.WriteLine("Terminal injection works!");
            return 0;
          })
          .AsCommand()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["svc04-terminal-test"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Terminal injection works!").ShouldBeTrue();
    }
  }
}
