namespace TimeWarp.Nuru;

/// <summary>
/// Cursor movement handlers for the REPL console reader.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: BackwardChar - Move the cursor back one character.
  /// </summary>
  internal Task HandleBackwardCharAsync()
  {
    EndUndoCharacterGrouping();  // Movement ends character grouping
    ClearSelectionOnMovement();  // Clear selection on non-shift movement

    if (CursorPosition > 0)
      CursorPosition--;

    UpdateCursorPosition();
    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: ForwardChar - Move the cursor forward one character.
  /// </summary>
  internal Task HandleForwardCharAsync()
  {
    EndUndoCharacterGrouping();  // Movement ends character grouping
    ClearSelectionOnMovement();  // Clear selection on non-shift movement

    if (CursorPosition < UserInput.Length)
      CursorPosition++;

    UpdateCursorPosition();
    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: BackwardWord - Move the cursor to the beginning of the current or previous word.
  /// </summary>
  internal Task HandleBackwardWordAsync()
  {
    EndUndoCharacterGrouping();  // Movement ends character grouping
    ClearSelectionOnMovement();  // Clear selection on non-shift movement

    int newPos = CursorPosition;

    // Skip whitespace behind cursor
    while (newPos > 0 && char.IsWhiteSpace(UserInput[newPos - 1]))
      newPos--;

    // Skip word characters to find start of word
    while (newPos > 0 && !char.IsWhiteSpace(UserInput[newPos - 1]))
      newPos--;

    CursorPosition = newPos;

    UpdateCursorPosition();
    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: ForwardWord - Move the cursor to the end of the current or next word.
  /// Note: PSReadLine moves to END of word, not start of next word.
  /// </summary>
  internal Task HandleForwardWordAsync()
  {
    EndUndoCharacterGrouping();  // Movement ends character grouping
    ClearSelectionOnMovement();  // Clear selection on non-shift movement

    int newPos = CursorPosition;

    // Skip whitespace ahead of cursor
    while (newPos < UserInput.Length && char.IsWhiteSpace(UserInput[newPos]))
      newPos++;

    // Move to end of word
    while (newPos < UserInput.Length && !char.IsWhiteSpace(UserInput[newPos]))
      newPos++;

    CursorPosition = newPos;

    UpdateCursorPosition();
    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: BeginningOfLine - Move the cursor to the beginning of the line.
  /// </summary>
  internal Task HandleBeginningOfLineAsync()
  {
    EndUndoCharacterGrouping();  // Movement ends character grouping
    ClearSelectionOnMovement();  // Clear selection on non-shift movement
    CursorPosition = 0;
    UpdateCursorPosition();
    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: EndOfLine - Move the cursor to the end of the line.
  /// </summary>
  internal Task HandleEndOfLineAsync()
  {
    EndUndoCharacterGrouping();  // Movement ends character grouping
    ClearSelectionOnMovement();  // Clear selection on non-shift movement
    CursorPosition = UserInput.Length;
    UpdateCursorPosition();
    return Task.CompletedTask;
  }
}
