namespace TimeWarp.Nuru;

/// <summary>
/// Fluent extension methods for applying ANSI color codes to strings.
/// Provides a clean, chainable API for colored console output.
/// </summary>
/// <example>
/// <code>
/// "Error!".Red().Bold()
/// "Success!".Green()
/// "Warning".Yellow().OnBlack()
/// </code>
/// </example>
public static class AnsiColorExtensions
{
  #region Basic Foreground Colors

  /// <summary>Applies black foreground color.</summary>
  public static string Black(this string text)
    => AnsiColors.Black + text + AnsiColors.Reset;

  /// <summary>Applies red foreground color.</summary>
  public static string Red(this string text)
    => AnsiColors.Red + text + AnsiColors.Reset;

  /// <summary>Applies green foreground color.</summary>
  public static string Green(this string text)
    => AnsiColors.Green + text + AnsiColors.Reset;

  /// <summary>Applies yellow foreground color.</summary>
  public static string Yellow(this string text)
    => AnsiColors.Yellow + text + AnsiColors.Reset;

  /// <summary>Applies blue foreground color.</summary>
  public static string Blue(this string text)
    => AnsiColors.Blue + text + AnsiColors.Reset;

  /// <summary>Applies magenta foreground color.</summary>
  public static string Magenta(this string text)
    => AnsiColors.Magenta + text + AnsiColors.Reset;

  /// <summary>Applies cyan foreground color.</summary>
  public static string Cyan(this string text)
    => AnsiColors.Cyan + text + AnsiColors.Reset;

  /// <summary>Applies white foreground color.</summary>
  public static string White(this string text)
    => AnsiColors.White + text + AnsiColors.Reset;

  /// <summary>Applies gray foreground color.</summary>
  public static string Gray(this string text)
    => AnsiColors.Gray + text + AnsiColors.Reset;

  #endregion

  #region Bright Foreground Colors

  /// <summary>Applies bright red foreground color.</summary>
  public static string BrightRed(this string text)
    => AnsiColors.BrightRed + text + AnsiColors.Reset;

  /// <summary>Applies bright green foreground color.</summary>
  public static string BrightGreen(this string text)
    => AnsiColors.BrightGreen + text + AnsiColors.Reset;

  /// <summary>Applies bright yellow foreground color.</summary>
  public static string BrightYellow(this string text)
    => AnsiColors.BrightYellow + text + AnsiColors.Reset;

  /// <summary>Applies bright blue foreground color.</summary>
  public static string BrightBlue(this string text)
    => AnsiColors.BrightBlue + text + AnsiColors.Reset;

  /// <summary>Applies bright magenta foreground color.</summary>
  public static string BrightMagenta(this string text)
    => AnsiColors.BrightMagenta + text + AnsiColors.Reset;

  /// <summary>Applies bright cyan foreground color.</summary>
  public static string BrightCyan(this string text)
    => AnsiColors.BrightCyan + text + AnsiColors.Reset;

  /// <summary>Applies bright white foreground color.</summary>
  public static string BrightWhite(this string text)
    => AnsiColors.BrightWhite + text + AnsiColors.Reset;

  #endregion

  #region Background Colors

  /// <summary>Applies black background color.</summary>
  public static string OnBlack(this string text)
    => AnsiColors.BgBlack + text + AnsiColors.Reset;

  /// <summary>Applies red background color.</summary>
  public static string OnRed(this string text)
    => AnsiColors.BgRed + text + AnsiColors.Reset;

  /// <summary>Applies green background color.</summary>
  public static string OnGreen(this string text)
    => AnsiColors.BgGreen + text + AnsiColors.Reset;

  /// <summary>Applies yellow background color.</summary>
  public static string OnYellow(this string text)
    => AnsiColors.BgYellow + text + AnsiColors.Reset;

  /// <summary>Applies blue background color.</summary>
  public static string OnBlue(this string text)
    => AnsiColors.BgBlue + text + AnsiColors.Reset;

  /// <summary>Applies magenta background color.</summary>
  public static string OnMagenta(this string text)
    => AnsiColors.BgMagenta + text + AnsiColors.Reset;

  /// <summary>Applies cyan background color.</summary>
  public static string OnCyan(this string text)
    => AnsiColors.BgCyan + text + AnsiColors.Reset;

  /// <summary>Applies white background color.</summary>
  public static string OnWhite(this string text)
    => AnsiColors.BgWhite + text + AnsiColors.Reset;

  #endregion

  #region Bright Background Colors

  /// <summary>Applies bright black (gray) background color.</summary>
  public static string OnBrightBlack(this string text)
    => AnsiColors.BgBrightBlack + text + AnsiColors.Reset;

  /// <summary>Applies bright red background color.</summary>
  public static string OnBrightRed(this string text)
    => AnsiColors.BgBrightRed + text + AnsiColors.Reset;

