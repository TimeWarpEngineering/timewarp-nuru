namespace TimeWarp.Terminal;

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

  // Table-specific T-junction and cross characters

  /// <summary>
  /// Gets the top T-junction character (┬) for the specified border style.
  /// Used at the top border where column separators meet.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the top T-junction.</returns>
  public static char GetTopT(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '┬',  // U+252C (uses square T, no rounded version exists)
    BorderStyle.Square => '┬',   // U+252C
    BorderStyle.Doubled => '╦',   // U+2566
    BorderStyle.Heavy => '┳',    // U+2533
    _ => ' '
  };

  /// <summary>
  /// Gets the bottom T-junction character (┴) for the specified border style.
  /// Used at the bottom border where column separators meet.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the bottom T-junction.</returns>
  public static char GetBottomT(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '┴',  // U+2534 (uses square T, no rounded version exists)
    BorderStyle.Square => '┴',   // U+2534
    BorderStyle.Doubled => '╩',   // U+2569
    BorderStyle.Heavy => '┻',    // U+253B
    _ => ' '
  };

  /// <summary>
  /// Gets the left T-junction character (├) for the specified border style.
  /// Used at the left border where row separators meet.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the left T-junction.</returns>
  public static char GetLeftT(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '├',  // U+251C (uses square T, no rounded version exists)
    BorderStyle.Square => '├',   // U+251C
    BorderStyle.Doubled => '╠',   // U+2560
    BorderStyle.Heavy => '┣',    // U+2523
    _ => ' '
  };

  /// <summary>
  /// Gets the right T-junction character (┤) for the specified border style.
  /// Used at the right border where row separators meet.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the right T-junction.</returns>
  public static char GetRightT(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '┤',  // U+2524 (uses square T, no rounded version exists)
    BorderStyle.Square => '┤',   // U+2524
    BorderStyle.Doubled => '╣',   // U+2563
    BorderStyle.Heavy => '┫',    // U+252B
    _ => ' '
  };

  /// <summary>
  /// Gets the cross/intersection character (┼) for the specified border style.
  /// Used where row and column separators intersect.
  /// </summary>
  /// <param name="style">The border style.</param>
  /// <returns>The Unicode box-drawing character for the cross/intersection.</returns>
  public static char GetCross(BorderStyle style) => style switch
  {
    BorderStyle.Rounded => '┼',  // U+253C (uses square cross, no rounded version exists)
    BorderStyle.Square => '┼',   // U+253C
    BorderStyle.Doubled => '╬',   // U+256C
    BorderStyle.Heavy => '╋',    // U+254B
    _ => ' '
  };
}
