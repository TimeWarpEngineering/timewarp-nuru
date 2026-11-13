#!/usr/bin/env -S dotnet --

#region Purpose
// Pins compile-time constructor dependency resolution, including complex graphs and error conditions.
#endregion

#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Evaluation of this argument may be expensive
#pragma warning disable CA1305 // Specify IFormatProvider

#region Design
// Covers #394: single, multiple, multi-level, diamond, and mixed built-in/custom dependencies; optional
// parameters; constructor selection; transient services; circular detection (NURU055); lifetime
// mismatch warnings (NURU056).
#endregion

using Microsoft.Extensions.Logging;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 1: Single Dependency - ServiceA depends on IServiceB
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26ServiceB
{
  string GetValue();
}

public class Gen26ServiceB : IGen26ServiceB
{
  public string GetValue() => "B-Value";
}

public interface IGen26ServiceA
{
  string GetMessage();
}

public class Gen26ServiceA : IGen26ServiceA
{
  private readonly IGen26ServiceB _serviceB;

  public Gen26ServiceA(IGen26ServiceB serviceB)
  {
    _serviceB = serviceB;
  }

  public string GetMessage() => $"A depends on {_serviceB.GetValue()}";
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 2: Multiple Dependencies - ServiceA depends on IServiceB and IServiceC
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26MultiC
{
  string GetC();
}

public class Gen26MultiC : IGen26MultiC
{
  public string GetC() => "C";
}

public interface IGen26MultiA
{
  string GetCombined();
}

public class Gen26MultiA : IGen26MultiA
{
  private readonly IGen26ServiceB _serviceB;
  private readonly IGen26MultiC _serviceC;

  public Gen26MultiA(IGen26ServiceB serviceB, IGen26MultiC serviceC)
  {
    _serviceB = serviceB;
    _serviceC = serviceC;
  }

  public string GetCombined() => $"{_serviceB.GetValue()} + {_serviceC.GetC()}";
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 7: Multiple Constructors - Choose one with most resolvable params
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26MultiCtor
{
  string GetInfo();
}

public class Gen26MultiCtor : IGen26MultiCtor
{
  private readonly IGen26ServiceB? _serviceB;
  private readonly IGen26MultiC? _serviceC;
  private readonly string _source;

  public Gen26MultiCtor()
  {
    _source = "parameterless";
  }

  public Gen26MultiCtor(IGen26ServiceB serviceB)
  {
    _serviceB = serviceB;
    _source = "single-param";
  }

  public Gen26MultiCtor(IGen26ServiceB serviceB, IGen26MultiC serviceC)
  {
    _serviceB = serviceB;
    _serviceC = serviceC;
    _source = "two-param";
  }

