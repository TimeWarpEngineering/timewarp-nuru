namespace TimeWarp.Nuru;

/// <summary>
/// History navigation handlers for the REPL console reader.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: PreviousHistory - Replace the input with the previous item in the history.
  /// </summary>
  internal Task HandlePreviousHistoryAsync()
  {
    if (HistoryIndex > 0)
    {
      HistoryIndex--;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      PrefixSearchString = null;  // Clear prefix search when using normal history nav
      RedrawLine();
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: NextHistory - Replace the input with the next item in the history.
  /// </summary>
  internal Task HandleNextHistoryAsync()
  {
    if (HistoryIndex < History.Count - 1)
    {
      HistoryIndex++;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      PrefixSearchString = null;  // Clear prefix search when using normal history nav
      RedrawLine();
    }
    else if (HistoryIndex == History.Count - 1)
    {
      HistoryIndex = History.Count;
      UserInput = string.Empty;
      CursorPosition = 0;
      PrefixSearchString = null;  // Clear prefix search
      RedrawLine();
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: BeginningOfHistory - Move to the first item in the history.
  /// </summary>
  internal Task HandleBeginningOfHistoryAsync()
  {
    if (History.Count > 0)
    {
      HistoryIndex = 0;
      UserInput = History[HistoryIndex];
      CursorPosition = UserInput.Length;
      PrefixSearchString = null;  // Clear prefix search
      RedrawLine();
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: EndOfHistory - Move to the last item (current input) in the history.
  /// </summary>
  internal Task HandleEndOfHistoryAsync()
  {
    HistoryIndex = History.Count;
    UserInput = string.Empty;
    CursorPosition = 0;
    PrefixSearchString = null;  // Clear prefix search
    RedrawLine();
    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: HistorySearchBackward - Search backward through history for entries starting with current input prefix.
  /// </summary>
  internal Task HandleHistorySearchBackwardAsync()
  {
    // If no prefix search active, use current input as the prefix
    if (PrefixSearchString is null)
    {
      PrefixSearchString = UserInput;
    }

    // Search backward from current position
    int searchIndex = HistoryIndex - 1;
    while (searchIndex >= 0)
    {
      if (History[searchIndex].StartsWith(PrefixSearchString, StringComparison.OrdinalIgnoreCase))
      {
        HistoryIndex = searchIndex;
        UserInput = History[HistoryIndex];
        CursorPosition = UserInput.Length;
        RedrawLine();
        return Task.CompletedTask;
      }

      searchIndex--;
    }

    // No match found - do nothing (keep current state)
    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: HistorySearchForward - Search forward through history for entries starting with current input prefix.
  /// </summary>
  internal Task HandleHistorySearchForwardAsync()
  {
    // If no prefix search active, use current input as the prefix
    if (PrefixSearchString is null)
    {
      PrefixSearchString = UserInput;
    }

    // Search forward from current position
    int searchIndex = HistoryIndex + 1;
    while (searchIndex < History.Count)
    {
      if (History[searchIndex].StartsWith(PrefixSearchString, StringComparison.OrdinalIgnoreCase))
      {
        HistoryIndex = searchIndex;
        UserInput = History[HistoryIndex];
        CursorPosition = UserInput.Length;
        RedrawLine();
        return Task.CompletedTask;
      }

      searchIndex++;
    }

    // No match found - do nothing (keep current state)
    return Task.CompletedTask;
  }
}
