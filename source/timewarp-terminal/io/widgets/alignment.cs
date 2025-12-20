namespace TimeWarp.Terminal;

/// <summary>
/// Specifies horizontal alignment for table column content.
/// </summary>
public enum Alignment
{
  /// <summary>
  /// Content is aligned to the left edge of the column.
  /// This is the default alignment for most text content.
  /// </summary>
  Left,

  /// <summary>
  /// Content is centered within the column.
  /// Useful for headers or status indicators.
  /// </summary>
  Center,

  /// <summary>
  /// Content is aligned to the right edge of the column.
  /// Common for numeric data like counts, sizes, or prices.
  /// </summary>
  Right
}
