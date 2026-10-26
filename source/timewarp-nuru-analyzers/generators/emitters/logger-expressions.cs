#region Purpose
// The one place generated code gets a logger from. Delegate handler parameters, endpoint handler
// constructors, and behavior constructors all resolve ILogger<T>, ILogger, and ILoggerFactory here,
// so every injection site receives the same logger (task 541: delegates got a silent default!).
#endregion

#region Design
// The logger factory source has two shapes (see InterceptorEmitter.GetFirstLoggerFactoryFieldName):
// a static __loggerFactory field built from AddLogging (the lambda body, or no providers for a
// parameterless AddLogging(), task 546; non-null), or app.LoggerFactory
// for telemetry logging (nullable, so the expression falls back to a NullLogger). With neither, the
// logger is a NullLogger and NURU_H007 warns. Types are matched by full name, so a user interface
// whose name merely contains "ILogger" is resolved as an ordinary service, not as a logger.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Expressions that resolve logging services from the app's logger factory.
/// </summary>
internal static class LoggerExpressions
{
  private const string LoggingNamespace = "Microsoft.Extensions.Logging.";
  private const string NullLoggerFactory = "global::Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance";

  /// <summary>
  /// True for <c>ILogger&lt;T&gt;</c>, <c>ILogger</c>, and <c>ILoggerFactory</c> from Microsoft.Extensions.Logging.
  /// </summary>
  /// <param name="typeName">The type name, with or without the <c>global::</c> prefix.</param>
  public static bool IsLoggingServiceType(string? typeName)
  {
    if (typeName is null)
      return false;

    string normalized = typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName[8..] : typeName;
    if (!normalized.StartsWith(LoggingNamespace, StringComparison.Ordinal))
      return false;

    string name = normalized[LoggingNamespace.Length..];
    return name is "ILogger" or "ILoggerFactory"
      || (name.StartsWith("ILogger<", StringComparison.Ordinal) && name.EndsWith('>'));
  }

  /// <summary>
  /// True only for the non-generic <c>ILoggerFactory</c> from Microsoft.Extensions.Logging.
  /// <c>ILogger&lt;ILoggerFactory&gt;</c> is a logger, not the factory (review 541 M1).
  /// </summary>
  /// <param name="typeName">The type name, with or without the <c>global::</c> prefix.</param>
  public static bool IsLoggerFactoryType(string? typeName)
  {
    if (typeName is null)
      return false;

    string normalized = typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName[8..] : typeName;
    return normalized == LoggingNamespace + "ILoggerFactory";
  }

  /// <summary>
  /// The category type of <c>ILogger&lt;T&gt;</c>, or <c>object</c> for the non-generic <c>ILogger</c>.
  /// </summary>
  public static string GetCategoryTypeName(string loggerTypeName)
  {
    int start = loggerTypeName.IndexOf('<', StringComparison.Ordinal);
    int end = loggerTypeName.LastIndexOf('>');

    return start >= 0 && end > start ? loggerTypeName[(start + 1)..end] : "object";
  }

  /// <summary>
  /// The expression for a logging service. Call only when <see cref="IsLoggingServiceType"/> is true.
  /// </summary>
  /// <param name="typeName">The logging service type.</param>
  /// <param name="loggerFactoryFieldName">The static factory field, <c>app.LoggerFactory</c>, or null when no logging is configured.</param>
  public static string Resolve(string typeName, string? loggerFactoryFieldName)
  {
    bool runtimeFactory = loggerFactoryFieldName?.Contains('.', StringComparison.Ordinal) == true;

    if (IsLoggerFactoryType(typeName))
    {
      return loggerFactoryFieldName switch
      {
        null => NullLoggerFactory,
        _ when runtimeFactory => $"({loggerFactoryFieldName} ?? {NullLoggerFactory})",
        _ => loggerFactoryFieldName
      };
    }

    string category = GetCategoryTypeName(typeName);
    if (loggerFactoryFieldName is null)
      return $"{NullLoggerFactory}.CreateLogger<{category}>()";

    // Telemetry path: app.LoggerFactory is nullable.
    if (runtimeFactory)
      return $"({loggerFactoryFieldName}?.CreateLogger<{category}>() ?? global::Microsoft.Extensions.Logging.Abstractions.NullLogger<{category}>.Instance)";

    // AddLogging path: the static field is never null.
    return $"{loggerFactoryFieldName}.CreateLogger<{category}>()";
  }
}
