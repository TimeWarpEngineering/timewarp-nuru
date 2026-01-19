namespace TimeWarp.Nuru;

/// <summary>
/// Configuration options for help output filtering and display.
/// Controls which routes appear in help listings.
/// </summary>
public sealed class HelpOptions
{
  /// <summary>
  /// Whether to show auto-generated per-command help routes in help output.
  /// When false, routes like "blog --help?" are hidden from listings.
  /// The routes still work, they're just not displayed.
  /// Default: false
  /// </summary>
  public bool ShowPerCommandHelpRoutes { get; set; }

  /// <summary>
  /// Whether to show REPL-specific commands in CLI help output.
  /// REPL commands: exit, quit, q, clear, cls, clear-history, history, help (literal)
  /// When false, these are hidden from CLI --help but shown in REPL's help command.
  /// Default: false
  /// </summary>
  public bool ShowReplCommandsInCli { get; set; }

  /// <summary>
  /// Whether to show shell completion infrastructure routes in help output.
  /// Completion routes: __complete, --generate-completion, --install-completion
  /// Default: false
  /// </summary>
  public bool ShowCompletionRoutes { get; set; }

  /// <summary>
  /// Additional route patterns to exclude from help output.
  /// Supports wildcards: * matches any characters.
  /// Example: ["*-debug", "*-internal"]
  /// </summary>
  public IList<string>? ExcludePatterns { get; init; }

  /// <summary>
  /// Known REPL command patterns that are hidden in CLI mode by default.
  /// </summary>
  internal static readonly HashSet<string> ReplCommandPatterns = new(StringComparer.OrdinalIgnoreCase)
  {
    "exit",
    "quit",
    "q",
    "clear",
    "cls",
    "clear-history",
    "history",
    "help"  // literal help (without dash)
  };

  /// <summary>
  /// Known completion route prefixes.
  /// </summary>
  internal static readonly string[] CompletionRoutePrefixes =
  [
    "__complete",
    "--generate-completion",
    "--install-completion"
  ];
}
