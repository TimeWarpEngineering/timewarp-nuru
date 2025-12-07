namespace TimeWarp.Nuru;

/// <summary>
/// Represents a single undo unit containing the text state and cursor position.
/// </summary>
/// <param name="Text">The text content at this state.</param>
/// <param name="CursorPosition">The cursor position at this state.</param>
internal readonly record struct UndoUnit(string Text, int CursorPosition);

/// <summary>
/// Manages undo and redo stacks for the REPL console reader.
/// </summary>
/// <remarks>
/// Implements PSReadLine-compatible undo/redo behavior:
/// - Consecutive character insertions are grouped into a single undo unit
/// - Space, punctuation, or commands start a new undo group
/// - Redo stack is cleared when a new edit is made
/// </remarks>
internal sealed class UndoStack
{
  private readonly Stack<UndoUnit> UndoHistory = new();
  private readonly Stack<UndoUnit> RedoHistory = new();
  private readonly int MaxCapacity;
  private bool IsGroupingCharacters;
  private string InitialText = string.Empty;
  private int InitialCursorPosition;

  /// <summary>
  /// Creates a new undo stack with the specified maximum capacity.
  /// </summary>
  /// <param name="maxCapacity">Maximum number of undo units to retain (default 100).</param>
  public UndoStack(int maxCapacity = 100)
  {
    MaxCapacity = maxCapacity;
  }

  /// <summary>
  /// Gets the number of items in the undo stack.
  /// </summary>
  public int UndoCount => UndoHistory.Count;

  /// <summary>
  /// Gets the number of items in the redo stack.
  /// </summary>
  public int RedoCount => RedoHistory.Count;

  /// <summary>
  /// Gets whether there are any items to undo.
  /// </summary>
  public bool CanUndo => UndoHistory.Count > 0;

  /// <summary>
  /// Gets whether there are any items to redo.
  /// </summary>
  public bool CanRedo => RedoHistory.Count > 0;

  /// <summary>
  /// Sets the initial state for the current line (used by RevertLine).
  /// </summary>
  /// <param name="text">The initial text.</param>
  /// <param name="cursorPosition">The initial cursor position.</param>
  public void SetInitialState(string text, int cursorPosition)
  {
    InitialText = text;
    InitialCursorPosition = cursorPosition;
  }

  /// <summary>
  /// Gets the initial state for RevertLine.
  /// </summary>
  /// <returns>The initial undo unit.</returns>
  public UndoUnit GetInitialState() => new(InitialText, InitialCursorPosition);

  /// <summary>
  /// Saves the current state before an edit operation.
  /// </summary>
  /// <param name="text">The current text before the edit.</param>
  /// <param name="cursorPosition">The current cursor position before the edit.</param>
  /// <param name="isCharacterInput">Whether this is a character insertion (for grouping).</param>
  public void SaveState(string text, int cursorPosition, bool isCharacterInput = false)
  {
    // If this is character input and we're grouping, skip saving
    // (we already saved the state at the start of the group)
    if (isCharacterInput && IsGroupingCharacters)
    {
      return;
    }

    // If this is the start of character grouping, mark it
    if (isCharacterInput && !IsGroupingCharacters)
    {
      IsGroupingCharacters = true;
    }
    else
    {
      // Non-character input ends grouping
      IsGroupingCharacters = false;
    }

    // Push to undo stack
    UndoHistory.Push(new UndoUnit(text, cursorPosition));

    // Clear redo stack (new edit invalidates redo history)
    RedoHistory.Clear();

    // Trim if over capacity
    TrimToCapacity();
  }

  /// <summary>
  /// Ends the current character grouping without creating a new undo unit.
  /// Called when movement commands are executed.
  /// </summary>
  public void EndCharacterGrouping()
  {
    IsGroupingCharacters = false;
  }

  /// <summary>
  /// Performs an undo operation.
  /// </summary>
  /// <param name="currentText">The current text before undo.</param>
  /// <param name="currentCursorPosition">The current cursor position before undo.</param>
  /// <returns>The restored undo unit, or null if nothing to undo.</returns>
  public UndoUnit? Undo(string currentText, int currentCursorPosition)
  {
    if (!CanUndo)
      return null;

    // End any active grouping
    IsGroupingCharacters = false;

    // Save current state to redo stack
    RedoHistory.Push(new UndoUnit(currentText, currentCursorPosition));

    // Pop and return from undo stack
    return UndoHistory.Pop();
  }

  /// <summary>
  /// Performs a redo operation.
  /// </summary>
  /// <param name="currentText">The current text before redo.</param>
  /// <param name="currentCursorPosition">The current cursor position before redo.</param>
  /// <returns>The restored redo unit, or null if nothing to redo.</returns>
  public UndoUnit? Redo(string currentText, int currentCursorPosition)
  {
    if (!CanRedo)
      return null;

    // Save current state to undo stack
    UndoHistory.Push(new UndoUnit(currentText, currentCursorPosition));

    // Pop and return from redo stack
    return RedoHistory.Pop();
  }

  /// <summary>
  /// Clears both undo and redo stacks.
  /// </summary>
  public void Clear()
  {
    UndoHistory.Clear();
    RedoHistory.Clear();
    IsGroupingCharacters = false;
    InitialText = string.Empty;
    InitialCursorPosition = 0;
  }

  private void TrimToCapacity()
  {
    // If over capacity, remove oldest items
    if (UndoHistory.Count > MaxCapacity)
    {
      // Convert to list, remove oldest, convert back
      List<UndoUnit> items = [.. UndoHistory];
      items.Reverse();
      while (items.Count > MaxCapacity)
      {
        items.RemoveAt(0);
      }

      UndoHistory.Clear();
      items.Reverse();
      foreach (UndoUnit item in items)
      {
        UndoHistory.Push(item);
      }
    }
  }
}
