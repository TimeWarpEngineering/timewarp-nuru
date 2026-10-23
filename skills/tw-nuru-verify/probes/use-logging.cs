// Probe: UseLogging (source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs, NuruAppBuilder.UseLogging) routes a handler's ILogger<T> to the
// factory the app passes in. A marker provider writes each entry to stdout.
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

using ILoggerFactory loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(new MarkerLoggerProvider()));

NuruApp app = NuruApp.CreateBuilder()
  .UseLogging(loggerFactory)
  .Map("work")
    .WithHandler((ILogger<WorkLog> logger) =>
    {
      logger.LogInformation("probe-log-entry");
      return "done";
    })
    .AsCommand()
    .Done()
  .Build();

return await app.RunAsync(args);

public sealed class WorkLog;

public sealed class MarkerLoggerProvider : ILoggerProvider
{
  public ILogger CreateLogger(string categoryName) => new MarkerLogger();
  public void Dispose() { }

  private sealed class MarkerLogger : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
      Console.WriteLine($"logged: {formatter(state, exception)}");
  }
}
