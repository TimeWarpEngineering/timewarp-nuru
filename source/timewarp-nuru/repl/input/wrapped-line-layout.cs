#region Purpose
// Maps a single-line REPL prompt-plus-input onto wrapped terminal rows and columns.
#endregion

#region Design
// Terminals wrap at WindowWidth: column = visualIndex % width, rowOffset = visualIndex / width.
// Occupied rows are ceil(displayLength / width) and at least 1, so an empty prompt line is still
// one row. RowsToClear uses the max of previous and next occupancy so shrinking a wrapped line
// erases stale characters on vacated rows. WindowWidth <= 0 is treated as 1 to avoid divide-by-zero.
// Display length is visible characters (prompt.Length + input.Length), not ANSI-coded string length.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Row and column math for a single logical line that wraps across terminal rows.
/// </summary>
internal static class WrappedLineLayout
{
  /// <summary>
  /// Returns a positive window width, treating non-positive values as 1.
  /// </summary>
  public static int EffectiveWindowWidth(int windowWidth) => windowWidth > 0 ? windowWidth : 1;

  /// <summary>
  /// Returns how many terminal rows a visible display of <paramref name="displayLength"/> occupies.
  /// </summary>
  public static int OccupiedRowCount(int displayLength, int windowWidth)
  {
    int width = EffectiveWindowWidth(windowWidth);
    if (displayLength <= 0)
      return 1;

    return (displayLength + width - 1) / width;
  }

  /// <summary>
  /// Maps a visible index (prompt length plus cursor index) onto a column and row offset.
  /// </summary>
  public static (int Column, int RowOffset) MapCursor(int visualIndex, int windowWidth)
  {
    int width = EffectiveWindowWidth(windowWidth);
    int safeIndex = visualIndex < 0 ? 0 : visualIndex;
    return (safeIndex % width, safeIndex / width);
  }

  /// <summary>
  /// Derives the row where the wrapped line starts from the current cursor row.
  /// </summary>
  public static int StartRow(int currentTop, int lastCursorVisualIndex, int windowWidth)
  {
    (_, int rowOffset) = MapCursor(lastCursorVisualIndex, windowWidth);
    int startRow = currentTop - rowOffset;
    return startRow < 0 ? 0 : startRow;
  }

  /// <summary>
  /// Returns how many rows to blank so both the previous and next display fit.
  /// </summary>
  public static int RowsToClear(int previousDisplayLength, int nextDisplayLength, int windowWidth)
  {
    int previousRows = OccupiedRowCount(previousDisplayLength, windowWidth);
    int nextRows = OccupiedRowCount(nextDisplayLength, windowWidth);
    return previousRows > nextRows ? previousRows : nextRows;
  }
}
