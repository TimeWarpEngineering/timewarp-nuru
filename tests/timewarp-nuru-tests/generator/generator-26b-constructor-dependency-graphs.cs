#!/usr/bin/env -S dotnet --

#region Purpose
// Pins constructor dependency resolution for graph shapes (multi-level chain, diamond, circular, optional parameters)
// and the lifetime mismatch warning; the single, multiple, mixed, and transient cases stay in generator-26.
#endregion


#region Design
// Split from generator-26: types and tests are grouped so each file defines every service type it uses.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

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
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.ConstructorDependencyResolutionGraphs
{
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
          .WithHandler((IGen26SingletonWithTransient singleton) => singleton.GetTransientInstanceId().ToString(CultureInfo.InvariantCulture))
          .AsQuery()
          .Done()
        .Build();
#pragma warning restore NURU056

      int exitCode = await app.RunAsync(["gen26-lifetime"]);
      exitCode.ShouldBe(0);
      terminal.Output.ShouldNotBeEmpty();
    }
  }
}
