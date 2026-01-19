namespace TimeWarp.Nuru;

/// <summary>
/// HelpProvider - ANSI color formatting helpers for section headers, usage, descriptions, and message types.
/// </summary>
public static partial class HelpProvider
{
  /// <summary>
  /// Gets the default application name using AppNameDetector.
  /// Falls back to "nuru-app" if detection fails.
  /// </summary>
  private static string GetDefaultAppName()
  {
    try
    {
      return AppNameDetector.GetEffectiveAppName();
    }
    catch (InvalidOperationException)
    {
      return "nuru-app";
    }
  }

  /// <summary>
  /// Formats a section header with optional color (bold yellow).
  /// </summary>
  private static string FormatSectionHeader(string header, bool useColor)
  {
    if (!useColor)
      return header + ":";

    return header.Yellow().Bold() + ":";
  }

  /// <summary>
  /// Formats the usage line with optional color.
  /// </summary>
  private static string FormatUsage(string appName, bool useColor)
  {
    if (!useColor)
      return appName + " [command] [options]";

    return appName.Cyan() + " " + "[command]".Gray() + " " + "[options]".Gray();
  }

  /// <summary>
  /// Formats description text with optional color (gray/dim).
  /// </summary>
  private static string FormatDescription(string description, bool useColor)
  {
    if (!useColor)
      return description;

    return description.Gray();
  }

  /// <summary>
  /// Formats the "(default)" marker with optional color.
  /// </summary>
  private static string FormatDefaultMarker(bool useColor)
  {
    if (!useColor)
      return "(default)";

    return "(default)".Dim();
  }

  /// <summary>
  /// Formats the message type indicator for help display.
  /// </summary>
  /// <param name="messageType">The message type to format.</param>
  /// <param name="useColor">Whether to apply ANSI colors.</param>
  /// <returns>A formatted indicator string like "(Q)", "(C)", or "(I)".</returns>
  private static string FormatMessageTypeIndicator(MessageType messageType, bool useColor)
  {
    string indicator = messageType switch
    {
      MessageType.Unspecified => "( )",
      MessageType.Query => "(Q)",
      MessageType.IdempotentCommand => "(I)",
      MessageType.Command => "(C)",
      _ => "(C)"
    };

    if (!useColor)
      return indicator;

    // Use different colors for different message types:
    // Unspecified = Gray (not yet classified, treated as Command for safety)
    // Query = Blue (safe/informational)
    // IdempotentCommand = Yellow (caution but retryable)
    // Command = Red (danger/confirm)
    return messageType switch
    {
      MessageType.Unspecified => indicator.Gray(),
      MessageType.Query => indicator.Blue(),
      MessageType.IdempotentCommand => indicator.Yellow(),
      MessageType.Command => indicator.Red(),
      _ => indicator.Red()
    };
  }

  /// <summary>
  /// Formats the message type legend for display at the bottom of help output.
  /// </summary>
  /// <param name="useColor">Whether to apply ANSI colors.</param>
  /// <returns>A formatted legend string.</returns>
  private static string FormatMessageTypeLegend(bool useColor)
  {
    if (!useColor)
      return "Legend: ( ) Unspecified  (Q) Query  (I) Idempotent  (C) Command";

    return "Legend: " +
           "( )".Gray() + " Unspecified  " +
           "(Q)".Blue() + " Query  " +
           "(I)".Yellow() + " Idempotent  " +
           "(C)".Red() + " Command";
  }
}
