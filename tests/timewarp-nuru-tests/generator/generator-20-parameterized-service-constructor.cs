#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Parameterized Service Constructor (#425, #426)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator resolves services with constructor
// dependencies at compile time using `new T(resolvedDeps...)` - NOT runtime DI.
//
// WHAT THIS TESTS:
// - Services with constructor dependencies are resolved at compile time
// - The generator emits `new ImplType(dep1, dep2)` with deps resolved statically
// - IConfiguration can be injected as a constructor dependency
// - Non-configuration dependencies (other registered services) work
// - Mixed mode: parameterless + parameterized services in same app
// - Transitive dependencies (service depending on service with deps)
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

// Services for non-configuration dependency test
public interface IPrefix
{
  string GetValue();
}

public class PrefixService : IPrefix
{
  public string GetValue() => "Hi";
}

public interface IGreeter
{
  string Greet(string name);
}

public class GreeterService : IGreeter
{
  private readonly IPrefix Prefix;

  public GreeterService(IPrefix prefix)
  {
    Prefix = prefix;
  }

  public string Greet(string name) => $"{Prefix.GetValue()}, {name}!";
}

// Service for mixed mode test (parameterless constructor)
public interface ICounter
{
  int Increment();
}

public class CounterService : ICounter
{
  private int count;
  public int Increment() => ++count;
}

// Service for transitive dependency test
public interface IMessageBuilder
{
  string Build(string name);
}

public class MessageBuilderService : IMessageBuilder
{
  private readonly IGreeter Greeter;

  public MessageBuilderService(IGreeter greeter)
  {
    Greeter = greeter;
  }

  public string Build(string name) => $"[{Greeter.Greet(name)}]";
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.ParameterizedServiceConstructor
{
  /// <summary>
  /// Tests that verify services with parameterized constructors are resolved
  /// at compile time (no MS DI runtime).
  /// </summary>
  [TestTag("generator")]
  [TestTag("DI")]
  [TestTag("Task426")]
  public class ParameterizedServiceConstructorTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ParameterizedServiceConstructorTests>();

    /// <summary>
    /// Service with IConfiguration dependency resolved at compile time.
    /// </summary>
    public static async Task Should_resolve_service_with_configuration_dependency()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .AddConfiguration()
        .ConfigureServices(services =>
        {
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
      terminal.OutputContains("[DEFAULT] Hello").ShouldBeTrue();
    }

    /// <summary>
    /// Service depending on another registered service (not IConfiguration).
    /// No AddConfiguration() needed - proves compile-time resolution works
    /// without configuration in scope.
    /// </summary>
    public static async Task Should_resolve_service_with_registered_service_dependency()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IPrefix, PrefixService>();
          services.AddTransient<IGreeter, GreeterService>();
        })
        .Map("gen20-greet {name}")
          .WithHandler((string name, IGreeter greeter) => greeter.Greet(name))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen20-greet", "World"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hi, World!").ShouldBeTrue();
    }

    /// <summary>
    /// Mixed mode: parameterless service (new T()) and parameterized service
    /// (new T(deps...)) in the same app.
    /// </summary>
    public static async Task Should_resolve_mixed_parameterless_and_parameterized_services()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<ICounter, CounterService>();   // parameterless
          services.AddTransient<IPrefix, PrefixService>();     // parameterless
          services.AddTransient<IGreeter, GreeterService>();   // parameterized (IPrefix)
        })
        .Map("gen20-mixed {name}")
          .WithHandler((string name, ICounter counter, IGreeter greeter) =>
            $"{counter.Increment()}: {greeter.Greet(name)}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen20-mixed", "Test"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("1: Hi, Test!").ShouldBeTrue();
    }

    /// <summary>
    /// Transitive dependencies: ServiceC depends on ServiceB which depends on ServiceA.
    /// All resolved at compile time.
    /// </summary>
    public static async Task Should_resolve_transitive_dependencies()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IPrefix, PrefixService>();              // no deps
          services.AddTransient<IGreeter, GreeterService>();            // depends on IPrefix
          services.AddTransient<IMessageBuilder, MessageBuilderService>(); // depends on IGreeter
        })
        .Map("gen20-transitive {name}")
          .WithHandler((string name, IMessageBuilder builder) => builder.Build(name))
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen20-transitive", "Deep"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("[Hi, Deep!]").ShouldBeTrue();
    }
  }
}
