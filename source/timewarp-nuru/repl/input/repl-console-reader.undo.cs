#region Purpose
// ReplConsoleReader partial for undo and redo handlers.
#endregion

#region Design
// Undo state is snapshotted before each edit; consecutive character inserts are grouped into one
// undo unit, and movement commands end the group via EndUndoCharacterGrouping. RevertLine
// restores the initial state captured at ReadLineAsync start but saves the current state first,
// so reverting is itself undoable.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Undo/redo handlers for the REPL console reader.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: Undo - Undo the last edit operation.
  /// </summary>
  internal Task HandleUndoAsync()
  {
    UndoUnit? undoUnit = UndoManager.Undo(UserInput, CursorPosition);
    if (undoUnit.HasValue)
    {
      ClearSelection();
      UserInput = undoUnit.Value.Text;
      CursorPosition = undoUnit.Value.CursorPosition;
      RedrawLine();
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: Redo - Redo an undone edit operation.
  /// </summary>
  internal Task HandleRedoAsync()
  {
    UndoUnit? redoUnit = UndoManager.Redo(UserInput, CursorPosition);
    if (redoUnit.HasValue)
    {
      ClearSelection();
      UserInput = redoUnit.Value.Text;
      CursorPosition = redoUnit.Value.CursorPosition;
      RedrawLine();
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// PSReadLine: RevertLine - Undo ALL changes to the current line, restoring it to its initial state.
  /// </summary>
  internal Task HandleRevertLineAsync()
  {
    UndoUnit initialState = UndoManager.GetInitialState();

    // Save current state for potential undo
    UndoManager.SaveState(UserInput, CursorPosition, isCharacterInput: false);

    ClearSelection();
    UserInput = initialState.Text;
    CursorPosition = initialState.CursorPosition;
    RedrawLine();
    return Task.CompletedTask;
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
