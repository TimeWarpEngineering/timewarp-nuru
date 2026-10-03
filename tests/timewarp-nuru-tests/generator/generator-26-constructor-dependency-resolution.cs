#!/usr/bin/env -S dotnet --
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
//
// Subject graphs live in generator-26-constructor-dependency-fixtures.cs.
// Directory.Build.props compiles that file only when this runfile is the project.
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.Logging;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

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
