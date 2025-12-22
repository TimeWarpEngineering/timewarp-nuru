namespace TimeWarp.Terminal;

/// <summary>
/// Defines the visual style for horizontal lines in terminal widgets.
/// </summary>
public enum LineStyle
{
  /// <summary>
  /// Single thin line using the box-drawing character ─ (U+2500).
  /// </summary>
  Thin,

  /// <summary>
  /// Double line using the box-drawing character ═ (U+2550).
  /// </summary>
  Doubled,

  /// <summary>
  /// Heavy thick line using the box-drawing character ━ (U+2501).
  /// </summary>
  Heavy
}

/// <summary>
/// Provides box-drawing characters for each <see cref="LineStyle"/>.
/// </summary>
public static class LineChars
{
  /// <summary>
  /// Gets the horizontal line character for the specified style.
  /// </summary>
  /// <param name="style">The line style.</param>
  /// <returns>The Unicode box-drawing character for horizontal lines.</returns>
  public static char GetHorizontal(LineStyle style) => style switch
  {
    LineStyle.Thin => '─',     // U+2500
    LineStyle.Doubled => '═',  // U+2550
    LineStyle.Heavy => '━',    // U+2501
    _ => '─'
  };
}
