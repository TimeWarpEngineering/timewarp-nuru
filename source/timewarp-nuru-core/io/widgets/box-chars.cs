namespace TimeWarp.Nuru;

/// <summary>
/// Provides box-drawing characters for each <see cref="BorderStyle"/>.
/// </summary>
public static class BoxChars
{
  /// <summary>
  /// Gets the top-left corner character for the specified border style.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the top-left corner.</returns>
  public static char GetTopLeft(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '╭',  // U+256D
    BorderStyle.Square => '┌',   // U+250C
    BorderStyle.Doubled => '╔',   // U+2554
    BorderStyle.Heavy => '┏',    // U+250F
    _ => ' '
  };

  /// <summary>
  /// Gets the top-right corner character for the specified border style.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the top-right corner.</returns>
  public static char GetTopRight(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '╮',  // U+256E
    BorderStyle.Square => '┐',   // U+2510
    BorderStyle.Doubled => '╗',   // U+2557
    BorderStyle.Heavy => '┓',    // U+2513
    _ => ' '
  };

  /// <summary>
  /// Gets the bottom-left corner character for the specified border style.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the bottom-left corner.</returns>
  public static char GetBottomLeft(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '╰',  // U+2570
    BorderStyle.Square => '└',   // U+2514
    BorderStyle.Doubled => '╚',   // U+255A
    BorderStyle.Heavy => '┗',    // U+2517
    _ => ' '
  };

  /// <summary>
  /// Gets the bottom-right corner character for the specified border style.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the bottom-right corner.</returns>
  public static char GetBottomRight(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '╯',  // U+256F
    BorderStyle.Square => '┘',   // U+2518
    BorderStyle.Doubled => '╝',   // U+255D
    BorderStyle.Heavy => '┛',    // U+251B
    _ => ' '
  };

  /// <summary>
  /// Gets the horizontal line character for the specified border style.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for horizontal lines.</returns>
  public static char GetHorizontal(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '─',  // U+2500
    BorderStyle.Square => '─',   // U+2500
    BorderStyle.Doubled => '═',   // U+2550
    BorderStyle.Heavy => '━',    // U+2501
    _ => ' '
  };

  /// <summary>
  /// Gets the vertical line character for the specified border style.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for vertical lines.</returns>
  public static char GetVertical(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '│',  // U+2502
    BorderStyle.Square => '│',   // U+2502
    BorderStyle.Doubled => '║',   // U+2551
    BorderStyle.Heavy => '┃',    // U+2503
    _ => ' '
  };
}
