namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Logging levels for Nuru framework components.
/// </summary>
public enum LogLevel
{
  /// <summary>Most detailed information, typically only enabled during development.</summary>
  Trace = 0,
  /// <summary>Detailed information for debugging.</summary>
  Debug = 1,
  /// <summary>General informational messages.</summary>
  Info = 2,
  /// <summary>Warning messages for potentially harmful situations.</summary>
  Warning = 3,
  /// <summary>Error messages for failures.</summary>
  Error = 4,
  /// <summary>No logging.</summary>
  None = 5
}

/// <summary>
/// Components that can produce log output.
/// </summary>
public enum LogComponent
{
  /// <summary>Route registration phase.</summary>
  Registration,
  /// <summary>Lexical analysis of route patterns.</summary>
  Lexer,
  /// <summary>Parsing route patterns into AST.</summary>
  Parser,
  /// <summary>Route matching and resolution.</summary>
  Matcher,
  /// <summary>Parameter binding to delegates.</summary>
  Binder,
  /// <summary>Type conversion operations.</summary>
  TypeConverter,
  /// <summary>Help text generation.</summary>
  HelpGen
}

/// <summary>
/// Central logging system for the Nuru framework with component and level filtering.
/// </summary>
public static class NuruLogger
{
  private static readonly Dictionary<LogComponent, LogLevel> ComponentLevels = InitializeComponentLevels();
  private static readonly LogLevel DefaultLevel = InitializeDefaultLevel();

  private static Dictionary<LogComponent, LogLevel> InitializeComponentLevels()
  {
    var levels = new Dictionary<LogComponent, LogLevel>();

#if !ANALYZER_BUILD
    // Parse combined config (e.g., "Parser:trace,Matcher:debug")
    string? combinedConfig = Environment.GetEnvironmentVariable("NURU_LOG");
    if (!string.IsNullOrEmpty(combinedConfig))
    {
      ParseCombinedConfig(combinedConfig, levels);
    }

    // Parse individual component configs (these override combined config)
    foreach (LogComponent component in Enum.GetValues<LogComponent>())
    {
      string envVar = $"NURU_LOG_{component.ToString().ToUpperInvariant()}";
      string? componentLevel = Environment.GetEnvironmentVariable(envVar);
      if (componentLevel is not null && Enum.TryParse<LogLevel>(componentLevel, true, out LogLevel compLevel))
      {
        levels[component] = compLevel;
      }
    }
#endif
    return levels;
  }

  private static LogLevel InitializeDefaultLevel()
  {
    LogLevel defaultLevel = LogLevel.None;

#if !ANALYZER_BUILD
    // Parse global log level
    string? globalLevel = Environment.GetEnvironmentVariable("NURU_LOG_LEVEL");
    if (globalLevel is not null && Enum.TryParse<LogLevel>(globalLevel, true, out LogLevel level))
    {
      defaultLevel = level;
    }

    // Backwards compatibility with NURU_DEBUG
    if (Environment.GetEnvironmentVariable("NURU_DEBUG") == "true" && ComponentLevels.Count == 0)
    {
      // Enable debug level for all components
      defaultLevel = LogLevel.Debug;
    }
#endif
    return defaultLevel;
  }

  private static void ParseCombinedConfig(string config, Dictionary<LogComponent, LogLevel> levels)
  {
    // Parse format like "Registration:info,Parser:debug,Matcher:trace"
    string[] parts = config.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (string part in parts)
    {
      string[] componentAndLevel = part.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      if (componentAndLevel.Length == 2)
      {
        if (Enum.TryParse<LogComponent>(componentAndLevel[0], true, out LogComponent component) &&
            Enum.TryParse<LogLevel>(componentAndLevel[1], true, out LogLevel level))
        {
          levels[component] = level;
        }
      }
    }
  }

  /// <summary>
  /// Gets the configured log level for a component.
  /// </summary>
  private static LogLevel GetComponentLevel(LogComponent component)
  {
    return ComponentLevels.TryGetValue(component, out LogLevel level) ? level : DefaultLevel;
  }

  /// <summary>
  /// Checks if a log level is enabled for a component.
  /// </summary>
  public static bool IsEnabled(LogComponent component, LogLevel level)
  {
    return level >= GetComponentLevel(component);
  }

  /// <summary>
  /// Logs a message if the specified level is enabled for the component.
  /// </summary>
  public static void Log(LogComponent component, LogLevel level, string message)
  {
#if !ANALYZER_BUILD
    if (IsEnabled(component, level))
    {
      string prefix = level switch
      {
        LogLevel.Trace => "[TRACE]",
        LogLevel.Debug => "[DEBUG]",
        LogLevel.Info => "[INFO]",
        LogLevel.Warning => "[WARN]",
        LogLevel.Error => "[ERROR]",
        _ => ""
      };

      // Add component name for trace/debug levels
      if (level <= LogLevel.Debug)
      {
        prefix = $"{prefix}[{component}]";
      }

      Console.WriteLine($"{prefix} {message}");
    }
#endif
  }

  // Component-specific loggers
  public static ComponentLogger Registration => new(LogComponent.Registration);
  public static ComponentLogger Lexer => new(LogComponent.Lexer);
  public static ComponentLogger Parser => new(LogComponent.Parser);
  public static ComponentLogger Matcher => new(LogComponent.Matcher);
  public static ComponentLogger Binder => new(LogComponent.Binder);
  public static ComponentLogger TypeConverter => new(LogComponent.TypeConverter);
  public static ComponentLogger HelpGen => new(LogComponent.HelpGen);
}