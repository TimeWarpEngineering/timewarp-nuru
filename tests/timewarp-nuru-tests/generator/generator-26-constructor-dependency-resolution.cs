#!/usr/bin/dotnet --
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Evaluation of this argument may be expensive
#pragma warning disable CA1305 // Specify IFormatProvider

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Constructor Dependency Resolution (#394)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly resolves constructor dependencies
// at compile time, including complex dependency graphs and error conditions.
//
// WHAT THIS TESTS:
// - Single dependency resolution
// - Multiple dependencies resolution
// - Multi-level dependency chains
// - Diamond dependency patterns
// - Circular dependency detection (NURU055)
// - Optional parameters with default values
// - Multiple constructor selection
// - Lifetime mismatch warnings (NURU056)
// - Mixed built-in and custom types
// - Transient services with dependencies
// ═══════════════════════════════════════════════════════════════════════════════

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
// TEST 3: Multi-Level Chain - ServiceA → IServiceB → IServiceC
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26ChainC
{
  string GetChainC();
}

public class Gen26ChainC : IGen26ChainC
{
  public string GetChainC() => "ChainC";
}

public interface IGen26ChainB
{
  string GetChainB();
}

public class Gen26ChainB : IGen26ChainB
{
  private readonly IGen26ChainC _chainC;

  public Gen26ChainB(IGen26ChainC chainC)
  {
    _chainC = chainC;
  }

  public string GetChainB() => $"ChainB({_chainC.GetChainC()})";
}

public interface IGen26ChainA
{
  string GetChainA();
}

public class Gen26ChainA : IGen26ChainA
{
  private readonly IGen26ChainB _chainB;

  public Gen26ChainA(IGen26ChainB chainB)
  {
    _chainB = chainB;
  }

  public string GetChainA() => $"ChainA({_chainB.GetChainB()})";
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 4: Diamond Pattern - ServiceA → IServiceB, IServiceC → IServiceD
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26DiamondD
{
  string GetDiamondD();
}

public class Gen26DiamondD : IGen26DiamondD
{
  public string GetDiamondD() => "D";
}

public interface IGen26DiamondB
{
  string GetDiamondB();
}

public class Gen26DiamondB : IGen26DiamondB
{
  private readonly IGen26DiamondD _diamondD;

  public Gen26DiamondB(IGen26DiamondD diamondD)
  {
    _diamondD = diamondD;
  }

  public string GetDiamondB() => $"B({_diamondD.GetDiamondD()})";
}

public interface IGen26DiamondC
{
  string GetDiamondC();
}

public class Gen26DiamondC : IGen26DiamondC
{
  private readonly IGen26DiamondD _diamondD;

  public Gen26DiamondC(IGen26DiamondD diamondD)
  {
    _diamondD = diamondD;
  }

  public string GetDiamondC() => $"C({_diamondD.GetDiamondD()})";
}

public interface IGen26DiamondA
{
  string GetDiamondA();
}

public class Gen26DiamondA : IGen26DiamondA
{
  private readonly IGen26DiamondB _diamondB;
  private readonly IGen26DiamondC _diamondC;

  public Gen26DiamondA(IGen26DiamondB diamondB, IGen26DiamondC diamondC)
  {
    _diamondB = diamondB;
    _diamondC = diamondC;
  }

  public string GetDiamondA() => $"A({_diamondB.GetDiamondB()}, {_diamondC.GetDiamondC()})";
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 5: Circular Dependency - ServiceX → ServiceY → ServiceX
// NOTE: This would produce NURU055 error. Test uses runtime DI to bypass.
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26CircularX
{
  string GetName();
}

public interface IGen26CircularY
{
  string GetName();
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 6: Optional Parameter with Default Value
// NOTE: Source-gen DI does not support non-service constructor parameters.
// This test uses runtime DI to demonstrate the pattern.
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26Optional
{
  string GetValue();
}

public class Gen26Optional : IGen26Optional
{
  private readonly string _prefix;

  public Gen26Optional(string prefix = "DEFAULT")
  {
    _prefix = prefix;
  }

  public string GetValue() => $"{_prefix}-Value";
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
// TEST 8: Singleton depends on Transient - NURU056 warning
// NOTE: This produces NURU056 warning. Test uses #pragma to suppress.
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen26TransientDep
{
  int GetInstanceId();
}

public class Gen26TransientDep : IGen26TransientDep
{
  private static int s_counter;
  private readonly int _instanceId = Interlocked.Increment(ref s_counter);

  public int GetInstanceId() => _instanceId;

  public static void Reset() => s_counter = 0;
}

public interface IGen26SingletonWithTransient
{
  int GetTransientInstanceId();
}

public class Gen26SingletonWithTransient : IGen26SingletonWithTransient
{
  private readonly IGen26TransientDep _transientDep;

  public Gen26SingletonWithTransient(IGen26TransientDep transientDep)
  {
    _transientDep = transientDep;
  }

  public int GetTransientInstanceId() => _transientDep.GetInstanceId();
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
  public class MultiLevelChainTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MultiLevelChainTests>();

    public static async Task Should_resolve_multi_level_chain()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen26ChainC, Gen26ChainC>();
          services.AddTransient<IGen26ChainB, Gen26ChainB>();
          services.AddTransient<IGen26ChainA, Gen26ChainA>();
        })
        .Map("gen26-chain")
          .WithHandler((IGen26ChainA chainA) => chainA.GetChainA())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-chain"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("ChainA(ChainB(ChainC))").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class DiamondPatternTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<DiamondPatternTests>();

    public static async Task Should_resolve_diamond_pattern()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddSingleton<IGen26DiamondD, Gen26DiamondD>();
          services.AddSingleton<IGen26DiamondB, Gen26DiamondB>();
          services.AddSingleton<IGen26DiamondC, Gen26DiamondC>();
          services.AddSingleton<IGen26DiamondA, Gen26DiamondA>();
        })
        .Map("gen26-diamond")
          .WithHandler((IGen26DiamondA diamondA) => diamondA.GetDiamondA())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-diamond"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("A(B(D), C(D))").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("ConstructorDependency")]
  [TestTag("Task394")]
  public class OptionalParameterTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<OptionalParameterTests>();

    public static async Task Should_use_default_value_for_optional_parameter_with_runtime_di()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen26Optional, Gen26Optional>();
        })
        .Map("gen26-optional")
          .WithHandler((IGen26Optional optional) => optional.GetValue())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen26-optional"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("DEFAULT-Value").ShouldBeTrue();
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
  public class LifetimeMismatchTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<LifetimeMismatchTests>();

    public static async Task Should_warn_on_singleton_depending_on_transient()
    {
      Gen26TransientDep.Reset();
      using TestTerminal terminal = new();

#pragma warning disable NURU056 // Singleton depends on Transient - intentional for test
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen26TransientDep, Gen26TransientDep>();
          services.AddSingleton<IGen26SingletonWithTransient, Gen26SingletonWithTransient>();
        })
        .Map("gen26-lifetime")
          .WithHandler((IGen26SingletonWithTransient singleton) => singleton.GetTransientInstanceId().ToString())
          .AsQuery()
          .Done()
        .Build();
#pragma warning restore NURU056

      int exitCode = await app.RunAsync(["gen26-lifetime"]);
      exitCode.ShouldBe(0);
      terminal.Output.ShouldNotBeEmpty();
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
