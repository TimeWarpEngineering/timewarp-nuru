namespace TimeWarp.Nuru;

/// <summary>
/// Basic editing enhancement handlers for the REPL console reader.
/// </summary>
/// <remarks>
/// Implements PSReadLine-compatible basic editing:
/// <list type="bullet">
/// <item>DeleteCharOrExit (Ctrl+D dual behavior)</item>
/// <item>ClearScreen (Ctrl+L)</item>
/// <item>Insert/Overwrite toggle</item>
/// </list>
/// </remarks>
public sealed partial class ReplConsoleReader
{
  // Overwrite mode flag
  private bool IsOverwriteMode;

  /// <summary>
  /// PSReadLine: DeleteCharOrExit - Delete character at cursor, or exit REPL if line is empty.
  /// </summary>
  /// <remarks>
  /// This is the Unix/bash-style Ctrl+D behavior:
  /// - If there's text and cursor is not at end: delete character at cursor
  /// - If line is empty: signal EOF (exit REPL)
  /// </remarks>
  internal void HandleDeleteCharOrExit()
  {
    // If line is empty, signal exit
    if (string.IsNullOrEmpty(UserInput))
    {
      ShouldExitRepl = true;
      return;
    }

    // If there's a selection, delete it
    if (SelectionState.IsActive)
    {
      HandleDeleteSelection();
      return;
    }

    // Otherwise delete character at cursor (if any)
    if (CursorPosition < UserInput.Length)
    {
      SaveUndoState(isCharacterInput: false);
      UserInput = UserInput[..CursorPosition] + UserInput[(CursorPosition + 1)..];
      CompletionHandler.Reset();
      ResetKillTracking();
      RedrawLine();
    }
  }

  /// <summary>
  /// PSReadLine: ClearScreen - Clear the terminal screen and redraw the prompt.
  /// </summary>
  internal void HandleClearScreen()
  {
    // Clear screen using ANSI escape codes: clear screen + cursor home
    Terminal.Write("\u001b[2J\u001b[H");

    // Redraw prompt and current input
    Terminal.Write(PromptFormatter.Format(ReplOptions));

    if (ReplOptions.EnableColors)
    {
      Terminal.Write(SyntaxHighlighter.Highlight(UserInput));
    }
    else
    {
      Terminal.Write(UserInput);
    }

    // Position cursor correctly
    UpdateCursorPosition();
  }

  /// <summary>
  /// PSReadLine: ToggleInsertMode - Toggle between insert and overwrite modes.
  /// </summary>
  internal void HandleToggleInsertMode()
  {
    IsOverwriteMode = !IsOverwriteMode;

    // Visual indicator: could change cursor shape, but that's terminal-dependent
    // For now, just toggle the state - the HandleCharacter method will use it
  }

  /// <summary>
  /// Handles character insertion with overwrite mode support.
  /// </summary>
  /// <param name="charToInsert">The character to insert or overwrite.</param>
  internal void HandleCharacterWithOverwrite(char charToInsert)
  {
    SaveUndoState(isCharacterInput: true);

    // If there's a selection, replace it regardless of mode
    if (SelectionState.IsActive)
    {
      int start = SelectionState.Start;
      int end = SelectionState.End;
      UserInput = UserInput[..start] + charToInsert + UserInput[end..];
      CursorPosition = start + 1;
      SelectionState.Clear();
    }
    else if (IsOverwriteMode && CursorPosition < UserInput.Length)
    {
      // Overwrite mode: replace character at cursor
      UserInput = UserInput[..CursorPosition] + charToInsert + UserInput[(CursorPosition + 1)..];
      CursorPosition++;
    }
    else
    {
      // Insert mode (default): insert at cursor
      UserInput = UserInput[..CursorPosition] + charToInsert + UserInput[CursorPosition..];
      CursorPosition++;
    }

    PrefixSearchString = null;
    CompletionHandler.Reset();
    ResetKillTracking();
    RedrawLine();
  }
}
