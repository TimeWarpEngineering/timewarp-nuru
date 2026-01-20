namespace TimeWarp.Nuru;

/// <summary>
/// Provides execution context information for the current session.
/// Registered as a singleton to allow detection of REPL vs CLI context at runtime.
/// </summary>
/// <remarks>
/// This class is used to determine the appropriate HelpContext at execution time
/// rather than registration time, ensuring both "help" and "--help" commands
/// behave consistently based on actual execution context.
/// </remarks>
public sealed class SessionContext
{
  /// <summary>
  /// Gets or sets whether the current session is running in REPL mode.
  /// </summary>
  /// <remarks>
  /// Set to true when entering a REPL session, and reset to false when exiting.
  /// Defaults to false for CLI execution context.
  /// </remarks>
  public bool IsReplSession { get; set; }

  /// <summary>
  /// Gets or sets whether the terminal supports ANSI color codes.
  /// </summary>
  /// <remarks>
  /// Set by the NuruApp based on the terminal's SupportsColor property.
  /// Defaults to true for color-enabled output.
  /// </remarks>
  public bool SupportsColor { get; set; } = true;

  /// <summary>
  /// Gets the appropriate HelpContext based on the current session state.
  /// </summary>
  /// <remarks>
  /// Returns <see cref="HelpContext.Repl"/> when <see cref="IsReplSession"/> is true,
  /// otherwise returns <see cref="HelpContext.Cli"/>.
  /// </remarks>
  public HelpContext HelpContext => IsReplSession ? HelpContext.Repl : HelpContext.Cli;
}
