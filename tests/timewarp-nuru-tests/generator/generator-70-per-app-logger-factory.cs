#!/usr/bin/env -S dotnet --

#region Purpose
// Kanban 544: when one compilation builds several apps, each app's loggers must come from its own
// AddLogging factory. The generator used to pick the first app's factory for every app, so a second
// app logged through the first app's providers and minimum level, and an app with no AddLogging still
// got a real logger. These tests run three apps in one file: one at Information, one at Warning, one
// with no logging. Each logs from a delegate handler, an endpoint handler constructor, a singleton
// service constructor, and a behavior constructor, and each entry must reach only its own app's sink.
// The service pair Gen70.Alpha.Worker / Gen70.Beta.Worker shares a short name: their ILogger<T> fields
// used to be named by short class name, so the generated code declared __fw_ILogger_Worker twice.
#endregion

#region Design
// The capture provider takes an app tag, and the AddLogging lambda body is copied into each app's
// static factory, so a sink entry names the factory it went through. All apps use Map<T>() so the CI
// multi-mode assembly, which builds many apps, adds no foreign endpoints. Runs in multi-mode too: that
// assembly is exactly the several-apps compilation this checks.
#endregion

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

public static class Gen70LogSink
{
  public static System.Collections.Concurrent.ConcurrentQueue<string> Entries { get; } = new();
}

public sealed class Gen70CaptureProvider(string appTag) : ILoggerProvider
{
  public ILogger CreateLogger(string categoryName) => new CaptureLogger(appTag, categoryName);
  public void Dispose() { }

  private sealed class CaptureLogger(string appTag, string categoryName) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
      Gen70LogSink.Entries.Enqueue($"{appTag}|{logLevel}|{categoryName}|{formatter(state, exception)}");
  }
}

public sealed class Gen70DelegateCategory;

// The delegate handler body is copied into generated code, so the helper it calls must be public.
public static class Gen70DelegateLogs
{
  public static string Log(ILogger<Gen70DelegateCategory> logger)
  {
#pragma warning disable CA1848 // Use LoggerMessage delegates
    logger.LogInformation("gen70-delegate-info");
    logger.LogWarning("gen70-delegate-warning");
#pragma warning restore CA1848
    return "gen70 delegate done";
  }
}

#pragma warning disable CA1848 // Use LoggerMessage delegates
public sealed class Gen70LoggingBehavior(ILogger<Gen70LoggingBehavior> logger) : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(proceed);

    logger.LogInformation("gen70-behavior-info");
    logger.LogWarning("gen70-behavior-warning");
    await proceed();
  }
}

[NuruRoute("gen70-work", Description = "Logs through constructor-injected loggers")]
public sealed class Gen70WorkCommand : ICommand<Unit>
{
  public sealed class Handler(
    ILogger<Gen70WorkCommand> logger,
    Gen70.Alpha.Worker alpha,
    Gen70.Beta.Worker beta,
    ITerminal terminal) : ICommandHandler<Gen70WorkCommand, Unit>
  {
    public async Task<Unit> Handle(Gen70WorkCommand request, CancellationToken cancellationToken)
    {
      logger.LogInformation("gen70-endpoint-info");
      logger.LogWarning("gen70-endpoint-warning");
      alpha.Log();
      beta.Log();
      await terminal.WriteLineAsync("gen70 done");
      return Unit.Value;
    }
  }
}

namespace Gen70.Alpha
{
  public sealed class Worker(ILogger<Worker> logger)
  {
    public void Log()
    {
      logger.LogInformation("gen70-alpha-info");
      logger.LogWarning("gen70-alpha-warning");
    }
  }
}

namespace Gen70.Beta
{
  public sealed class Worker(ILogger<Worker> logger)
  {
    public void Log()
    {
      logger.LogInformation("gen70-beta-info");
      logger.LogWarning("gen70-beta-warning");
    }
  }
}
#pragma warning restore CA1848

namespace TimeWarp.Nuru.Tests.Generator.Gen70PerAppLoggerFactory
{
  [TestTag("generator")]
  public class PerAppLoggerFactoryTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<PerAppLoggerFactoryTests>();

    private static readonly string[] Sources = ["delegate", "behavior", "endpoint", "alpha", "beta"];

