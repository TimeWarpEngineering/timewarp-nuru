namespace TimeWarp.Nuru;

/// <summary>
/// Configuration options for REPL (Read-Eval-Print Loop) mode.
/// </summary>
public class ReplOptions
{
  /// <summary>
  /// The prompt string displayed before each command input.
  /// Default is "&gt; ".
  /// </summary>
  public string Prompt { get; set; } = "> ";

  /// <summary>
  /// The welcome message displayed when REPL mode starts.
  /// Set to null to disable.
  /// </summary>
  public string? WelcomeMessage { get; set; } = "TimeWarp.Nuru REPL Mode. Type 'help' for commands, 'exit' to quit.";

  /// <summary>
  /// The goodbye message displayed when REPL mode exits.
  /// Set to null to disable.
  /// </summary>
  public string? GoodbyeMessage { get; set; } = "Goodbye!";

  /// <summary>
  /// Whether to save command history to a file for persistence across sessions.
  /// </summary>
  public bool PersistHistory { get; set; } = true;

  /// <summary>
  /// Path to the history file. If null, uses default location in user's home directory.
  /// </summary>
  public string? HistoryFilePath { get; set; }

  /// <summary>
  /// Maximum number of commands to keep in history.
  /// </summary>
  public int MaxHistorySize { get; set; } = 1000;

  /// <summary>
  /// Whether to continue running after a command fails (returns non-zero exit code).
  /// Default is true (REPL continues on error).
  /// </summary>
  public bool ContinueOnError { get; set; } = true;

  /// <summary>
  /// Whether to display exit code after each command execution.
  /// </summary>
  public bool ShowExitCode { get; set; }

  /// <summary>
  /// Whether to enable colored output for prompts and errors.
  /// </summary>
  public bool EnableColors { get; set; } = true;

  /// <summary>
  /// The ANSI color to use for the prompt when EnableColors is true.
  /// Default is green (\x1b[32m). Set to any ANSI color code string.
  /// Common values: "\x1b[32m" (green), "\x1b[36m" (cyan), "\x1b[33m" (yellow).
  /// </summary>
  public string PromptColor { get; set; } = "\x1b[32m"; // Green

  /// <summary>
  /// Whether to show execution time for each command.
  /// </summary>
  public bool ShowTiming { get; set; } = true;

  /// <summary>
  /// Whether to enable arrow key history navigation.
  /// </summary>
  public bool EnableArrowHistory { get; set; } = true;

  /// <summary>
  /// Patterns for commands that should not be saved to history.
  /// Supports wildcards: * matches any characters, ? matches single character.
  /// Example: ["*password*", "*secret*", "*token*", "*apikey*"]
  /// Set to null or empty to save all commands to history.
  /// Default includes common sensitive patterns.
  /// </summary>
  public IList<string>? HistoryIgnorePatterns { get; init; } =
  [
    "*password*",
    "*secret*",
    "*token*",
    "*apikey*",
    "*credential*"
  ];

}