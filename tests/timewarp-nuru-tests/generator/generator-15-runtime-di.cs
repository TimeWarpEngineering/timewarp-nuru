#!/usr/bin/env -S dotnet --

#region Purpose
// Pins that UseMicrosoftDependencyInjection() resolves services with constructor dependencies at runtime.
#endregion

#region Design
// Covers #392 and #396: constructor-dependency chains, singleton/transient lifetimes, method group
// ConfigureServices references, and transitive ILogger<T> dependencies.
#endregion

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE INTERFACES AND IMPLEMENTATIONS (global scope for generator discovery)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Simple service with no dependencies.
/// </summary>
public interface IRdi15Formatter
{
  string Format(string format, params object[] args);
}

public class Rdi15Formatter : IRdi15Formatter
{
  public string Format(string format, params object[] args)
    => string.Format(CultureInfo.InvariantCulture, format, args);
}

/// <summary>
/// Service that DEPENDS on IRdi15Formatter via constructor injection.
/// This cannot be instantiated by source-gen DI (which uses new T()).
/// </summary>
public interface IRdi15Greeter
{
  string Greet(string name);
}

public class Rdi15Greeter : IRdi15Greeter
{
  private readonly IRdi15Formatter _formatter;

  // Constructor dependency - requires MS DI to resolve
  public Rdi15Greeter(IRdi15Formatter formatter)
  {
    _formatter = formatter;
  }

  public string Greet(string name)
    => _formatter.Format("Hello, {0}!", name);
}

/// <summary>
/// Service with two-level dependency chain.
/// </summary>
public interface IRdi15FancyGreeter
{
  string FancyGreet(string name);
}

public class Rdi15FancyGreeter : IRdi15FancyGreeter
{
  private readonly IRdi15Greeter _greeter;
  private readonly IRdi15Formatter _formatter;

  // Two constructor dependencies
  public Rdi15FancyGreeter(
    IRdi15Greeter greeter,
    IRdi15Formatter formatter)
  {
    _greeter = greeter;
    _formatter = formatter;
  }

  public string FancyGreet(string name)
    => _formatter.Format("*** {0} ***", _greeter.Greet(name));
}

/// <summary>
/// Service that has ILogger as a constructor dependency.
/// Tests transitive ILogger&lt;T&gt; resolution (#396).
/// </summary>
public interface IRdi15ServiceWithLogger
{
  void DoSomething();
}

#pragma warning disable CA1848 // Use LoggerMessage delegates
public class Rdi15ServiceWithLogger : IRdi15ServiceWithLogger
{
  private readonly ILogger<Rdi15ServiceWithLogger> _logger;

  public Rdi15ServiceWithLogger(ILogger<Rdi15ServiceWithLogger> logger)
  {
    _logger = logger;
  }

  public void DoSomething()
  {
    _logger.LogInformation("Rdi15ServiceWithLogger.DoSomething called!");
  }
}
#pragma warning restore CA1848

// ═══════════════════════════════════════════════════════════════════════════════
// CONFIGURE SERVICES HELPER CLASS (for method group reference tests)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Helper class with ConfigureServices methods for method group reference tests.
/// </summary>
public static class Rdi15ConfigureServicesHelper
{
  /// <summary>
  /// ConfigureServices method for method group reference test.
  /// </summary>
  public static void ConfigureServicesMethodGroup(IServiceCollection services)
  {
    services.AddSingleton<IRdi15Formatter, Rdi15Formatter>();
    services.AddSingleton<IRdi15Greeter, Rdi15Greeter>();
  }

  /// <summary>
  /// ConfigureServices with AddLogging for method group reference test.
  /// </summary>
#pragma warning disable CA1848 // Use LoggerMessage delegates
  public static void ConfigureServicesWithLogging(IServiceCollection services)
  {
    services.AddLogging(builder => builder.AddConsole());
    services.AddSingleton<IRdi15ServiceWithLogger, Rdi15ServiceWithLogger>();
  }
#pragma warning restore CA1848
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.RuntimeDI
{
  /// <summary>
  /// Tests that verify UseMicrosoftDependencyInjection() enables runtime DI.
  /// </summary>
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("RuntimeDI")]
  [TestTag("Task392")]
  public class RuntimeDITests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<RuntimeDITests>();

