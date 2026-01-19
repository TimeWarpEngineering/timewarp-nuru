namespace TimeWarp.Nuru;

/// <summary>
/// Word manipulation handlers for the REPL console reader.
/// Implements PSReadLine-compatible case conversion and character transposition.
/// </summary>
public sealed partial class ReplConsoleReader
{
  /// <summary>
  /// PSReadLine: UpcaseWord - Convert characters from cursor to end of word to UPPERCASE.
  /// Moves cursor to end of word after conversion.
  /// </summary>
  internal void HandleUpcaseWord()
  {
    if (CursorPosition >= UserInput.Length)
      return;

    SaveUndoState(isCharacterInput: false);

    int startPos = CursorPosition;
    int endPos = FindWordEnd(CursorPosition);

    if (endPos > startPos)
    {
      string beforeWord = UserInput[..startPos];
      string word = UserInput[startPos..endPos].ToUpperInvariant();
      string afterWord = UserInput[endPos..];

      UserInput = beforeWord + word + afterWord;
      CursorPosition = endPos;
    }

    ResetKillTracking();
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: DowncaseWord - Convert characters from cursor to end of word to lowercase.
  /// Moves cursor to end of word after conversion.
  /// </summary>
  internal void HandleDowncaseWord()
  {
    if (CursorPosition >= UserInput.Length)
      return;

    SaveUndoState(isCharacterInput: false);

    int startPos = CursorPosition;
    int endPos = FindWordEnd(CursorPosition);

    if (endPos > startPos)
    {
      string beforeWord = UserInput[..startPos];
      string word = UserInput[startPos..endPos].ToLowerInvariant();
      string afterWord = UserInput[endPos..];

      UserInput = beforeWord + word + afterWord;
      CursorPosition = endPos;
    }

    ResetKillTracking();
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: CapitalizeWord - Capitalize first character after cursor, lowercase rest of word.
  /// Moves cursor to end of word after conversion.
  /// </summary>
  internal void HandleCapitalizeWord()
  {
    if (CursorPosition >= UserInput.Length)
      return;

    SaveUndoState(isCharacterInput: false);

    int startPos = CursorPosition;

    // Skip non-word characters to find start of word
    while (startPos < UserInput.Length && !IsWordChar(UserInput[startPos]))
      startPos++;

    if (startPos >= UserInput.Length)
      return;

    int endPos = FindWordEnd(startPos);

    if (endPos > startPos)
    {
      string beforeWord = UserInput[..startPos];
      char firstChar = char.ToUpperInvariant(UserInput[startPos]);
      string restOfWord = UserInput[(startPos + 1)..endPos].ToLowerInvariant();
      string afterWord = UserInput[endPos..];

      UserInput = beforeWord + firstChar + restOfWord + afterWord;
      CursorPosition = endPos;
    }

    ResetKillTracking();
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: SwapCharacters - Swap the character at cursor with the previous character.
  /// Moves cursor forward after swap (Emacs behavior).
  /// At the end of line, swaps the two characters before cursor.
  /// </summary>
  internal void HandleSwapCharacters()
  {
    // Need at least 2 characters and cursor not at position 0
    if (UserInput.Length < 2)
      return;

    SaveUndoState(isCharacterInput: false);

    int swapPos;
    if (CursorPosition == 0)
    {
      // At beginning: swap first two characters, cursor moves after them
      swapPos = 0;
    }
    else if (CursorPosition >= UserInput.Length)
    {
      // At end: swap the two characters before cursor
      swapPos = CursorPosition - 2;
    }
    else
    {
      // In middle: swap character at cursor with previous
      swapPos = CursorPosition - 1;
    }

    // Ensure swapPos is valid
    if (swapPos < 0 || swapPos + 1 >= UserInput.Length)
      return;

    char[] chars = UserInput.ToCharArray();
    (chars[swapPos], chars[swapPos + 1]) = (chars[swapPos + 1], chars[swapPos]);
    UserInput = new string(chars);

    // Move cursor forward (but not past end of line)
    CursorPosition = Math.Min(swapPos + 2, UserInput.Length);

    ResetKillTracking();
    CompletionHandler.Reset();
    RedrawLine();
  }

  /// <summary>
  /// PSReadLine: DeleteWord - Delete from cursor to end of word.
  /// This is an alias for KillWord in this implementation.
  /// </summary>
  internal void HandleDeleteWord()
  {
    // Delegate to KillWord - in PSReadLine, DeleteWord and KillWord behave the same
    HandleKillWord();
  }

  /// <summary>
  /// PSReadLine: BackwardDeleteWord - Delete from start of current word to cursor.
  /// This is an alias for BackwardKillWord in this implementation.
  /// </summary>
  internal void HandleBackwardDeleteWord()
  {
    // Delegate to BackwardKillWord - in PSReadLine these behave the same
    HandleBackwardKillWord();
  }

  /// <summary>
  /// Finds the end of the word starting from the given position.
  /// Word characters are letters, digits, and underscores.
  /// </summary>
  /// <param name="startPos">Position to start searching from.</param>
  /// <returns>Position just after the end of the word.</returns>
  private int FindWordEnd(int startPos)
  {
    int pos = startPos;

    // If we're on a non-word character, include it and any following non-word chars
    // then continue to end of word
    if (pos < UserInput.Length && !IsWordChar(UserInput[pos]))
    {
      // Skip non-word characters
      while (pos < UserInput.Length && !IsWordChar(UserInput[pos]))
        pos++;
    }

    // Skip word characters to find end of word
    while (pos < UserInput.Length && IsWordChar(UserInput[pos]))
      pos++;

    return pos;
  }
}
