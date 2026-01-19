namespace TimeWarp.Nuru;

/// <summary>
/// Yank argument handlers for the REPL console reader.
/// Implements PSReadLine YankLastArg and YankNthArg operations.
/// </summary>
/// <remarks>
/// YankLastArg (Alt+.) inserts the last argument from the previous history entry.
/// Consecutive presses cycle through history, inserting the last argument from
/// progressively older commands.
///
/// YankNthArg (Alt+Ctrl+Y) inserts the Nth argument from the previous command,
/// where N can be specified with a digit argument prefix (Alt+0, Alt+1, etc.).
/// </remarks>
public sealed partial class ReplConsoleReader
{
  // Yank argument state fields
  private bool LastCommandWasYankArg;      // For consecutive YankLastArg to cycle through history
  private int YankArgHistoryIndex;         // History index for YankLastArg cycling
  private int LastYankArgStart;            // Start position of last yanked argument text
  private int LastYankArgLength;           // Length of last yanked argument text
  private int? DigitArgument;              // Digit prefix for YankNthArg (e.g., Alt+3)

  /// <summary>
  /// PSReadLine: YankLastArg - Insert the last argument from the previous history line.
  /// Consecutive presses cycle through history, inserting older last arguments.
  /// </summary>
  /// <remarks>
  /// Behavior:
  /// - First press: Insert last argument from most recent history entry
  /// - Consecutive presses: Cycle through older history entries
  /// - Non-YankLastArg command resets the cycle
  ///
  /// Example:
  /// History:
  ///   [1] git commit -m "Initial commit"
  ///   [2] git push origin main
  ///   [3] echo "done"
  ///
  /// Alt+. → "done" (last arg of most recent)
  /// Alt+. → "main" (last arg of older command)
  /// Alt+. → "Initial commit" (last arg, quoted string)
  /// </remarks>
  internal void HandleYankLastArg()
  {
    if (History.Count == 0)
      return;

    SaveUndoState(isCharacterInput: false);

    // Determine which history entry to use
    int historyIndexToUse;
    if (LastCommandWasYankArg)
    {
      // Consecutive press - go to older history entry
      historyIndexToUse = YankArgHistoryIndex - 1;

      // Remove previously yanked text first
      if (LastYankArgLength > 0 && LastYankArgStart >= 0 && LastYankArgStart + LastYankArgLength <= UserInput.Length)
      {
        UserInput = UserInput[..LastYankArgStart] + UserInput[(LastYankArgStart + LastYankArgLength)..];
        CursorPosition = LastYankArgStart;
      }
    }
    else
    {
      // First press - start from most recent history
      historyIndexToUse = History.Count - 1;
    }

    // Find a history entry with arguments
    while (historyIndexToUse >= 0)
    {
      string[] args = ParseHistoryArguments(History[historyIndexToUse]);
      if (args.Length > 0)
      {
        // Use digit argument if set, otherwise use last argument
        int argIndex = DigitArgument.HasValue
          ? Math.Min(DigitArgument.Value, args.Length - 1)
          : args.Length - 1;

        string argToInsert = args[argIndex];

        // Insert the argument at cursor position
        LastYankArgStart = CursorPosition;
        LastYankArgLength = argToInsert.Length;
        UserInput = UserInput[..CursorPosition] + argToInsert + UserInput[CursorPosition..];
        CursorPosition += argToInsert.Length;

        // Update state for consecutive presses
        YankArgHistoryIndex = historyIndexToUse;
        LastCommandWasYankArg = true;
        DigitArgument = null;  // Clear digit argument after use

        CompletionHandler.Reset();
        RedrawLine();
        return;
      }

      historyIndexToUse--;
    }

    // No history entry with arguments found - reset state
    LastCommandWasYankArg = false;
    DigitArgument = null;
  }