    /// <summary>
    /// Verify service with constructor dependency works with runtime DI.
    /// </summary>
    public static async Task Should_resolve_service_with_constructor_dependency()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection() // Enable runtime DI
        .ConfigureServices(services =>
        {
          services.AddSingleton<IRdi15Formatter, Rdi15Formatter>();
          services.AddSingleton<IRdi15Greeter, Rdi15Greeter>();
        })
        .Map("rdi15-greet {name}")
          .WithHandler((string name, IRdi15Greeter greeter) => greeter.Greet(name))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["rdi15-greet", "World"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello, World!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify two-level dependency chain resolves correctly.
    /// </summary>
    public static async Task Should_resolve_two_level_dependency_chain()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IRdi15Formatter, Rdi15Formatter>();
          services.AddSingleton<IRdi15Greeter, Rdi15Greeter>();
          services.AddSingleton<IRdi15FancyGreeter, Rdi15FancyGreeter>();
        })
        .Map("rdi15-fancy {name}")
          .WithHandler((string name, IRdi15FancyGreeter greeter) => greeter.FancyGreet(name))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["rdi15-fancy", "Alice"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("*** Hello, Alice! ***").ShouldBeTrue();
    }

    /// <summary>
    /// Verify multiple services with dependencies work together.
    /// </summary>
    public static async Task Should_inject_multiple_services_with_dependencies()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IRdi15Formatter, Rdi15Formatter>();
          services.AddSingleton<IRdi15Greeter, Rdi15Greeter>();
        })
        .Map("rdi15-both {name}")
          .WithHandler((
            string name,
            IRdi15Formatter formatter,
            IRdi15Greeter greeter) =>
          {
            string greeting = greeter.Greet(name);
            return formatter.Format("Formatted: {0}", greeting);
          })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["rdi15-both", "Bob"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Formatted: Hello, Bob!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify method group reference works for ConfigureServices.
    /// Tests the scenario: .ConfigureServices(ConfigureServices)
    /// </summary>
    public static async Task Should_support_method_group_reference()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(Rdi15ConfigureServicesHelper.ConfigureServicesMethodGroup)
        .Map("rdi15-method-group {name}")
          .WithHandler((string name, IRdi15Greeter greeter) => greeter.Greet(name))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["rdi15-method-group", "MethodGroup"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello, MethodGroup!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify service with transitive ILogger dependency works.
    /// This is the exact scenario from issue #396.
    /// </summary>
    public static async Task Should_resolve_service_with_transitive_ilogger_dependency()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddLogging(builder => builder.AddConsole());
          services.AddSingleton<IRdi15ServiceWithLogger, Rdi15ServiceWithLogger>();
        })
        .Map("rdi15-transitive-logger")
          .WithHandler((IRdi15ServiceWithLogger svc) =>
          {
            svc.DoSomething();
            return "Done";
          })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["rdi15-transitive-logger"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Done").ShouldBeTrue();
    }

    /// <summary>
    /// Verify method group reference with transitive ILogger dependency.
    /// This combines both fixes from #396.
    /// </summary>
    public static async Task Should_resolve_method_group_with_transitive_ilogger()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(Rdi15ConfigureServicesHelper.ConfigureServicesWithLogging)
        .Map("rdi15-method-logger")
          .WithHandler((IRdi15ServiceWithLogger svc) =>
          {
            svc.DoSomething();
            return "Method group with logger works!";
          })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["rdi15-method-logger"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Method group with logger works!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify anonymous method syntax (C# 2.0 delegate) works with runtime DI.
    /// </summary>
    public static async Task Should_support_anonymous_method_syntax()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IRdi15Formatter, Rdi15Formatter>();
          services.AddSingleton<IRdi15Greeter, Rdi15Greeter>();
        })
        .Map("rdi15-anon-method {name}")
          // Old C# 2.0 delegate syntax - still valid C#
          .WithHandler(delegate(string name, IRdi15Greeter greeter) { return greeter.Greet(name); })
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["rdi15-anon-method", "Delegate"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello, Delegate!").ShouldBeTrue();
    }
  }
}
