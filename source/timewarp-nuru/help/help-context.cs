namespace TimeWarp.Nuru;

/// <summary>
/// Specifies the context in which help is being displayed.
/// Used to filter context-specific commands from help output.
/// </summary>
public enum HelpContext
{
  /// <summary>
  /// Help displayed in CLI (command-line interface) mode.
  /// REPL-specific commands may be hidden based on configuration.
  /// </summary>
  Cli,

  /// <summary>
  /// Help displayed in REPL (Read-Eval-Print Loop) mode.
  /// REPL-specific commands are always shown.
  /// </summary>
  Repl
}
