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
  /// Example: ["*password*", "*secret*", "*apikey*", "*token*"]
  /// Set to null or empty to save all commands to history.
  /// Default includes common sensitive patterns and history management commands.
  /// </summary>
  public IList<string>? HistoryIgnorePatterns { get; init; } =
  [
    "*password*",
    "*secret*",
    "*token*",
    "*apikey*",
    "*credential*",
    "clear-history"  // Don't add history management commands to history
  ];

  /// <summary>
  /// The name of the key binding profile to use for REPL input handling.
  /// Determines which key combinations trigger which editing actions.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Available profile names:
  /// </para>
  /// <list type="bullet">
  /// <item><description><c>"Default"</c> - PSReadLine-compatible bindings (default)</description></item>
  /// <item><description><c>"Emacs"</c> - Emacs/bash/readline conventions</description></item>
  /// <item><description><c>"Vi"</c> - Vi-inspired bindings</description></item>
  /// <item><description><c>"VSCode"</c> - VSCode-style modern IDE bindings</description></item>
  /// </list>
  /// <para>
  /// Default is <c>"Default"</c> for backward compatibility.
  /// </para>
  /// <para>
  /// This property is ignored if <see cref="KeyBindingProfile"/> is set.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Use Emacs-style key bindings
  /// var options = new ReplOptions
  /// {
  ///   KeyBindingProfileName = "Emacs"
  /// };
  /// </code>
  /// </example>
  public string KeyBindingProfileName { get; set; } = "Default";

  /// <summary>
  /// A custom key binding profile instance to use for REPL input handling.
  /// </summary>
  /// <remarks>
  /// <para>
  /// When set, this takes precedence over <see cref="KeyBindingProfileName"/>.
  /// The value must implement <c>IKeyBindingProfile</c> from TimeWarp.Nuru.Repl.
  /// </para>
  /// <para>
  /// Use this property with <c>CustomKeyBindingProfile</c> to create personalized
  /// key bindings that extend or modify the built-in profiles.
  /// </para>
  /// </remarks>
  /// <example>
  /// <code>
  /// // Use a custom profile based on Emacs
  /// var customProfile = new CustomKeyBindingProfile(new EmacsKeyBindingProfile())
  ///   .Override(ConsoleKey.K, ConsoleModifiers.Control, reader => reader.HandleDeleteToEnd)
  ///   .Remove(ConsoleKey.D, ConsoleModifiers.Control);
  ///
  /// var options = new ReplOptions
  /// {
  ///   KeyBindingProfile = customProfile
  /// };
  /// </code>
  /// </example>
  public object? KeyBindingProfile { get; set; }
}
