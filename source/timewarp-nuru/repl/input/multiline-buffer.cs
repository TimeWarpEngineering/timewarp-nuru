namespace TimeWarp.Nuru;

/// <summary>
/// Represents a cursor position in a multiline buffer.
/// </summary>
/// <param name="Line">The zero-based line index.</param>
/// <param name="Column">The zero-based column position within the line.</param>
public readonly record struct MultilineCursor(int Line, int Column)
{
  /// <summary>
  /// Creates a cursor at the start of the buffer.
  /// </summary>
  public static MultilineCursor Start => new(0, 0);
}

/// <summary>
/// Manages multiline input text with cursor tracking for REPL editing.
/// </summary>
/// <remarks>
/// This class provides a data model for multiline editing:
/// <list type="bullet">
/// <item>Lines stored as List&lt;string&gt;</item>
/// <item>Cursor position as (Line, Column)</item>
/// <item>Methods for text manipulation across lines</item>
/// </list>
/// </remarks>
public sealed class MultilineBuffer
{
  private readonly List<string> _lines = [""];

  /// <summary>
  /// Gets the lines in the buffer.
  /// </summary>
  public IReadOnlyList<string> Lines => _lines;

  /// <summary>
  /// Gets or sets the current cursor position.
  /// </summary>
  public MultilineCursor Cursor { get; private set; } = MultilineCursor.Start;

  /// <summary>
  /// Gets the number of lines in the buffer.
  /// </summary>
  public int LineCount => _lines.Count;

  /// <summary>
  /// Gets whether the buffer contains multiple lines.
  /// </summary>
  public bool IsMultiline => _lines.Count > 1;

  /// <summary>
  /// Gets the current line text.
  /// </summary>
  public string CurrentLine => _lines[Cursor.Line];

  /// <summary>
  /// Gets the full text content with newlines between lines.
  /// </summary>
  /// <returns>The complete text as a single string.</returns>
  public string GetFullText() => string.Join(Environment.NewLine, _lines);

  /// <summary>
  /// Gets the full text content with a custom line separator.
  /// </summary>
  /// <param name="separator">The separator to use between lines.</param>
  /// <returns>The complete text as a single string.</returns>
  public string GetFullText(string separator) => string.Join(separator, _lines);

  /// <summary>
  /// Clears all content and resets the cursor.
  /// </summary>
  public void Clear()
  {
    _lines.Clear();
    _lines.Add(string.Empty);
    Cursor = MultilineCursor.Start;
  }

  /// <summary>
  /// Sets the buffer content from a single string.
  /// </summary>
  /// <param name="text">The text to set (may contain newlines).</param>
  public void SetText(string text)
  {
    _lines.Clear();

    if (string.IsNullOrEmpty(text))
    {
      _lines.Add(string.Empty);
      Cursor = MultilineCursor.Start;
      return;
    }

    // Split on any newline style
    string[] splitLines = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
    _lines.AddRange(splitLines);

    // Position cursor at end of last line
    Cursor = new MultilineCursor(_lines.Count - 1, _lines[^1].Length);
  }

  /// <summary>
  /// Inserts a character at the current cursor position.
  /// </summary>
  /// <param name="c">The character to insert.</param>
  public void InsertCharacter(char c)
  {
    string line = _lines[Cursor.Line];
    int col = Cursor.Column;

    _lines[Cursor.Line] = line[..col] + c + line[col..];
    Cursor = new MultilineCursor(Cursor.Line, col + 1);
  }

  /// <summary>
  /// Inserts text at the current cursor position.
  /// </summary>
  /// <param name="text">The text to insert (may contain newlines).</param>
  public void InsertText(string text)
  {
    if (string.IsNullOrEmpty(text))
      return;

    foreach (char c in text)
    {
      if (c is '\n' or '\r')
      {
        AddLine();
      }
      else
      {
        InsertCharacter(c);
      }
    }
  }