    public static async Task Should_log_each_app_through_its_own_factory()
    {
      // Arrange: app "info" is built first, so the old generator handed its factory to every app.
      using TestTerminal infoTerminal = new();
      NuruApp infoApp = NuruApp.CreateBuilder()
        .UseTerminal(infoTerminal)
        .ConfigureServices(services =>
        {
          services.AddLogging(builder => builder
            .AddProvider(new Gen70CaptureProvider("info"))
            .SetMinimumLevel(LogLevel.Information));
          services.AddSingleton<Gen70.Alpha.Worker>();
          services.AddSingleton<Gen70.Beta.Worker>();
        })
        .AddBehavior(typeof(Gen70LoggingBehavior))
        .Map<Gen70WorkCommand>()
        .Map("gen70-delegate").WithHandler((ILogger<Gen70DelegateCategory> logger) => Gen70DelegateLogs.Log(logger)).AsCommand().Done()
        .Build();

      using TestTerminal warningTerminal = new();
      NuruApp warningApp = NuruApp.CreateBuilder()
        .UseTerminal(warningTerminal)
        .ConfigureServices(services =>
        {
          services.AddLogging(builder => builder
            .AddProvider(new Gen70CaptureProvider("warning"))
            .SetMinimumLevel(LogLevel.Warning));
          services.AddSingleton<Gen70.Alpha.Worker>();
          services.AddSingleton<Gen70.Beta.Worker>();
        })
        .AddBehavior(typeof(Gen70LoggingBehavior))
        .Map<Gen70WorkCommand>()
        .Map("gen70-delegate").WithHandler((ILogger<Gen70DelegateCategory> logger) => Gen70DelegateLogs.Log(logger)).AsCommand().Done()
        .Build();

      using TestTerminal silentTerminal = new();
      NuruApp silentApp = NuruApp.CreateBuilder()
        .UseTerminal(silentTerminal)
        .ConfigureServices(services =>
        {
          services.AddSingleton<Gen70.Alpha.Worker>();
          services.AddSingleton<Gen70.Beta.Worker>();
        })
        .AddBehavior(typeof(Gen70LoggingBehavior))
        .Map<Gen70WorkCommand>()
        .Map("gen70-delegate").WithHandler((ILogger<Gen70DelegateCategory> logger) => Gen70DelegateLogs.Log(logger)).AsCommand().Done()
        .Build();

      // Act: run the later apps first, then the first app, so no app inherits another's state.
      Gen70LogSink.Entries.Clear();
      int warningWork = await warningApp.RunAsync(["gen70-work"]);
      int warningDelegate = await warningApp.RunAsync(["gen70-delegate"]);
      int silentWork = await silentApp.RunAsync(["gen70-work"]);
      int silentDelegate = await silentApp.RunAsync(["gen70-delegate"]);
      int infoWork = await infoApp.RunAsync(["gen70-work"]);
      int infoDelegate = await infoApp.RunAsync(["gen70-delegate"]);

      // Assert
      int[] exitCodes = [warningWork, warningDelegate, silentWork, silentDelegate, infoWork, infoDelegate];
      exitCodes.ShouldAllBe(static code => code == 0);
      infoTerminal.OutputContains("gen70 done").ShouldBeTrue();
      warningTerminal.OutputContains("gen70 done").ShouldBeTrue();
      silentTerminal.OutputContains("gen70 done").ShouldBeTrue();

      string[] entries = [.. Gen70LogSink.Entries];
      foreach (string source in Sources)
      {
        // The Information app logs both levels; the Warning app drops Information.
        entries.ShouldContain(e => e.StartsWith("info|Information|", StringComparison.Ordinal) && e.Contains($"gen70-{source}-info", StringComparison.Ordinal), source);
        entries.ShouldContain(e => e.StartsWith("info|Warning|", StringComparison.Ordinal) && e.Contains($"gen70-{source}-warning", StringComparison.Ordinal), source);
        entries.ShouldContain(e => e.StartsWith("warning|Warning|", StringComparison.Ordinal) && e.Contains($"gen70-{source}-warning", StringComparison.Ordinal), source);
        entries.ShouldNotContain(e => e.StartsWith("warning|", StringComparison.Ordinal) && e.Contains($"gen70-{source}-info", StringComparison.Ordinal), source);
      }

      // Per allowed level each app logs once from four sources and twice from the behavior (two commands).
      // The app with no AddLogging logged nothing: its loggers are NullLoggers.
      entries.Count(e => e.StartsWith("info|", StringComparison.Ordinal)).ShouldBe(12, string.Join("\n", entries));
      entries.Count(e => e.StartsWith("warning|", StringComparison.Ordinal)).ShouldBe(6, string.Join("\n", entries));
      entries.Length.ShouldBe(18, string.Join("\n", entries));

      // Same-named services in two namespaces each get a logger of their own category.
      entries.ShouldContain($"info|Information|{typeof(Gen70.Alpha.Worker).FullName}|gen70-alpha-info");
      entries.ShouldContain($"info|Information|{typeof(Gen70.Beta.Worker).FullName}|gen70-beta-info");
    }
  }
}