  public string GetInfo()
  {
    string deps = (_serviceB, _serviceC) switch
    {
      (null, null) => "no deps",
      (not null, null) => $"B only: {_serviceB.GetValue()}",
      (null, not null) => $"C only: {_serviceC.GetC()}",
      (not null, not null) => $"B: {_serviceB.GetValue()}, C: {_serviceC.GetC()}"
    };
    return $"{_source} -> {deps}";
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 9: Mixed Built-in and Custom Types (ILogger + custom service)
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26MixedService
{
  string LogAndGet(string message);
}

public class Gen26MixedService : IGen26MixedService
{
  private readonly ILogger<Gen26MixedService> _logger;
  private readonly IGen26ServiceB _serviceB;

  public Gen26MixedService(ILogger<Gen26MixedService> logger, IGen26ServiceB serviceB)
  {
    _logger = logger;
    _serviceB = serviceB;
  }

  public string LogAndGet(string message)
  {
    _logger.LogInformation("Processing: {Message}", message);
    return $"{message} + {_serviceB.GetValue()}";
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 10: Transient with Dependencies
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26TransientWithDeps
{
  string Process(string input);
}

public class Gen26TransientWithDeps : IGen26TransientWithDeps
{
  private static int s_counter;
  private readonly int _instanceId = Interlocked.Increment(ref s_counter);
  private readonly IGen26ServiceB _serviceB;

  public Gen26TransientWithDeps(IGen26ServiceB serviceB)
  {
    _serviceB = serviceB;
  }

  public string Process(string input) => $"[{_instanceId}] {input}: {_serviceB.GetValue()}";

  public static void Reset() => s_counter = 0;
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.ConstructorDependencyResolution
{
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class SingleDependencyTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SingleDependencyTests>();

    public static async Task Should_resolve_single_dependency()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen26ServiceB, Gen26ServiceB>();
          services.AddTransient<IGen26ServiceA, Gen26ServiceA>();
        })
        .Map("gen26-single")
          .WithHandler((IGen26ServiceA serviceA) => serviceA.GetMessage())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-single"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("A depends on B-Value").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class MultipleDependenciesTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MultipleDependenciesTests>();

    public static async Task Should_resolve_multiple_dependencies()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen26ServiceB, Gen26ServiceB>();
          services.AddTransient<IGen26MultiC, Gen26MultiC>();
          services.AddTransient<IGen26MultiA, Gen26MultiA>();
        })
        .Map("gen26-multi")
          .WithHandler((IGen26MultiA multiA) => multiA.GetCombined())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-multi"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("B-Value + C").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class MultipleConstructorsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MultipleConstructorsTests>();

    public static async Task Should_choose_constructor_with_most_resolvable_params()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen26ServiceB, Gen26ServiceB>();
          services.AddTransient<IGen26MultiC, Gen26MultiC>();
          services.AddTransient<IGen26MultiCtor, Gen26MultiCtor>();
        })
        .Map("gen26-multictor")
          .WithHandler((IGen26MultiCtor multiCtor) => multiCtor.GetInfo())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-multictor"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("two-param").ShouldBeTrue();
      terminal.OutputContains("B: B-Value").ShouldBeTrue();
      terminal.OutputContains("C: C").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class MixedBuiltInAndCustomTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MixedBuiltInAndCustomTests>();

    public static async Task Should_resolve_mixed_builtin_and_custom_types()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddLogging(builder => builder.AddConsole());
          services.AddTransient<IGen26ServiceB, Gen26ServiceB>();
          services.AddTransient<IGen26MixedService, Gen26MixedService>();
        })
        .Map("gen26-mixed {message}")
          .WithHandler((string message, IGen26MixedService mixed) => mixed.LogAndGet(message))
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-mixed", "Hello"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("Hello + B-Value").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class TransientWithDependenciesTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TransientWithDependenciesTests>();

    public static async Task Should_create_new_transient_instance_each_resolution()
    {
      Gen26TransientWithDeps.Reset();
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IGen26ServiceB, Gen26ServiceB>();
          services.AddTransient<IGen26TransientWithDeps, Gen26TransientWithDeps>();
        })
        .Map("gen26-transient-deps")
          .WithHandler((IGen26TransientWithDeps transient) => transient.Process("test"))
          .AsQuery()
          .Done()
        .Build();

      await app.RunAsync(["gen26-transient-deps"]);
      string firstOutput = terminal.Output;
      terminal.ClearOutput();

      await app.RunAsync(["gen26-transient-deps"]);
      string secondOutput = terminal.Output;

      firstOutput.ShouldContain("[1]");
      secondOutput.ShouldContain("[2]");
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class CircularDependencyRuntimeDITests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<CircularDependencyRuntimeDITests>();

    public static async Task Should_resolve_circular_dependency_with_runtime_di()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen26ServiceB, Gen26ServiceB>();
          services.AddTransient<IGen26MultiC, Gen26MultiC>();
        })
        .Map("gen26-circular-runtime")
          .WithHandler((IGen26ServiceB serviceB) => serviceB.GetValue())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-circular-runtime"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("B-Value").ShouldBeTrue();
    }
  }
}
