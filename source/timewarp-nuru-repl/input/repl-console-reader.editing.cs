namespace TimeWarp.Nuru;

/// <summary>
/// Text editing handlers for the REPL console reader.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: BackwardDeleteChar - Delete the character before the cursor.
  /// </summary>
  internal void HandleBackwardDeleteChar()
  {
    if (CursorPosition > 0)
    {
      ReplLoggerMessages.BackspacePressed(Logger, CursorPosition, null);

      UserInput = UserInput[..(CursorPosition - 1)] + UserInput[CursorPosition..];
      CursorPosition--;
      CompletionHandler.Reset();  // Clear completion cycling when user deletes

      ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: DeleteChar - Delete the character under the cursor.
  /// </summary>
  internal void HandleDeleteChar()
  {
    if (CursorPosition < UserInput.Length)
    {
      ReplLoggerMessages.DeletePressed(Logger, CursorPosition, null);

      UserInput = UserInput[..CursorPosition] + UserInput[(CursorPosition + 1)..];
      CompletionHandler.Reset();  // Clear completion cycling when user deletes

      ReplLoggerMessages.UserInputChanged(Logger, UserInput, CursorPosition, null);
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: RevertLine - Clear the entire input line (like Escape in PowerShell).
  /// Clears all user input and resets cursor to the beginning.
  /// </summary>
  internal void HandleEscape()
  {
    // Clear completion state
    CompletionHandler.Reset();

    // Clear the entire input line
    UserInput = string.Empty;
    CursorPosition = 0;

    // Clear any prefix search state
    PrefixSearchString = null;

    // Redraw the empty line
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: KillLine - Delete from the cursor position to the end of the line.
  /// The deleted text is removed (kill ring not implemented).
  /// </summary>
  internal void HandleKillLine()
  {
    if (CursorPosition < UserInput.Length)
    {
      // Delete everything from cursor to end of line
      UserInput = UserInput[..CursorPosition];
      CompletionHandler.Reset();
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: BackwardKillWord - Delete the word before the cursor.
  /// Deletes from cursor back to the start of the current or previous word.
  /// </summary>
  internal void HandleDeleteWordBackward()
  {
    if (CursorPosition > 0)
    {
      int newPos = CursorPosition;

      // Skip whitespace behind cursor
      while (newPos > 0 && char.IsWhiteSpace(UserInput[newPos - 1]))
        newPos--;

      // Skip word characters to find start of word
      while (newPos > 0 && !char.IsWhiteSpace(UserInput[newPos - 1]))
        newPos--;

      // Delete from newPos to CursorPosition
      UserInput = UserInput[..newPos] + UserInput[CursorPosition..];
      CursorPosition = newPos;
      CompletionHandler.Reset();
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: BackwardKillLine - Delete from the beginning of the line to the cursor.
  /// The deleted text is removed (kill ring not implemented).
  /// </summary>
  internal void HandleDeleteToLineStart()
  {
    if (CursorPosition > 0)
    {
      // Delete everything from start of line to cursor
      UserInput = UserInput[CursorPosition..];
      CursorPosition = 0;
      CompletionHandler.Reset();
      RedrawLine();
    }
  }
}
