namespace TimeWarp.Nuru.Search.Services;

#region Purpose
// Kanban 545: nuru search logged through ILogger<T> with no logging configured, so every warning and
// error (why a CLI did not index, why a search failed) went to a NullLogger. This provider writes log
// entries to stderr so stdout stays results-only.
#endregion

#region Design
// Writes synchronously through TimeWarpTerminal.Default (System.Console is banned) instead of using
// the console logger: the generated logger factory is a static field that is never disposed, and the
// console logger's background queue can drop the last entries when the process exits right after
// logging. One line per entry, "level: message", plus the exception when there is one. The minimum
// level is set by the AddLogging builder, not here.
#endregion

/// <summary>
/// Writes log entries to standard error, one line per entry.
/// </summary>
internal sealed class StandardErrorLoggerProvider : ILoggerProvider
{
  public ILogger CreateLogger(string categoryName) => StandardErrorLogger.Instance;

  public void Dispose() { }

  private sealed class StandardErrorLogger : ILogger
  {
    internal static readonly StandardErrorLogger Instance = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      ArgumentNullException.ThrowIfNull(formatter);
      if (!IsEnabled(logLevel))
        return;

      string message = formatter(state, exception);
      string line = exception is null
        ? $"{FormatLevel(logLevel)}: {message}"
        : $"{FormatLevel(logLevel)}: {message}{Environment.NewLine}{exception}";

      TimeWarpTerminal.Default.WriteErrorLine(line);
    }

    private static string FormatLevel(LogLevel logLevel) => logLevel switch
    {
      LogLevel.Trace => "trace",
      LogLevel.Debug => "debug",
      LogLevel.Information => "info",
      LogLevel.Warning => "warning",
      LogLevel.Error => "error",
      LogLevel.Critical => "critical",
      _ => logLevel.ToString().ToLowerInvariant()
    };
  }
}
