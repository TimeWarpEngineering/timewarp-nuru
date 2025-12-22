namespace TimeWarp.Terminal;

/// <summary>
/// Abstraction for interactive terminal operations.
/// Extends <see cref="IConsole"/> with capabilities needed for REPL and interactive CLI features.
/// </summary>
/// <remarks>
/// Implement this interface for interactive terminal environments requiring:
/// <list type="bullet">
///   <item><description>Key-by-key input handling (arrow keys, tab completion)</description></item>
///   <item><description>Cursor positioning for line editing</description></item>
///   <item><description>Terminal capability detection</description></item>
///   <item><description>Screen clearing</description></item>
/// </list>
/// </remarks>
public interface ITerminal : IConsole
{
  /// <summary>
  /// Obtains the next character or function key pressed by the user.
  /// </summary>
  /// <param name="intercept">
  /// If true, the pressed key is not displayed in the console window.
  /// </param>
  /// <returns>
  /// An object that describes the <see cref="ConsoleKey"/> constant and Unicode character,
  /// if any, that correspond to the pressed console key.
  /// </returns>
  ConsoleKeyInfo ReadKey(bool intercept);

  /// <summary>
  /// Sets the position of the cursor.
  /// </summary>
  /// <param name="left">The column position of the cursor. Columns are numbered from left to right starting at 0.</param>
  /// <param name="top">The row position of the cursor. Rows are numbered from top to bottom starting at 0.</param>
  void SetCursorPosition(int left, int top);

  /// <summary>
  /// Gets the current position of the cursor.
  /// </summary>
  /// <returns>A tuple containing the column (Left) and row (Top) position of the cursor.</returns>
  (int Left, int Top) GetCursorPosition();

  /// <summary>
  /// Gets the width of the terminal window in characters.
  /// </summary>
  /// <value>The width of the terminal window measured in columns.</value>
  int WindowWidth { get; }

  /// <summary>
  /// Gets a value indicating whether the terminal is interactive.
  /// </summary>
  /// <value>
  /// <c>true</c> if the terminal supports interactive input (not redirected);
  /// otherwise, <c>false</c>.
  /// </value>
  bool IsInteractive { get; }

  /// <summary>
  /// Gets a value indicating whether the terminal supports ANSI color codes.
  /// </summary>
  /// <value>
  /// <c>true</c> if the terminal supports color output;
  /// otherwise, <c>false</c>.
  /// </value>
  bool SupportsColor { get; }

  /// <summary>
  /// Gets a value indicating whether the terminal supports OSC 8 hyperlinks.
  /// </summary>
  /// <value>
  /// <c>true</c> if the terminal supports clickable hyperlinks;
  /// otherwise, <c>false</c>.
  /// </value>
  /// <remarks>
  /// Supported terminals include Windows Terminal, iTerm2, VS Code terminal,
  /// Hyper, Konsole, and GNOME Terminal 3.26+.
  /// </remarks>
  bool SupportsHyperlinks { get; }

  /// <summary>
  /// Clears the console buffer and corresponding console window of display information.
  /// </summary>
  void Clear();
}
