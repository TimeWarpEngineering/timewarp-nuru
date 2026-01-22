#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: ConfigureServices Patterns
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify different ConfigureServices patterns work correctly.
//
// WHAT THIS TESTS:
// - Lambda expressions (inline)
// - Qualified method references (ClassName.MethodName)
// - Anonymous method expressions (delegate syntax)
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE INTERFACES AND IMPLEMENTATIONS
// ═══════════════════════════════════════════════════════════════════════════════

public interface ICsp16Service
{
  string GetMessage();
}

public class Csp16Service : ICsp16Service
{
  public string GetMessage() => "Service working!";
}

/// <summary>
/// Helper class with ConfigureServices methods for qualified method reference tests.
/// </summary>
public static class Csp16ServiceConfig
{
  public static void ConfigureServices(IServiceCollection services)
  {
    services.AddSingleton<ICsp16Service, Csp16Service>();
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.ConfigureServicesPatterns
{
  [TestTag("Generator")]
  [TestTag("ConfigureServices")]
  public class ConfigureServicesPatternsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ConfigureServicesPatternsTests>();

    /// <summary>
    /// Verify lambda expression works for ConfigureServices.
    /// </summary>
    public static async Task Should_support_lambda_expression()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<ICsp16Service, Csp16Service>();
        })
        .Map("csp16-lambda")
          .WithHandler((ICsp16Service svc) => svc.GetMessage())
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["csp16-lambda"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Service working!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify qualified method reference (ClassName.MethodName) works for ConfigureServices.
    /// </summary>
    public static async Task Should_support_qualified_method_reference()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(Csp16ServiceConfig.ConfigureServices) // Qualified reference
        .Map("csp16-qualified")
          .WithHandler((ICsp16Service svc) => svc.GetMessage())
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["csp16-qualified"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Service working!").ShouldBeTrue();
    }

    /// <summary>
    /// Verify anonymous method expression works for ConfigureServices.
    /// </summary>
    public static async Task Should_support_anonymous_method_expression()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(delegate(IServiceCollection services)
        {
          services.AddSingleton<ICsp16Service, Csp16Service>();
        })
        .Map("csp16-delegate")
          .WithHandler((ICsp16Service svc) => svc.GetMessage())
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["csp16-delegate"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Service working!").ShouldBeTrue();
    }
  }
}
