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

      SaveUndoState(isCharacterInput: false);  // Save state before edit
      UserInput = UserInput[..(CursorPosition - 1)] + UserInput[CursorPosition..];
      CursorPosition--;
      CompletionHandler.Reset();  // Clear completion cycling when user deletes
      ResetKillTracking();        // Backspace is not a kill command

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

      SaveUndoState(isCharacterInput: false);  // Save state before edit
      UserInput = UserInput[..CursorPosition] + UserInput[(CursorPosition + 1)..];
      CompletionHandler.Reset();  // Clear completion cycling when user deletes
      ResetKillTracking();        // Delete is not a kill command

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

    SaveUndoState(isCharacterInput: false);  // Save state before clearing

    // Clear the entire input line
    UserInput = string.Empty;
    CursorPosition = 0;

    // Clear any prefix search state
    PrefixSearchString = null;

    // Clear kill tracking
    ResetKillTracking();

    // Redraw the empty line
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: KillLine - Delete from the cursor position to the end of the line.
  /// The deleted text is stored in the kill ring for later yanking.
  /// </summary>
  internal void HandleKillLine()
  {
    // Delegate to the kill ring implementation
    HandleKillLineToRing();
  }

  /// <summary>
  /// PSReadLine: BackwardKillWord - Delete the word before the cursor.
  /// Deletes from cursor back to the start of the current or previous word.
  /// The deleted text is stored in the kill ring for later yanking.
  /// </summary>
  internal void HandleDeleteWordBackward()
  {
    // Delegate to the kill ring implementation (BackwardKillWord uses word boundaries)
    HandleBackwardKillWord();
  }

  /// <summary>
  /// PSReadLine: BackwardKillLine - Delete from the beginning of the line to the cursor.
  /// The deleted text is stored in the kill ring for later yanking.
  /// </summary>
  internal void HandleDeleteToLineStart()
  {
    // Delegate to the kill ring implementation
    HandleBackwardKillInput();
  }
}
