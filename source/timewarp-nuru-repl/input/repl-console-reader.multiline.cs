namespace TimeWarp.Nuru;

/// <summary>
/// Multiline editing handlers for the REPL console reader.
/// </summary>
/// <remarks>
/// Implements PSReadLine-compatible multiline editing:
/// <list type="bullet">
/// <item>AddLine (Shift+Enter) - Add a new line without executing</item>
/// <item>Multiline rendering with continuation prompt</item>
/// <item>Execution of full multiline input with Enter</item>
/// </list>
/// </remarks>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// The multiline buffer for managing multiline input.
  /// </summary>
  private readonly MultilineBuffer MultilineInput = new();

  /// <summary>
  /// Whether we're in multiline mode (have more than one line).
  /// </summary>
  private bool IsMultilineMode => MultilineInput.IsMultiline;

  /// <summary>
  /// The continuation prompt displayed for subsequent lines in multiline mode.
  /// </summary>
  private string ContinuationPrompt => ReplOptions.ContinuationPrompt ?? ">> ";

  /// <summary>
  /// PSReadLine: AddLine - Add a new line without executing (Shift+Enter).
  /// </summary>
  /// <remarks>
  /// Splits the current line at the cursor position, moving text after the cursor
  /// to a new line. The cursor moves to the start of the new line.
  /// </remarks>
  internal void HandleAddLine()
  {
    // Save undo state before the edit
    SaveUndoState(isCharacterInput: false);

    // Sync current UserInput to the multiline buffer first
    SyncToMultilineBuffer();

    // Add the new line to the multiline buffer
    MultilineInput.AddLine();

    // Sync UserInput with the multiline buffer (for compatibility with existing code)
    SyncFromMultilineBuffer();

    // Reset state
    PrefixSearchString = null;
    CompletionHandler.Reset();
    ResetKillTracking();

    // Redraw with multiline support
    RedrawMultiline();
  }

  /// <summary>
  /// Synchronizes UserInput and CursorPosition from the MultilineBuffer.
  /// </summary>
  private void SyncFromMultilineBuffer()
  {
    // For compatibility with existing single-line operations, we maintain UserInput
    // as a space-separated version of the multiline content
    // But for multiline mode, we use the actual buffer
    UserInput = MultilineInput.GetFullText(Environment.NewLine);
    CursorPosition = MultilineInput.CursorToPosition(MultilineInput.Cursor);
  }

  /// <summary>
  /// Synchronizes the MultilineBuffer from UserInput and CursorPosition.
  /// </summary>
  private void SyncToMultilineBuffer()
  {
    MultilineInput.SetText(UserInput);
    if (UserInput.Length > 0)
    {
      MultilineCursor cursor = MultilineInput.PositionToCursor(CursorPosition);
      MultilineInput.SetCursor(cursor.Line, cursor.Column);
    }
  }

  /// <summary>
  /// Redraws the input for multiline mode.
  /// </summary>
  private void RedrawMultiline()
  {
    ReplLoggerMessages.LineRedrawn(Logger, UserInput, null);

    int lineCount = MultilineInput.LineCount;

    // Move cursor to beginning of first line of our input
    (int _, int currentTop) = Terminal.GetCursorPosition();

    // Calculate the starting row (we may have scrolled down due to previous lines)
    int startRow = currentTop - MultilineInput.Cursor.Line;
    if (startRow < 0) startRow = 0;

    // Clear all lines of our input
    for (int i = 0; i < lineCount; i++)
    {
      Terminal.SetCursorPosition(0, startRow + i);
      Terminal.Write(new string(' ', Terminal.WindowWidth));
    }

    // Move back to start
    Terminal.SetCursorPosition(0, startRow);

    // Render each line
    for (int i = 0; i < lineCount; i++)
    {
      // Write prompt (primary for first line, continuation for rest)
      string linePrompt = i == 0
        ? PromptFormatter.Format(ReplOptions)
        : (ReplOptions.EnableColors ? ContinuationPrompt.WithStyle(ReplOptions.PromptColor) : ContinuationPrompt);

      Terminal.Write(linePrompt);

      // Write line content
      string lineContent = MultilineInput.Lines[i];
      if (ReplOptions.EnableColors && Endpoints is not null)
      {
        Terminal.Write(SyntaxHighlighter.Highlight(lineContent));
      }
      else
      {
        Terminal.Write(lineContent);
      }

      // Move to next line (except for last)
      if (i < lineCount - 1)
      {
        Terminal.WriteLine();
      }
    }

    // Position cursor correctly
    UpdateMultilineCursorPosition(startRow);
  }

  /// <summary>
  /// Updates the cursor position for multiline display.
  /// </summary>
  /// <param name="startRow">The row where multiline input starts.</param>
  private void UpdateMultilineCursorPosition(int startRow)
  {
    MultilineCursor cursor = MultilineInput.Cursor;
    int promptLength = cursor.Line == 0
      ? ReplOptions.Prompt.Length
      : ContinuationPrompt.Length;

    int row = startRow + cursor.Line;
    int col = promptLength + cursor.Column;

    // Set cursor position (bounds checking handled by terminal)
    if (col < Terminal.WindowWidth)
    {
      Terminal.SetCursorPosition(col, row);
    }
  }

  /// <summary>
  /// Initializes multiline mode from the current single-line input.
  /// Called when switching from single-line to multiline mode.
  /// </summary>
  private void InitializeMultilineFromSingleLine()
  {
    if (!MultilineInput.IsMultiline)
    {
      MultilineInput.SetText(UserInput);
      MultilineCursor cursor = MultilineInput.PositionToCursor(CursorPosition);
      MultilineInput.SetCursor(cursor.Line, cursor.Column);
    }
  }
}
