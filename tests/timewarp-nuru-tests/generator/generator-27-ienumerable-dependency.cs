#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: IEnumerable<T> Dependency Resolution
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Document and test IEnumerable<T> dependency injection patterns.
// These tests use runtime DI to demonstrate expected behavior that source-gen
// DI should eventually support.
//
// WHAT THIS TESTS:
// - Single IEnumerable<IService> dependency in constructor
// - Multiple implementations of same interface resolved as IEnumerable
// - Empty IEnumerable when no implementations registered
// - IEnumerable with mixed lifetimes (Singleton + Transient implementations)
//
// SOURCE-GEN DI TODO:
// - Generator should detect IEnumerable<T> constructor parameters
// - Generator should emit code to resolve all registered implementations
// - Generator should handle empty IEnumerable case (return empty collection)
// - Generator should respect individual implementation lifetimes
// ═══════════════════════════════════════════════════════════════════════════════

using System.Globalization;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 1: Single IEnumerable<IService> Dependency
// Service that takes IEnumerable<IHandler> in constructor
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen27Handler
{
  string Name { get; }
  int Execute(string input);
}

public class Gen27HandlerA : IGen27Handler
{
  public string Name => "HandlerA";
  public int Execute(string input)
  {
    ArgumentNullException.ThrowIfNull(input);
    return input.Length;
  }
}

public class Gen27HandlerB : IGen27Handler
{
  public string Name => "HandlerB";
  public int Execute(string input)
  {
    ArgumentNullException.ThrowIfNull(input);
    return input.Length * 2;
  }
}

public class Gen27HandlerC : IGen27Handler
{
  public string Name => "HandlerC";
  public int Execute(string input)
  {
    ArgumentNullException.ThrowIfNull(input);
    return input.Length * 3;
  }
}

public interface IGen27Processor
{
  string ProcessAll(string input);
}

public class Gen27Processor : IGen27Processor
{
  private readonly IEnumerable<IGen27Handler> _handlers;

  public Gen27Processor(IEnumerable<IGen27Handler> handlers)
  {
    _handlers = handlers;
  }