  /// <summary>
  /// PSReadLine: YankNthArg - Insert the Nth argument from the previous command.
  /// N is determined by the digit argument prefix (Alt+0, Alt+1, etc.).
  /// Without a prefix, defaults to the first argument (index 1, skipping command name).
  /// </summary>
  /// <remarks>
  /// Example:
  /// Previous command: git commit -m "Initial commit"
  /// Arguments: [0]="git" [1]="commit" [2]="-m" [3]="Initial commit"
  ///
  /// Alt+Ctrl+Y → "commit" (first arg after command, index 1)
  /// Alt+0 Alt+Ctrl+Y → "git" (command name, index 0)
  /// Alt+3 Alt+Ctrl+Y → "Initial commit" (index 3)
  /// </remarks>
  internal void HandleYankNthArg()
  {
    if (History.Count == 0)
      return;

    // Get most recent history entry
    string lastCommand = History[^1];
    string[] args = ParseHistoryArguments(lastCommand);

    if (args.Length == 0)
      return;

    SaveUndoState(isCharacterInput: false);

    // Determine which argument to yank
    // Without digit argument: default to index 1 (first arg after command name)
    // With digit argument: use that index
    int argIndex = DigitArgument ?? 1;

    // Clamp to valid range
    if (argIndex >= args.Length)
      argIndex = args.Length - 1;
    if (argIndex < 0)
      argIndex = 0;

    string argToInsert = args[argIndex];

    // Insert at cursor position
    LastYankArgStart = CursorPosition;
    LastYankArgLength = argToInsert.Length;
    UserInput = UserInput[..CursorPosition] + argToInsert + UserInput[CursorPosition..];
    CursorPosition += argToInsert.Length;

    // Clear state (YankNthArg doesn't cycle like YankLastArg)
    LastCommandWasYankArg = false;
    DigitArgument = null;

    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// Handle digit argument prefix for YankNthArg.
  /// Alt+0 through Alt+9 set the digit argument.
  /// </summary>
  internal void HandleDigitArgument(int digit)
  {
    if (DigitArgument.HasValue)
    {
      // Already have a digit - append to form multi-digit number (e.g., Alt+1 Alt+2 = 12)
      DigitArgument = DigitArgument.Value * 10 + digit;
    }
    else
    {
      DigitArgument = digit;
    }
  }

  /// <summary>
  /// Resets the yank argument tracking. Called by non-yank-arg commands.
  /// </summary>
  private void ResetYankArgTracking()
  {
    LastCommandWasYankArg = false;
    // Note: Don't reset DigitArgument here - it should persist until used
  }

  /// <summary>
  /// Parse a command line string into individual arguments.
  /// Handles quoted strings and escaped characters.
  /// </summary>
  /// <param name="commandLine">The command line to parse.</param>
  /// <returns>Array of arguments.</returns>
  /// <remarks>
  /// Parsing rules:
  /// - Whitespace separates arguments
  /// - Double quotes preserve whitespace: "arg with spaces" → "arg with spaces"
  /// - Single quotes preserve whitespace: 'arg with spaces' → "arg with spaces"
  /// - Backslash escapes the next character: arg\ with\ spaces → "arg with spaces"
  /// - Quotes can be escaped: "say \"hello\"" → say "hello"
  /// </remarks>
  internal static string[] ParseHistoryArguments(string commandLine)
  {
    if (string.IsNullOrWhiteSpace(commandLine))
      return [];

    List<string> args = [];
    int i = 0;
    int length = commandLine.Length;

    while (i < length)
    {
      // Skip leading whitespace
      while (i < length && char.IsWhiteSpace(commandLine[i]))
        i++;

      if (i >= length)
        break;

      // Parse an argument
      System.Text.StringBuilder argBuilder = new();
      char quoteChar = '\0';
      bool inQuotes = false;

      while (i < length)
      {
        char c = commandLine[i];

        if (inQuotes)
        {
          if (c == '\\' && i + 1 < length)
          {
            // Escape sequence inside quotes
            char next = commandLine[i + 1];
            if (next == quoteChar || next == '\\')
            {
              argBuilder.Append(next);
              i += 2;
              continue;
            }
          }

          if (c == quoteChar)
          {
            // End of quoted section
            inQuotes = false;
            i++;
            continue;
          }

          argBuilder.Append(c);
          i++;
        }
        else
        {
          if (c == '\\' && i + 1 < length)
          {
            // Escape sequence outside quotes
            argBuilder.Append(commandLine[i + 1]);
            i += 2;
            continue;
          }

          if (c == '"' || c == '\'')
          {
            // Start of quoted section
            quoteChar = c;
            inQuotes = true;
            i++;
            continue;
          }

          if (char.IsWhiteSpace(c))
          {
            // End of argument
            break;
          }

          argBuilder.Append(c);
          i++;
        }
      }

      if (argBuilder.Length > 0)
      {
        args.Add(argBuilder.ToString());
      }
    }

    return [.. args];
  }
}
