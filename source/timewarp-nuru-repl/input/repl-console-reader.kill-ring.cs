namespace TimeWarp.Nuru;

/// <summary>
/// Kill ring (cut/paste) handlers for the REPL console reader.
/// Implements Emacs-style kill and yank operations.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: KillLine - Kill text from cursor to end of line.
  /// Stores the killed text in the kill ring.
  /// </summary>
  internal void HandleKillLineToRing()
  {
    if (CursorPosition >= UserInput.Length)
      return;

    SaveUndoState(isCharacterInput: false);  // Save state before kill

    string killedText = UserInput[CursorPosition..];

    if (LastCommandWasKill)
    {
      KillRing.AppendToLast(killedText, prepend: false);
    }
    else
    {
      KillRing.Add(killedText);
    }

    UserInput = UserInput[..CursorPosition];
    LastCommandWasKill = true;
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: BackwardKillInput - Kill text from beginning of line to cursor.
  /// Also known as unix-line-discard in readline.
  /// </summary>
  internal void HandleBackwardKillInput()
  {
    if (CursorPosition == 0)
      return;

    SaveUndoState(isCharacterInput: false);  // Save state before kill

    string killedText = UserInput[..CursorPosition];

    if (LastCommandWasKill)
    {
      KillRing.AppendToLast(killedText, prepend: true);
    }
    else
    {
      KillRing.Add(killedText);
    }

    UserInput = UserInput[CursorPosition..];
    CursorPosition = 0;
    LastCommandWasKill = true;
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: UnixWordRubout - Kill the previous whitespace-delimited word.
  /// Also known as backward-kill-word with whitespace as the word boundary.
  /// </summary>
  internal void HandleUnixWordRubout()
  {
    if (CursorPosition == 0)
      return;

    SaveUndoState(isCharacterInput: false);  // Save state before kill

    int startPos = CursorPosition;

    // Skip whitespace behind cursor
    while (CursorPosition > 0 && char.IsWhiteSpace(UserInput[CursorPosition - 1]))
      CursorPosition--;

    // Skip non-whitespace to find start of word
    while (CursorPosition > 0 && !char.IsWhiteSpace(UserInput[CursorPosition - 1]))
      CursorPosition--;

    string killedText = UserInput[CursorPosition..startPos];

    if (LastCommandWasKill)
    {
      KillRing.AppendToLast(killedText, prepend: true);
    }
    else
    {
      KillRing.Add(killedText);
    }

    UserInput = UserInput[..CursorPosition] + UserInput[startPos..];
    LastCommandWasKill = true;
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: KillWord - Kill from cursor to end of current word.
  /// Uses word boundaries (non-word to word transitions).
  /// </summary>
  internal void HandleKillWord()
  {
    if (CursorPosition >= UserInput.Length)
      return;

    SaveUndoState(isCharacterInput: false);  // Save state before kill

    int startPos = CursorPosition;

    // Skip non-word characters at cursor
    while (CursorPosition < UserInput.Length && !IsWordChar(UserInput[CursorPosition]))
      CursorPosition++;

    // Skip word characters to find end of word
    while (CursorPosition < UserInput.Length && IsWordChar(UserInput[CursorPosition]))
      CursorPosition++;

    string killedText = UserInput[startPos..CursorPosition];

    if (LastCommandWasKill)
    {
      KillRing.AppendToLast(killedText, prepend: false);
    }
    else
    {
      KillRing.Add(killedText);
    }

    UserInput = UserInput[..startPos] + UserInput[CursorPosition..];
    CursorPosition = startPos;
    LastCommandWasKill = true;
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: BackwardKillWord - Kill from start of current word to cursor.
  /// Uses word boundaries (non-word to word transitions).
  /// </summary>
  internal void HandleBackwardKillWord()
  {
    if (CursorPosition == 0)
      return;

    SaveUndoState(isCharacterInput: false);  // Save state before kill

    int endPos = CursorPosition;

    // Skip non-word characters behind cursor
    while (CursorPosition > 0 && !IsWordChar(UserInput[CursorPosition - 1]))
      CursorPosition--;

    // Skip word characters to find start of word
    while (CursorPosition > 0 && IsWordChar(UserInput[CursorPosition - 1]))
      CursorPosition--;

    string killedText = UserInput[CursorPosition..endPos];

    if (LastCommandWasKill)
    {
      KillRing.AppendToLast(killedText, prepend: true);
    }
    else
    {
      KillRing.Add(killedText);
    }

    UserInput = UserInput[..CursorPosition] + UserInput[endPos..];
    LastCommandWasKill = true;
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: Yank - Paste the most recently killed text at the cursor position.
  /// </summary>
  internal void HandleYank()
  {
    string? text = KillRing.Yank();
    if (text is null)
      return;

    SaveUndoState(isCharacterInput: false);  // Save state before yank

    // Track yank position for YankPop
    LastYankStart = CursorPosition;
    LastYankLength = text.Length;

    UserInput = UserInput[..CursorPosition] + text + UserInput[CursorPosition..];
    CursorPosition += text.Length;
    LastCommandWasKill = false;
    LastCommandWasYank = true;
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: YankPop - Replace the just-yanked text with the previous kill ring entry.
  /// Only works immediately after Yank or YankPop.
  /// </summary>
  internal void HandleYankPop()
  {
    // YankPop only works if the last command was Yank or YankPop
    if (!LastCommandWasYank || !KillRing.CanYankPop)
      return;

    string? text = KillRing.YankPop();
    if (text is null)
      return;

    SaveUndoState(isCharacterInput: false);  // Save state before yank pop

    // Remove the previously yanked text
    int removeStart = LastYankStart;
    int removeEnd = removeStart + LastYankLength;
    UserInput = UserInput[..removeStart] + UserInput[removeEnd..];

    // Insert the new text
    UserInput = UserInput[..removeStart] + text + UserInput[removeStart..];
    CursorPosition = removeStart + text.Length;

    // Update yank tracking for subsequent YankPop calls
    LastYankLength = text.Length;
    LastCommandWasYank = true;

    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// Determines if a character is a word character for word-based operations.
  /// </summary>
  private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

  /// <summary>
  /// Resets the kill command tracking. Called by non-kill commands.
  /// </summary>
  private void ResetKillTracking()
  {
    LastCommandWasKill = false;
    LastCommandWasYank = false;
    KillRing.ResetYankPosition();
  }
}
