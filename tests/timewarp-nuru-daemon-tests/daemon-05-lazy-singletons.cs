#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

// Source-generated DI builds a singleton on first read (task 508-007). A forwarded call, a built-in
// and a parse error construct no user singleton, so warm state loaded in a constructor costs the CLI
// nothing when the daemon runs the route. A local route builds only what its handler and behaviors
// read, once, and the behavior and handler share the instance.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Daemon.LazySingletons
{
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Nuru.Daemon;

public interface ILsWarmState
{
  int Measure(string word);
}

/// <summary>Stands in for warm state (a loaded Roslyn workspace) and counts its constructions.</summary>
public sealed class LsWarmState : ILsWarmState
{
  private static int ConstructedCount;

  public LsWarmState()
  {
    Interlocked.Increment(ref ConstructedCount);
  }

  public static int Constructed => Volatile.Read(ref ConstructedCount);

  public static void Reset() => Volatile.Write(ref ConstructedCount, 0);

  public int Bias { get; init; }

  public int Measure(string word) => (word ?? "").Length + Bias;
}

public interface ILsSharedState
{
  int Id { get; }
}

/// <summary>A singleton read by both a behavior and a handler.</summary>
public sealed class LsSharedState : ILsSharedState
{
  private static int ConstructedCount;

  public LsSharedState()
  {
    Id = Interlocked.Increment(ref ConstructedCount);
  }

  public int Id { get; }

  public static int Constructed => Volatile.Read(ref ConstructedCount);

  public static ILsSharedState? SeenByBehavior { get; set; }

  public static ILsSharedState? SeenByHandler { get; set; }

  public static void Reset()
  {
    Volatile.Write(ref ConstructedCount, 0);
    SeenByBehavior = null;
    SeenByHandler = null;
  }
}

public sealed class LsSharedBehavior(ILsSharedState state) : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    ArgumentNullException.ThrowIfNull(proceed);
    LsSharedState.SeenByBehavior = state;
    await proceed();
  }
}

internal static class LsHandlers
{
  internal static string Shared(ILsSharedState state)
  {
    LsSharedState.SeenByHandler = state;
    return "shared";
  }
}

/// <summary>Answers every offered route as the server would.</summary>
public sealed class LsServerForwarder : INuruRouteForwarder
{
  public int Calls { get; private set; }

  public Task<NuruForwardResult?> ForwardAsync(NuruApp app, NuruForwardRequest request, CancellationToken cancellationToken)
  {
    Calls++;
    return Task.FromResult<NuruForwardResult?>(new NuruForwardResult(0, "from server\n", ""));
  }
}

[TestTag("Daemon")]
public class DaemonLazySingletonTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DaemonLazySingletonTests>();

  private static NuruApp CreateServedApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("ls-app")
      .ConfigureServices(services => services.AddSingleton<ILsWarmState, LsWarmState>())
      .Map("ls-measure {word}").WithHandler((string word, ILsWarmState state) => state.Measure(word)).AsQuery().Done()
      .Map("ls-twice {n:int}").WithHandler((int n, ILsWarmState state) => n * 2 + state.Measure("")).AsQuery().Done()
      .AddServeRoute()
      .Build();
    return app;
  }

  public static async Task Should_not_construct_singletons_for_built_ins()
  {
    LsWarmState.Reset();
    using TestTerminal terminal = new();
    NuruApp app = CreateServedApp(terminal);
    app.RouteForwarder = new LsServerForwarder();

    (await app.ExecuteAsync(["--version"])).ShouldBe(0);
    (await app.ExecuteAsync(["--capabilities"])).ShouldBe(0);
    (await app.ExecuteAsync(["--help"])).ShouldBe(0);

    LsWarmState.Constructed.ShouldBe(0);
  }

  public static async Task Should_not_construct_singletons_for_a_parse_error()
  {
    LsWarmState.Reset();
    using TestTerminal terminal = new();
    NuruApp app = CreateServedApp(terminal);
    app.RouteForwarder = new LsServerForwarder();

    // "abc" is not an int: the call fails before the handler or forwarder runs.
    (await app.ExecuteAsync(["ls-twice", "abc"])).ShouldNotBe(0);

    LsWarmState.Constructed.ShouldBe(0);
  }

  public static async Task Should_not_construct_singletons_for_a_forwarded_call()
  {
    LsWarmState.Reset();
    using TestTerminal terminal = new();
    NuruApp app = CreateServedApp(terminal);
    LsServerForwarder forwarder = new();
    app.RouteForwarder = forwarder;

    (await app.ExecuteAsync(["ls-measure", "hello"])).ShouldBe(0);
    (await app.ExecuteAsync(["ls-twice", "3"])).ShouldBe(0);

    forwarder.Calls.ShouldBe(2);
    terminal.Output.ShouldContain("from server");
    LsWarmState.Constructed.ShouldBe(0);
  }

  public static async Task Should_construct_a_singleton_once_when_a_route_runs_locally()
  {
    LsWarmState.Reset();
    using TestTerminal terminal = new();
    NuruApp app = CreateServedApp(terminal);
    app.RouteForwarder = null;

    (await app.ExecuteAsync(["ls-measure", "hello"])).ShouldBe(0);
    (await app.ExecuteAsync(["ls-twice", "4"])).ShouldBe(0);
    (await app.ExecuteAsync(["ls-measure", "hey"])).ShouldBe(0);

    terminal.Output.ShouldContain("5");
    terminal.Output.ShouldContain("8");
    terminal.Output.ShouldContain("3");
    LsWarmState.Constructed.ShouldBe(1);
  }

  public static async Task Should_share_one_singleton_between_a_behavior_and_the_handler()
  {
    LsWarmState.Reset();
    LsSharedState.Reset();
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("ls-behavior-app")
      .ConfigureServices(services =>
      {
        services.AddSingleton<ILsSharedState, LsSharedState>();
        services.AddSingleton<ILsWarmState, LsWarmState>();
      })
      .AddBehavior(typeof(LsSharedBehavior))
      .Map("ls-shared").WithHandler((ILsSharedState state) => global::TimeWarp.Nuru.Tests.Daemon.LazySingletons.LsHandlers.Shared(state)).AsQuery().Done()
      .Map("ls-other").WithHandler((ILsWarmState state) => state.Measure("x")).AsQuery().Done()
      .Build();

    (await app.ExecuteAsync(["ls-shared"])).ShouldBe(0);
    (await app.ExecuteAsync(["ls-shared"])).ShouldBe(0);

    terminal.Output.ShouldContain("shared");
    LsSharedState.Constructed.ShouldBe(1);
    LsSharedState.SeenByHandler.ShouldNotBeNull();
    LsSharedState.SeenByBehavior.ShouldBeSameAs(LsSharedState.SeenByHandler);
    // A singleton only another route reads is never built.
    LsWarmState.Constructed.ShouldBe(0);
  }
}
}
