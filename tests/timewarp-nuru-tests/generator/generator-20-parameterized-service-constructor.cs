#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Parameterized Service Constructor (#425)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles services that have
// ONLY parameterized constructors (no parameterless constructor).
//
// WHAT THIS TESTS:
// - Services with constructor dependencies can be resolved via runtime DI
// - The generator uses GetRequiredService<T>() instead of `new T()` when
//   the service has constructor dependencies
// - IConfiguration can be injected as a constructor dependency
//
// REPRODUCES BUG: CS1729 - Cannot use 'new' on a class with no parameterless constructor
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE INTERFACES AND IMPLEMENTATIONS (global scope for generator discovery)
// ═══════════════════════════════════════════════════════════════════════════════

public interface IFormatter
{
  string Format(string input);
}

public class FormatterService : IFormatter
{
  private readonly IConfiguration Configuration;

  // Only parameterized constructor - NO parameterless constructor
  public FormatterService(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  public string Format(string input)
  {
    string prefix = Configuration["Formatter:Prefix"] ?? "[DEFAULT]";
    return $"{prefix} {input}";
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.ParameterizedServiceConstructor
{
  /// <summary>
  /// Tests that verify services with parameterized constructors work correctly.
  /// </summary>
  [TestTag("generator")]
  [TestTag("DI")]
  [TestTag("Task425")]
  public class ParameterizedServiceConstructorTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ParameterizedServiceConstructorTests>();

    /// <summary>
    /// Verify service with only parameterized constructor can be resolved and injected.
    /// This test reproduces issue #425 - services with constructor dependencies
    /// should use runtime DI resolution (GetRequiredService) instead of static `new`.
    /// </summary>
    public static async Task Should_resolve_service_with_parameterized_constructor()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .ConfigureServices(services =>
        {
          // Register IFormatter -> FormatterService (which has parameterized constructor)
          // FormatterService requires IConfiguration, which is provided by AddConfiguration()
          services.AddTransient<IFormatter, FormatterService>();
        })
        .Map("gen20-format {input}")
          .WithHandler((string input, IFormatter formatter) => formatter.Format(input))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen20-format", "Hello"]);

      // Assert
      exitCode.ShouldBe(0);
      // FormatterService reads from configuration, default prefix is [DEFAULT]
      terminal.OutputContains("[DEFAULT] Hello").ShouldBeTrue();
    }
  }
}