  public string ProcessAll(string input)
  {
    List<string> results = [];
    foreach (IGen27Handler handler in _handlers)
    {
      int result = handler.Execute(input);
      results.Add($"{handler.Name}={result}");
    }

    return string.Join(", ", results);
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 2: Service with Empty IEnumerable Dependency
// Demonstrates behavior when no implementations are registered
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen27OptionalPlugin
{
  void Apply();
}

public interface IGen27PluginHost
{
  int PluginCount { get; }
  string RunPlugins();
}

public class Gen27PluginHost : IGen27PluginHost
{
  private readonly IEnumerable<IGen27OptionalPlugin> _plugins;

  public Gen27PluginHost(IEnumerable<IGen27OptionalPlugin> plugins)
  {
    _plugins = plugins;
  }

  public int PluginCount => _plugins.Count();

  public string RunPlugins()
  {
    if (!_plugins.Any())
    {
      return "No plugins registered";
    }

    foreach (IGen27OptionalPlugin plugin in _plugins)
    {
      plugin.Apply();
    }

    return $"Executed {_plugins.Count()} plugins";
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 3: Mixed Lifetime Implementations
// Singleton and Transient implementations of same interface
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen27MixedLifetime
{
  string Name { get; }
  int InstanceId { get; }
}

public class Gen27SingletonCore : IGen27MixedLifetime
{
  private static int s_counter;
  private readonly int _instanceId;

  public Gen27SingletonCore()
  {
    _instanceId = Interlocked.Increment(ref s_counter);
  }

  public string Name => "Singleton";
  public int InstanceId => _instanceId;

  public static void Reset() => s_counter = 0;
}

public class Gen27TransientCore : IGen27MixedLifetime
{
  private static int s_counter;
  private readonly int _instanceId;

  public Gen27TransientCore()
  {
    _instanceId = Interlocked.Increment(ref s_counter);
  }

  public string Name => "Transient";
  public int InstanceId => _instanceId;

  public static void Reset() => s_counter = 0;
}

public interface IGen27MixedConsumer
{
  string GetImplementationInfo();
  string GetInstanceIds();
}

public class Gen27MixedConsumer : IGen27MixedConsumer
{
  private readonly IEnumerable<IGen27MixedLifetime> _implementations;

  public Gen27MixedConsumer(IEnumerable<IGen27MixedLifetime> implementations)
  {
    _implementations = implementations;
  }

  public string GetImplementationInfo()
  {
    List<string> names = _implementations.Select(i => i.Name).ToList();
    return string.Join(", ", names);
  }

  public string GetInstanceIds()
  {
    List<string> ids = _implementations.Select(i => $"{i.Name}={i.InstanceId}").ToList();
    return string.Join(", ", ids);
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// TEST 4: Ordered IEnumerable (registration order preserved)
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGen27OrderedStep
{
  int Order { get; }
  string Execute();
}

public class Gen27Step1 : IGen27OrderedStep
{
  public int Order => 1;
  public string Execute() => "Step1";
}

public class Gen27Step2 : IGen27OrderedStep
{
  public int Order => 2;
  public string Execute() => "Step2";
}

public class Gen27Step3 : IGen27OrderedStep
{
  public int Order => 3;
  public string Execute() => "Step3";
}

public interface IGen27Pipeline
{
  string RunPipeline();
}

public class Gen27Pipeline : IGen27Pipeline
{
  private readonly IEnumerable<IGen27OrderedStep> _steps;

  public Gen27Pipeline(IEnumerable<IGen27OrderedStep> steps)
  {
    _steps = steps;
  }

  public string RunPipeline()
  {
    List<string> results = _steps
      .OrderBy(s => s.Order)
      .Select(s => s.Execute())
      .ToList();
    return string.Join(" -> ", results);
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// JARIBU TESTS
// ═══════════════════════════════════════════════════════════════════════════════

namespace TimeWarp.Nuru.Tests.Generator.IEnumerableDependency
{
  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("IEnumerableDependency")]
  public class SingleIEnumerableDependencyTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SingleIEnumerableDependencyTests>();

    public static async Task Should_resolve_all_implementations_as_ienumerable()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen27Handler, Gen27HandlerA>();
          services.AddTransient<IGen27Handler, Gen27HandlerB>();
          services.AddTransient<IGen27Handler, Gen27HandlerC>();
          services.AddTransient<IGen27Processor, Gen27Processor>();
        })
        .Map("gen27-process {input}")
          .WithHandler((string input, IGen27Processor processor) => processor.ProcessAll(input))
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen27-process", "test"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("HandlerA=4").ShouldBeTrue();
      terminal.OutputContains("HandlerB=8").ShouldBeTrue();
      terminal.OutputContains("HandlerC=12").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("IEnumerableDependency")]
  public class MultipleImplementationsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MultipleImplementationsTests>();

    public static async Task Should_enumerate_all_registered_implementations()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IGen27Handler, Gen27HandlerA>();
          services.AddSingleton<IGen27Handler, Gen27HandlerB>();
          services.AddSingleton<IGen27Processor, Gen27Processor>();
        })
        .Map("gen27-count")
          .WithHandler((IGen27Processor processor) => processor.ProcessAll("ab"))
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen27-count"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("HandlerA=2").ShouldBeTrue();
      terminal.OutputContains("HandlerB=4").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("IEnumerableDependency")]
  public class EmptyIEnumerableTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<EmptyIEnumerableTests>();

    public static async Task Should_receive_empty_collection_when_no_implementations()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen27PluginHost, Gen27PluginHost>();
        })
        .Map("gen27-empty")
          .WithHandler((IGen27PluginHost host) => host.RunPlugins())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen27-empty"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("No plugins registered").ShouldBeTrue();
    }

    public static async Task Should_report_zero_plugin_count()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen27PluginHost, Gen27PluginHost>();
        })
        .Map("gen27-count")
          .WithHandler((IGen27PluginHost host) => host.PluginCount.ToString(CultureInfo.InvariantCulture))
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen27-count"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("0").ShouldBeTrue();
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("IEnumerableDependency")]
  public class MixedLifetimeTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MixedLifetimeTests>();

    public static async Task Should_resolve_implementations_with_different_lifetimes()
    {
      Gen27SingletonCore.Reset();
      Gen27TransientCore.Reset();

      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IGen27MixedLifetime, Gen27SingletonCore>();
          services.AddTransient<IGen27MixedLifetime, Gen27TransientCore>();
          services.AddTransient<IGen27MixedConsumer, Gen27MixedConsumer>();
        })
        .Map("gen27-mixed")
          .WithHandler((IGen27MixedConsumer consumer) => consumer.GetImplementationInfo())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen27-mixed"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("Singleton").ShouldBeTrue();
      terminal.OutputContains("Transient").ShouldBeTrue();
    }

    public static async Task Should_maintain_singleton_instance_across_resolutions()
    {
      Gen27SingletonCore.Reset();
      Gen27TransientCore.Reset();

      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddSingleton<IGen27MixedLifetime, Gen27SingletonCore>();
          services.AddTransient<IGen27MixedLifetime, Gen27TransientCore>();
          services.AddTransient<IGen27MixedConsumer, Gen27MixedConsumer>();
        })
        .Map("gen27-ids")
          .WithHandler((IGen27MixedConsumer consumer) => consumer.GetInstanceIds())
          .AsQuery()
          .Done()
        .Build();

      await app.RunAsync(["gen27-ids"]);
      string firstOutput = terminal.Output;
      terminal.ClearOutput();

      await app.RunAsync(["gen27-ids"]);
      string secondOutput = terminal.Output;

      firstOutput.ShouldContain("Singleton=1");
      secondOutput.ShouldContain("Singleton=1");
    }
  }

  [TestTag("Generator")]
  [TestTag("DI")]
  [TestTag("IEnumerableDependency")]
  public class OrderedEnumerableTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<OrderedEnumerableTests>();

    public static async Task Should_preserve_registration_order()
    {
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseMicrosoftDependencyInjection()
        .ConfigureServices(services =>
        {
          services.AddTransient<IGen27OrderedStep, Gen27Step3>();
          services.AddTransient<IGen27OrderedStep, Gen27Step1>();
          services.AddTransient<IGen27OrderedStep, Gen27Step2>();
          services.AddTransient<IGen27Pipeline, Gen27Pipeline>();
        })
        .Map("gen27-pipeline")
          .WithHandler((IGen27Pipeline pipeline) => pipeline.RunPipeline())
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["gen27-pipeline"]);
      exitCode.ShouldBe(0);
      terminal.OutputContains("Step1 -> Step2 -> Step3").ShouldBeTrue();
    }
  }
}
