namespace TimeWarp.Nuru;

/// <summary>
/// Represents a text selection with anchor and cursor positions.
/// </summary>
/// <remarks>
/// <para>
/// The selection model uses two positions:
/// <list type="bullet">
/// <item><description>Anchor: Where the selection started (fixed)</description></item>
/// <item><description>Cursor: Current cursor position (moves with selection commands)</description></item>
/// </list>
/// </para>
/// <para>
/// The selection can extend in either direction from the anchor. Start and End
/// properties normalize the positions so Start is always less than or equal to End.
/// </para>
/// </remarks>
internal sealed class Selection
{
  /// <summary>
  /// Gets or sets the anchor position (where selection started).
  /// </summary>
  public int Anchor { get; set; } = -1;

  /// <summary>
  /// Gets or sets the cursor position (current end of selection).
  /// </summary>
  public int Cursor { get; set; } = -1;

  /// <summary>
  /// Gets whether a selection is active.
  /// </summary>
  public bool IsActive => Anchor >= 0 && Cursor >= 0 && Anchor != Cursor;

  /// <summary>
  /// Gets the start position of the selection (minimum of Anchor and Cursor).
  /// </summary>
  public int Start => Math.Min(Anchor, Cursor);

  /// <summary>
  /// Gets the end position of the selection (maximum of Anchor and Cursor).
  /// </summary>
  public int End => Math.Max(Anchor, Cursor);

  /// <summary>
  /// Gets the length of the selection.
  /// </summary>
  public int Length => IsActive ? End - Start : 0;

  /// <summary>
  /// Gets the selected text from the given input string.
  /// </summary>
  /// <param name="text">The full input text.</param>
  /// <returns>The selected portion of the text, or empty string if no selection.</returns>
  public string GetSelectedText(string text)
  {
    if (!IsActive || string.IsNullOrEmpty(text))
      return string.Empty;

    int start = Math.Max(0, Start);
    int end = Math.Min(text.Length, End);

    if (start >= end)
      return string.Empty;

    return text[start..end];
  }

  /// <summary>
  /// Starts a new selection at the given position.
  /// </summary>
  /// <param name="position">The position to start the selection.</param>
  public void StartAt(int position)
  {
    Anchor = position;
    Cursor = position;
  }

  /// <summary>
  /// Extends the selection to the given cursor position.
  /// </summary>
  /// <param name="newCursor">The new cursor position.</param>
  public void ExtendTo(int newCursor)
  {
    // If no anchor set, start selection at current cursor
    if (Anchor < 0)
      Anchor = Cursor >= 0 ? Cursor : newCursor;

    Cursor = newCursor;
  }

  /// <summary>
  /// Clears the selection.
  /// </summary>
  public void Clear()
  {
    Anchor = -1;
    Cursor = -1;
  }

  /// <summary>
  /// Gets whether the selection is valid for the given text length.
  /// </summary>
  /// <param name="textLength">The length of the text.</param>
  /// <returns>True if the selection is within bounds.</returns>
  public bool IsValidFor(int textLength)
  {
    if (!IsActive)
      return true;

    return Start >= 0 && End <= textLength;
  }
}