  /// <summary>Applies bright green background color.</summary>
  public static string OnBrightGreen(this string text)
    => AnsiColors.BgBrightGreen + text + AnsiColors.Reset;

  /// <summary>Applies bright yellow background color.</summary>
  public static string OnBrightYellow(this string text)
    => AnsiColors.BgBrightYellow + text + AnsiColors.Reset;

  /// <summary>Applies bright blue background color.</summary>
  public static string OnBrightBlue(this string text)
    => AnsiColors.BgBrightBlue + text + AnsiColors.Reset;

  /// <summary>Applies bright magenta background color.</summary>
  public static string OnBrightMagenta(this string text)
    => AnsiColors.BgBrightMagenta + text + AnsiColors.Reset;

  /// <summary>Applies bright cyan background color.</summary>
  public static string OnBrightCyan(this string text)
    => AnsiColors.BgBrightCyan + text + AnsiColors.Reset;

  /// <summary>Applies bright white background color.</summary>
  public static string OnBrightWhite(this string text)
    => AnsiColors.BgBrightWhite + text + AnsiColors.Reset;

  #endregion

  #region Text Formatting

  /// <summary>Applies bold formatting.</summary>
  public static string Bold(this string text)
    => AnsiColors.Bold + text + AnsiColors.Reset;

  /// <summary>Applies dim (faint) formatting.</summary>
  public static string Dim(this string text)
    => AnsiColors.Dim + text + AnsiColors.Reset;

  /// <summary>Applies italic formatting.</summary>
  public static string Italic(this string text)
    => AnsiColors.Italic + text + AnsiColors.Reset;

  /// <summary>Applies underline formatting.</summary>
  public static string Underline(this string text)
    => AnsiColors.Underline + text + AnsiColors.Reset;

  /// <summary>Applies blink formatting (not supported in all terminals).</summary>
  public static string Blink(this string text)
    => AnsiColors.Blink + text + AnsiColors.Reset;

  /// <summary>Applies reverse (inverted colors) formatting.</summary>
  public static string Reverse(this string text)
    => AnsiColors.Reverse + text + AnsiColors.Reset;

  /// <summary>Applies hidden (invisible) formatting.</summary>
  public static string Hidden(this string text)
    => AnsiColors.Hidden + text + AnsiColors.Reset;

  /// <summary>Applies strikethrough formatting.</summary>
  public static string Strikethrough(this string text)
    => AnsiColors.Strikethrough + text + AnsiColors.Reset;

  #endregion

  #region Common CSS Named Colors

  /// <summary>Applies orange foreground color.</summary>
  public static string Orange(this string text)
    => AnsiColors.Orange + text + AnsiColors.Reset;

  /// <summary>Applies pink foreground color.</summary>
  public static string Pink(this string text)
    => AnsiColors.Pink + text + AnsiColors.Reset;

  /// <summary>Applies purple foreground color.</summary>
  public static string Purple(this string text)
    => AnsiColors.Purple + text + AnsiColors.Reset;

  /// <summary>Applies gold foreground color.</summary>
  public static string Gold(this string text)
    => AnsiColors.Gold + text + AnsiColors.Reset;

  /// <summary>Applies coral foreground color.</summary>
  public static string Coral(this string text)
    => AnsiColors.Coral + text + AnsiColors.Reset;

  /// <summary>Applies crimson foreground color.</summary>
  public static string Crimson(this string text)
    => AnsiColors.Crimson + text + AnsiColors.Reset;

  /// <summary>Applies teal foreground color.</summary>
  public static string Teal(this string text)
    => AnsiColors.Teal + text + AnsiColors.Reset;

  /// <summary>Applies navy foreground color.</summary>
  public static string Navy(this string text)
    => AnsiColors.Navy + text + AnsiColors.Reset;

  /// <summary>Applies olive foreground color.</summary>
  public static string Olive(this string text)
    => AnsiColors.Olive + text + AnsiColors.Reset;

  /// <summary>Applies maroon foreground color.</summary>
  public static string Maroon(this string text)
    => AnsiColors.Maroon + text + AnsiColors.Reset;

  /// <summary>Applies lime foreground color.</summary>
  public static string Lime(this string text)
    => AnsiColors.Lime + text + AnsiColors.Reset;

  /// <summary>Applies aqua foreground color.</summary>
  public static string Aqua(this string text)
    => AnsiColors.Aqua + text + AnsiColors.Reset;

  /// <summary>Applies silver foreground color.</summary>
  public static string Silver(this string text)
    => AnsiColors.Silver + text + AnsiColors.Reset;

  /// <summary>Applies indigo foreground color.</summary>
  public static string Indigo(this string text)
    => AnsiColors.Indigo + text + AnsiColors.Reset;

  /// <summary>Applies violet foreground color.</summary>
  public static string Violet(this string text)
    => AnsiColors.Violet + text + AnsiColors.Reset;

  #endregion
}
