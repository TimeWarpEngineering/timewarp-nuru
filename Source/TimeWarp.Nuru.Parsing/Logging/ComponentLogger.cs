namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Component-specific logger that provides convenient logging methods.
/// </summary>
public readonly struct ComponentLogger : IEquatable<ComponentLogger>
{
  private readonly LogComponent Component;

  public ComponentLogger(LogComponent component)
  {
    Component = component;
  }

  /// <summary>
  /// Logs a trace-level message (most verbose).
  /// </summary>
  public void Trace(string message) => NuruLogger.Log(Component, LogLevel.Trace, message);
  /// <summary>
  /// Logs a debug-level message.
  /// </summary>
  public void Debug(string message) => NuruLogger.Log(Component, LogLevel.Debug, message);
  /// <summary>
  /// Logs an info-level message.
  /// </summary>
  public void Info(string message) => NuruLogger.Log(Component, LogLevel.Info, message);
  /// <summary>
  /// Logs a warning-level message.
  /// </summary>
  public void Warn(string message) => NuruLogger.Log(Component, LogLevel.Warning, message);
  /// <summary>
  /// Logs an error-level message.
  /// </summary>
  public void Error(string message) => NuruLogger.Log(Component, LogLevel.Error, message);

  /// <summary>
  /// Checks if trace level is enabled for this component.
  /// </summary>
  public bool IsTraceEnabled => NuruLogger.IsEnabled(Component, LogLevel.Trace);
  /// <summary>
  /// Checks if debug level is enabled for this component.
  /// </summary>
  public bool IsDebugEnabled => NuruLogger.IsEnabled(Component, LogLevel.Debug);
  /// <summary>
  /// Checks if info level is enabled for this component.
  /// </summary>
  public bool IsInfoEnabled => NuruLogger.IsEnabled(Component, LogLevel.Info);

  public override bool Equals(object? obj) => obj is ComponentLogger other && Equals(other);
  public bool Equals(ComponentLogger other) => Component == other.Component;
  public override int GetHashCode() => Component.GetHashCode();
  public static bool operator ==(ComponentLogger left, ComponentLogger right) => left.Equals(right);
  public static bool operator !=(ComponentLogger left, ComponentLogger right) => !left.Equals(right);
}