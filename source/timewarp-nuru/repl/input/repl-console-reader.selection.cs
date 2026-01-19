namespace TimeWarp.Nuru;

/// <summary>
/// Text selection handlers for the REPL console reader.
/// </summary>
/// <remarks>
/// Implements PSReadLine-compatible text selection:
/// <list type="bullet">
/// <item>Shift+Arrow: Character/word selection</item>
/// <item>Shift+Home/End: Line selection</item>
/// <item>Ctrl+Shift+A: Select all</item>
/// <item>Copy/Cut/Delete with selection</item>
/// </list>
/// Clipboard operations are in repl-console-reader.clipboard.cs.
/// </remarks>
public sealed partial class ReplConsoleReader
{
  // ============================================================================
  // Character Selection
  // ============================================================================

  /// <summary>
  /// PSReadLine: SelectBackwardChar - Extend selection one character backward.
  /// </summary>
  internal void HandleSelectBackwardChar()
  {
    if (CursorPosition > 0)
    {
      StartOrExtendSelection();
      CursorPosition--;
      SelectionState.ExtendTo(CursorPosition);
      RedrawLineWithSelection();
    }
  }

  /// <summary>
  /// PSReadLine: SelectForwardChar - Extend selection one character forward.
  /// </summary>
  internal void HandleSelectForwardChar()
  {
    if (CursorPosition < UserInput.Length)
    {
      StartOrExtendSelection();
      CursorPosition++;
      SelectionState.ExtendTo(CursorPosition);
      RedrawLineWithSelection();
    }
  }

  // ============================================================================
  // Word Selection
  // ============================================================================

  /// <summary>
  /// PSReadLine: SelectBackwardWord - Extend selection to beginning of previous word.
  /// </summary>
  internal void HandleSelectBackwardWord()
  {
    StartOrExtendSelection();

    int newPos = CursorPosition;

    // Skip whitespace behind cursor
    while (newPos > 0 && char.IsWhiteSpace(UserInput[newPos - 1]))
      newPos--;

    // Skip word characters to find start of word
    while (newPos > 0 && !char.IsWhiteSpace(UserInput[newPos - 1]))
      newPos--;

    CursorPosition = newPos;
    SelectionState.ExtendTo(CursorPosition);
    RedrawLineWithSelection();
  }

  /// <summary>
  /// PSReadLine: SelectNextWord - Extend selection to end of next word.
  /// </summary>
  internal void HandleSelectNextWord()
  {
    StartOrExtendSelection();

    int newPos = CursorPosition;

    // Skip whitespace ahead of cursor
    while (newPos < UserInput.Length && char.IsWhiteSpace(UserInput[newPos]))
      newPos++;

    // Move to end of word
    while (newPos < UserInput.Length && !char.IsWhiteSpace(UserInput[newPos]))
      newPos++;

    CursorPosition = newPos;
    SelectionState.ExtendTo(CursorPosition);
    RedrawLineWithSelection();
  }

  // ============================================================================
  // Line Selection
  // ============================================================================

  /// <summary>
  /// PSReadLine: SelectBackwardsLine - Extend selection to beginning of line.
  /// </summary>
  internal void HandleSelectBackwardsLine()
  {
    StartOrExtendSelection();
    CursorPosition = 0;
    SelectionState.ExtendTo(CursorPosition);
    RedrawLineWithSelection();
  }

  /// <summary>
  /// PSReadLine: SelectLine - Extend selection to end of line.
  /// </summary>
  internal void HandleSelectLine()
  {
    StartOrExtendSelection();
    CursorPosition = UserInput.Length;
    SelectionState.ExtendTo(CursorPosition);
    RedrawLineWithSelection();
  }

  /// <summary>
  /// PSReadLine: SelectAll - Select entire input.
  /// </summary>
  internal void HandleSelectAll()
  {
    SelectionState.StartAt(0);
    CursorPosition = UserInput.Length;
    SelectionState.ExtendTo(CursorPosition);
    RedrawLineWithSelection();
  }

  // ============================================================================
  // Selection Actions
  // ============================================================================

  /// <summary>
  /// PSReadLine: CopyOrCancelLine - Copy selection to clipboard, or cancel line if no selection.
  /// </summary>
  internal void HandleCopyOrCancelLine()
  {
    if (SelectionState.IsActive)
    {
      // Copy selection to clipboard
      string selectedText = SelectionState.GetSelectedText(UserInput);
      if (!string.IsNullOrEmpty(selectedText))
      {
        SetClipboardText(selectedText);
      }

      ClearSelection();
      RedrawLine();
    }
    else
    {
      // No selection - cancel line (like Escape)
      HandleEscape();
    }
  }