  /// <summary>
  /// Deletes the character before the cursor (backspace).
  /// </summary>
  /// <returns>True if a character was deleted, false if at start of buffer.</returns>
  public bool DeleteCharacterBefore()
  {
    if (Cursor.Column > 0)
    {
      // Delete within current line
      string line = _lines[Cursor.Line];
      _lines[Cursor.Line] = line[..(Cursor.Column - 1)] + line[Cursor.Column..];
      Cursor = new MultilineCursor(Cursor.Line, Cursor.Column - 1);
      return true;
    }

    if (Cursor.Line > 0)
    {
      // At start of line, merge with previous line
      string currentLine = _lines[Cursor.Line];
      int prevLineLength = _lines[Cursor.Line - 1].Length;
      _lines[Cursor.Line - 1] += currentLine;
      _lines.RemoveAt(Cursor.Line);
      Cursor = new MultilineCursor(Cursor.Line - 1, prevLineLength);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Deletes the character at the cursor position (delete key).
  /// </summary>
  /// <returns>True if a character was deleted, false if at end of buffer.</returns>
  public bool DeleteCharacterAt()
  {
    string line = _lines[Cursor.Line];

    if (Cursor.Column < line.Length)
    {
      // Delete within current line
      _lines[Cursor.Line] = line[..Cursor.Column] + line[(Cursor.Column + 1)..];
      return true;
    }

    if (Cursor.Line < _lines.Count - 1)
    {
      // At end of line, merge with next line
      _lines[Cursor.Line] += _lines[Cursor.Line + 1];
      _lines.RemoveAt(Cursor.Line + 1);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Adds a new line at the cursor position (like Shift+Enter).
  /// Splits the current line at the cursor and moves the remainder to the new line.
  /// </summary>
  public void AddLine()
  {
    string line = _lines[Cursor.Line];
    string beforeCursor = line[..Cursor.Column];
    string afterCursor = line[Cursor.Column..];

    _lines[Cursor.Line] = beforeCursor;
    _lines.Insert(Cursor.Line + 1, afterCursor);
    Cursor = new MultilineCursor(Cursor.Line + 1, 0);
  }

  /// <summary>
  /// Moves the cursor left by one character.
  /// </summary>
  /// <returns>True if cursor moved, false if at start.</returns>
  public bool MoveCursorLeft()
  {
    if (Cursor.Column > 0)
    {
      Cursor = new MultilineCursor(Cursor.Line, Cursor.Column - 1);
      return true;
    }

    if (Cursor.Line > 0)
    {
      Cursor = new MultilineCursor(Cursor.Line - 1, _lines[Cursor.Line - 1].Length);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Moves the cursor right by one character.
  /// </summary>
  /// <returns>True if cursor moved, false if at end.</returns>
  public bool MoveCursorRight()
  {
    if (Cursor.Column < _lines[Cursor.Line].Length)
    {
      Cursor = new MultilineCursor(Cursor.Line, Cursor.Column + 1);
      return true;
    }

    if (Cursor.Line < _lines.Count - 1)
    {
      Cursor = new MultilineCursor(Cursor.Line + 1, 0);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Moves the cursor to the beginning of the current line.
  /// </summary>
  public void MoveCursorToLineStart()
  {
    Cursor = new MultilineCursor(Cursor.Line, 0);
  }

  /// <summary>
  /// Moves the cursor to the end of the current line.
  /// </summary>
  public void MoveCursorToLineEnd()
  {
    Cursor = new MultilineCursor(Cursor.Line, _lines[Cursor.Line].Length);
  }

  /// <summary>
  /// Moves the cursor to the start of the buffer.
  /// </summary>
  public void MoveCursorToStart()
  {
    Cursor = MultilineCursor.Start;
  }

  /// <summary>
  /// Moves the cursor to the end of the buffer.
  /// </summary>
  public void MoveCursorToEnd()
  {
    Cursor = new MultilineCursor(_lines.Count - 1, _lines[^1].Length);
  }

  /// <summary>
  /// Sets the cursor position directly.
  /// </summary>
  /// <param name="line">The line index.</param>
  /// <param name="column">The column position.</param>
  /// <exception cref="ArgumentOutOfRangeException">If position is invalid.</exception>
  public void SetCursor(int line, int column)
  {
    if (line < 0 || line >= _lines.Count)
      throw new ArgumentOutOfRangeException(nameof(line), $"Line {line} is out of range [0, {_lines.Count - 1}]");

    int maxCol = _lines[line].Length;
    if (column < 0 || column > maxCol)
      throw new ArgumentOutOfRangeException(nameof(column), $"Column {column} is out of range [0, {maxCol}]");

    Cursor = new MultilineCursor(line, column);
  }

  /// <summary>
  /// Converts a linear position to a multiline cursor.
  /// </summary>
  /// <param name="position">The linear position (as if all text on one line).</param>
  /// <returns>The corresponding multiline cursor.</returns>
  public MultilineCursor PositionToCursor(int position)
  {
    int remaining = position;
    for (int line = 0; line < _lines.Count; line++)
    {
      int lineLength = _lines[line].Length;
      if (remaining <= lineLength)
        return new MultilineCursor(line, remaining);

      remaining -= lineLength + 1; // +1 for newline
    }

    // Position beyond end, return end of buffer
    return new MultilineCursor(_lines.Count - 1, _lines[^1].Length);
  }

  /// <summary>
  /// Converts a multiline cursor to a linear position.
  /// </summary>
  /// <param name="cursor">The cursor position.</param>
  /// <returns>The linear position (as if all text on one line).</returns>
  public int CursorToPosition(MultilineCursor cursor)
  {
    int position = 0;
    for (int line = 0; line < cursor.Line; line++)
      position += _lines[line].Length + 1; // +1 for newline

    position += cursor.Column;
    return position;
  }

  /// <summary>
  /// Gets the total character count including newlines.
  /// </summary>
  public int TotalLength
  {
    get
    {
      if (_lines.Count == 0) return 0;
      int total = 0;
      foreach (string line in _lines)
      {
        total += line.Length;
      }
      // Add newlines between lines
      total += _lines.Count - 1;
      return total;
    }
  }
}
