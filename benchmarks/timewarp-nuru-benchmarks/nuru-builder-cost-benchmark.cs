namespace TimeWarp.Nuru.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Benchmark to measure the cost of each feature in CreateBuilder() vs CreateEmptyBuilder().
/// Used to identify performance regression sources and build a cost matrix.
/// </summary>
/// <remarks>
/// Run with: dotnet run -c Release -- --cost
///
/// Expected output: Cost matrix showing time/memory for each feature toggle,
/// helping identify which extension or initialization step is the performance bottleneck.
/// </remarks>
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
public class NuruBuilderCostBenchmark
{
  private static readonly string[] TestArgs = ["test", "--str", "hello world", "-i", "13", "-b"];

  // ==========================================================================
  // Baseline: CreateEmptyBuilder (no DI, no extensions)
  // ==========================================================================

  [Benchmark(Description = "1-Empty (baseline)", Baseline = true)]
  public async Task EmptyBuilder()
  {
    NuruCoreApp app = NuruCoreApp.CreateEmptyBuilder(TestArgs)
      .Map("test --str {str} -i {intOption:int} -b")
        .WithHandler((string str, int intOption) => { })
        .AsQuery()
        .Done()
      .Build();

    await app.RunAsync(TestArgs);
  }

  // ==========================================================================
  // Full builder with ALL extensions disabled (DI + Config + AutoHelp only)
  // This isolates the cost of BuilderMode.Full initialization
  // ==========================================================================

  [Benchmark(Description = "2-Full-NoExtensions")]
  public async Task FullNoExtensions()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = true,
        DisableCompletion = true,
        DisableInteractiveRoute = true,
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  // ==========================================================================
  // Individual extension costs (one extension enabled at a time)
  // Delta from Full-NoExtensions shows the cost of each extension
  // ==========================================================================

  [Benchmark(Description = "3-Full+Telemetry")]
  public async Task FullPlusTelemetry()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = false, // ENABLED
        DisableRepl = true,
        DisableCompletion = true,
        DisableInteractiveRoute = true,
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  [Benchmark(Description = "4-Full+Repl")]
  public async Task FullPlusRepl()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = false, // ENABLED
        DisableCompletion = true,
        DisableInteractiveRoute = true,
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  [Benchmark(Description = "5-Full+Completion")]
  public async Task FullPlusCompletion()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = true,
        DisableCompletion = false, // ENABLED
        DisableInteractiveRoute = true,
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  [Benchmark(Description = "6-Full+Interactive")]
  public async Task FullPlusInteractive()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = true,
        DisableCompletion = true,
        DisableInteractiveRoute = false, // ENABLED
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  [Benchmark(Description = "7-Full+Version")]
  public async Task FullPlusVersion()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = true,
        DisableCompletion = true,
        DisableInteractiveRoute = true,
        DisableVersionRoute = false, // ENABLED
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  [Benchmark(Description = "8-Full+CheckUpdates")]
  public async Task FullPlusCheckUpdates()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = true,
        DisableCompletion = true,
        DisableInteractiveRoute = true,
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = false // ENABLED
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  // ==========================================================================
  // Full builder with ALL extensions enabled (current CreateBuilder behavior)
  // ==========================================================================

  [Benchmark(Description = "9-Full-AllExtensions")]
  public async Task FullAllExtensions()
  {
    NuruCoreApp app = NuruApp.CreateBuilder(TestArgs)
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();

    await app.RunAsync(TestArgs);
  }

  // ==========================================================================
  // Init vs Run breakdown: Isolate initialization from execution costs
  // ==========================================================================

  // Pre-built apps for RunOnly benchmarks
  private NuruCoreApp? _prebuiltEmptyApp;
  private NuruCoreApp? _prebuiltFullApp;

  [GlobalSetup]
  public void Setup()
  {
    // Diagnostic: Check invoker registration
    Console.WriteLine($"[DIAG] InvokerRegistry.SyncCount = {InvokerRegistry.SyncCount}");
    Console.WriteLine($"[DIAG] InvokerRegistry.AsyncCount = {InvokerRegistry.AsyncCount}");

    // Pre-build Empty app for RunOnly benchmark
    _prebuiltEmptyApp = NuruCoreApp.CreateEmptyBuilder(TestArgs)
      .Map("test --str {str} -i {intOption:int} -b")
        .WithHandler((string str, int intOption) => { })
        .AsQuery()
        .Done()
      .Build();

    // Pre-build Full app for RunOnly benchmark
    _prebuiltFullApp = NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = true,
        DisableCompletion = true,
        DisableInteractiveRoute = true,
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();
  }

  /// <summary>
  /// Measures ONLY pattern parsing cost (Lexer + Parser + Compiler).
  /// </summary>
  [Benchmark(Description = "A1-PatternParse-Only")]
  public CompiledRoute PatternParseOnly()
  {
    return PatternParser.Parse("test --str {str} -i {intOption:int} -b");
  }

  /// <summary>
  /// Measures ONLY Empty builder initialization (no RunAsync).
  /// Cost = Builder creation + Map + Pattern parsing + Build
  /// </summary>
  [Benchmark(Description = "A2-Empty-InitOnly")]
  public NuruCoreApp EmptyBuilderInitOnly()
  {
    return NuruCoreApp.CreateEmptyBuilder(TestArgs)
      .Map("test --str {str} -i {intOption:int} -b")
        .WithHandler((string str, int intOption) => { })
        .AsQuery()
        .Done()
      .Build();
  }

  /// <summary>
  /// Measures ONLY RunAsync with pre-built Empty app.
  /// Cost = Route matching + Parameter binding + Delegate invocation
  /// </summary>
  [Benchmark(Description = "A3-Empty-RunOnly")]
  public Task<int> EmptyBuilderRunOnly()
  {
    return _prebuiltEmptyApp!.RunAsync(TestArgs);
  }

  /// <summary>
  /// Measures ONLY Full builder initialization (no RunAsync).
  /// Cost = DI setup + Configuration + Mediator registration + Build
  /// </summary>
  [Benchmark(Description = "A4-Full-InitOnly")]
  public NuruCoreApp FullBuilderInitOnly()
  {
    return NuruApp.CreateBuilder(TestArgs, new NuruAppOptions
      {
        DisableTelemetry = true,
        DisableRepl = true,
        DisableCompletion = true,
        DisableInteractiveRoute = true,
        DisableVersionRoute = true,
        DisableCheckUpdatesRoute = true
      })
      .ConfigureServices(services => services.AddMediator())
      .Map<CostBenchmarkCommand>("test --str {str} -i {intOption:int} -b")
      .Build();
  }

  /// <summary>
  /// Measures ONLY RunAsync with pre-built Full app.
  /// Cost = Route matching + Parameter binding + Mediator dispatch
  /// </summary>
  [Benchmark(Description = "A5-Full-RunOnly")]
  public Task<int> FullBuilderRunOnly()
  {
    return _prebuiltFullApp!.RunAsync(TestArgs);
  }
}

/// <summary>
/// Test command for cost benchmark. Separate from CliFrameworkBenchmark's TestCommand
/// to avoid conflicts with Mediator source generation.
/// </summary>
public sealed class CostBenchmarkCommand : IRequest
{
  public string Str { get; set; } = string.Empty;
  public int IntOption { get; set; }
  public bool B { get; set; }

  internal sealed class Handler : IRequestHandler<CostBenchmarkCommand>
  {
    public ValueTask<Unit> Handle(CostBenchmarkCommand request, CancellationToken cancellationToken) => default;
  }
}
