namespace TimeWarp.Terminal;

/// <summary>
/// Specifies where to place the ellipsis when truncating content that exceeds column width.
/// </summary>
public enum TruncateMode
{
  /// <summary>
  /// Truncate at the end, showing the beginning of the content.
  /// Example: "long text..." - This is the default behavior.
  /// </summary>
  End,

  /// <summary>
  /// Truncate at the start, showing the end of the content.
  /// Example: "...long text" - Useful for file paths where the end is most relevant.
  /// </summary>
  Start,

  /// <summary>
  /// Truncate in the middle, showing both the beginning and end of the content.
  /// Example: "long...text" - Useful when both start and end are important.
  /// </summary>
  Middle
}
