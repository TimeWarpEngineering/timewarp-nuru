#!/usr/bin/env -S dotnet --

#region Purpose
// Kanban 527: Nuru configures logging one way, ConfigureServices(s => s.AddLogging(...)). This
// test proves that path end to end with the source-generated DI: the AddLogging lambda (AddConsole
// plus SetMinimumLevel) becomes the app's logger factory, and an endpoint handler's constructor
// ILogger<T> writes through it at the configured level. UseLogging, UseConsoleLogging, UseDebugLogging and
// ConfigureLogging were removed because the generator never supported them.
#endregion

#region Design
// The generator copies the AddLogging lambda body into a static LoggerFactory field, so the
// capture provider and its sink are public global-scope types the generated code can name. The
// console provider is in the same lambda to keep the consumer shape; its output goes to
// System.Console, which TestTerminal does not capture, so the assertions read the capture sink.
// The tw-nuru-verify add-logging probe checks the console output in a real process.
// The app uses Map<T>() rather than DiscoverEndpoints() so the CI multi-file assembly does not
// pull other files' endpoints in. It runs in that assembly too: each app logs through its own
// AddLogging factory (kanban 544), so the other files' apps do not hide this capture provider.
// generator-70 checks that separation directly. The delegate handler test covers kanban 541: a
// WithHandler((ILogger<T> logger) => ...) parameter used to compile to a null logger.
#endregion

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

public static class Gen66LogSink
{
  public static System.Collections.Concurrent.ConcurrentQueue<string> Entries { get; } = new();
}

public sealed class Gen66CaptureProvider : ILoggerProvider
{
  public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName);
  public void Dispose() { }

  private sealed class CaptureLogger(string categoryName) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
      Gen66LogSink.Entries.Enqueue($"{logLevel}|{categoryName}|{formatter(state, exception)}");
  }
}

public sealed class Gen66DelegateCategory;

[NuruRoute("gen66-work", Description = "Logs through an injected ILogger<T>")]
public sealed class Gen66WorkCommand : ICommand<Unit>
{
  public sealed class Handler(ILogger<Gen66WorkCommand> logger, ITerminal terminal) : ICommandHandler<Gen66WorkCommand, Unit>
  {
    public async Task<Unit> Handle(Gen66WorkCommand request, CancellationToken cancellationToken)
    {
#pragma warning disable CA1848 // Use LoggerMessage delegates
      logger.LogDebug("gen66-debug-entry");
      logger.LogInformation("gen66-info-entry");
#pragma warning restore CA1848
      await terminal.WriteLineAsync("gen66 done");
      return Unit.Value;
    }
  }
}

namespace TimeWarp.Nuru.Tests.Generator.Gen66AddLoggingInjectedLogger
{
  [TestTag("generator")]
  public class AddLoggingInjectedLoggerTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<AddLoggingInjectedLoggerTests>();

    public static async Task Should_log_through_injected_logger_at_configured_level()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services => services.AddLogging(builder => builder
          .AddConsole()
          .AddProvider(new Gen66CaptureProvider())
          .SetMinimumLevel(LogLevel.Information)))
        .Map<Gen66WorkCommand>()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen66-work"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("gen66 done").ShouldBeTrue();
      string[] entries = [.. Gen66LogSink.Entries];
      entries.ShouldContain($"Information|{typeof(Gen66WorkCommand).FullName}|gen66-info-entry");
      entries.Any(e => e.Contains("gen66-debug-entry", StringComparison.Ordinal)).ShouldBeFalse();
    }

    public static async Task Should_log_through_delegate_handler_logger_parameter()
    {
      // Arrange
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .ConfigureServices(services => services.AddLogging(builder => builder
          .AddConsole()
          .AddProvider(new Gen66CaptureProvider())
          .SetMinimumLevel(LogLevel.Information)))
        .Map("gen66-delegate").WithHandler((ILogger<Gen66DelegateCategory> logger) =>
        {
#pragma warning disable CA1848 // Use LoggerMessage delegates
          logger.LogDebug("gen66-delegate-debug");
          logger.LogInformation("gen66-delegate-info");
#pragma warning restore CA1848
          return "gen66 delegate done";
        }).AsCommand().Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["gen66-delegate"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("gen66 delegate done").ShouldBeTrue();
      string[] entries = [.. Gen66LogSink.Entries];
      entries.ShouldContain($"Information|{typeof(Gen66DelegateCategory).FullName}|gen66-delegate-info");
      entries.Any(e => e.Contains("gen66-delegate-debug", StringComparison.Ordinal)).ShouldBeFalse();
    }
  }
}