  /// <summary>
  /// PSReadLine: Cut - Cut selection to clipboard and kill ring.
  /// </summary>
  internal void HandleCut()
  {
    if (!SelectionState.IsActive)
      return;

    string selectedText = SelectionState.GetSelectedText(UserInput);
    if (string.IsNullOrEmpty(selectedText))
      return;

    // Save to clipboard and kill ring
    SetClipboardText(selectedText);
    KillRing.Add(selectedText);

    // Save undo state before deletion
    SaveUndoState(isCharacterInput: false);

    // Delete the selected text
    int start = SelectionState.Start;
    int end = SelectionState.End;
    UserInput = UserInput[..start] + UserInput[end..];
    CursorPosition = start;

    ClearSelection();
    ResetKillTracking();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: Paste - Paste from system clipboard.
  /// Falls back to the kill ring when system clipboard is unavailable.
  /// </summary>
  internal void HandlePaste()
  {
    string? clipboardText = GetClipboardText();

    // Fall back to kill ring when clipboard is unavailable
    if (string.IsNullOrEmpty(clipboardText))
    {
      clipboardText = KillRing.Yank();
    }

    if (string.IsNullOrEmpty(clipboardText))
      return;

    SaveUndoState(isCharacterInput: false);

    // If there's a selection, replace it
    if (SelectionState.IsActive)
    {
      int start = SelectionState.Start;
      int end = SelectionState.End;
      UserInput = UserInput[..start] + clipboardText + UserInput[end..];
      CursorPosition = start + clipboardText.Length;
      ClearSelection();
    }
    else
    {
      // Insert at cursor position
      UserInput = UserInput[..CursorPosition] + clipboardText + UserInput[CursorPosition..];
      CursorPosition += clipboardText.Length;
    }

    ResetKillTracking();
    RedrawLine();
  }

  /// <summary>
  /// Delete selected text (called when Delete or Backspace pressed with selection).
  /// </summary>
  internal void HandleDeleteSelection()
  {
    if (!SelectionState.IsActive)
      return;

    SaveUndoState(isCharacterInput: false);

    int start = SelectionState.Start;
    int end = SelectionState.End;
    UserInput = UserInput[..start] + UserInput[end..];
    CursorPosition = start;

    ClearSelection();
    ResetKillTracking();
    RedrawLine();
  }

  // ============================================================================
  // Helper Methods
  // ============================================================================

  /// <summary>
  /// Starts a new selection if one isn't active, or continues extending the current one.
  /// </summary>
  private void StartOrExtendSelection()
  {
    if (!SelectionState.IsActive && SelectionState.Anchor < 0)
    {
      SelectionState.StartAt(CursorPosition);
    }
  }

  /// <summary>
  /// Clears the current selection.
  /// </summary>
  private void ClearSelection()
  {
    SelectionState.Clear();
  }

  /// <summary>
  /// Clears selection if cursor moves without Shift held.
  /// Should be called from non-selection movement commands.
  /// </summary>
  private void ClearSelectionOnMovement()
  {
    if (SelectionState.IsActive)
    {
      ClearSelection();
      RedrawLine();  // Redraw to remove selection highlighting
    }
  }

  /// <summary>
  /// Redraws the line with selection highlighting.
  /// </summary>
  private void RedrawLineWithSelection()
  {
    // Move cursor to beginning of line
    (int _, int top) = Terminal.GetCursorPosition();
    Terminal.SetCursorPosition(0, top);

    // Clear line
    Terminal.Write(new string(' ', Terminal.WindowWidth));

    // Move back to beginning
    Terminal.SetCursorPosition(0, top);

    // Redraw the prompt
    Terminal.Write(PromptFormatter.Format(ReplOptions));

    // Render text with selection highlighting
    if (SelectionState.IsActive && SelectionState.IsValidFor(UserInput.Length))
    {
      int start = Math.Min(SelectionState.Start, UserInput.Length);
      int end = Math.Min(SelectionState.End, UserInput.Length);

      // Text before selection
      if (start > 0)
      {
        string before = UserInput[..start];
        if (ReplOptions.EnableColors)
          Terminal.Write(SyntaxHighlighter.Highlight(before));
        else
          Terminal.Write(before);
      }

      // Selected text with inverse colors
      if (end > start)
      {
        string selected = UserInput[start..end];
        Terminal.Write($"\u001b[7m{selected}\u001b[0m");  // ANSI inverse
      }

      // Text after selection
      if (end < UserInput.Length)
      {
        string after = UserInput[end..];
        if (ReplOptions.EnableColors)
          Terminal.Write(SyntaxHighlighter.Highlight(after));
        else
          Terminal.Write(after);
      }
    }
    else
    {
      // No selection, render normally
      if (ReplOptions.EnableColors)
      {
        Terminal.Write(SyntaxHighlighter.Highlight(UserInput));
      }
      else
      {
        Terminal.Write(UserInput);
      }
    }

    // Update cursor position
    UpdateCursorPosition();
  }
}
