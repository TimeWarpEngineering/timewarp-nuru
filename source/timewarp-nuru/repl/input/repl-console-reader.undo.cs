namespace TimeWarp.Nuru;

/// <summary>
/// Undo/redo handlers for the REPL console reader.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: Undo - Undo the last edit operation.
  /// </summary>
  internal void HandleUndo()
  {
    UndoUnit? undoUnit = UndoManager.Undo(UserInput, CursorPosition);
    if (undoUnit.HasValue)
    {
      UserInput = undoUnit.Value.Text;
      CursorPosition = undoUnit.Value.CursorPosition;
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: Redo - Redo an undone edit operation.
  /// </summary>
  internal void HandleRedo()
  {
    UndoUnit? redoUnit = UndoManager.Redo(UserInput, CursorPosition);
    if (redoUnit.HasValue)
    {
      UserInput = redoUnit.Value.Text;
      CursorPosition = redoUnit.Value.CursorPosition;
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: RevertLine - Undo ALL changes to the current line, restoring it to its initial state.
  /// </summary>
  internal void HandleRevertLine()
  {
    UndoUnit initialState = UndoManager.GetInitialState();

    // Save current state for potential undo
    UndoManager.SaveState(UserInput, CursorPosition, isCharacterInput: false);

    UserInput = initialState.Text;
    CursorPosition = initialState.CursorPosition;
    RedrawLine();
  }

  /// <summary>
  /// Saves the current state to the undo stack before an edit.
  /// </summary>
  /// <param name="isCharacterInput">Whether this is a character insertion (for grouping).</param>
  private void SaveUndoState(bool isCharacterInput = false)
  {
    UndoManager.SaveState(UserInput, CursorPosition, isCharacterInput);
  }

  /// <summary>
  /// Ends character grouping for undo (called by movement commands).
  /// </summary>
  private void EndUndoCharacterGrouping()
  {
    UndoManager.EndCharacterGrouping();
  }
}
