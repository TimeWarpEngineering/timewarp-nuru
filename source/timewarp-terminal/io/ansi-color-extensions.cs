// CA1822: Analyzer bug - doesn't recognize C# 14 extension block receiver 'text' as instance data
#pragma warning disable CA1822

namespace TimeWarp.Terminal;

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
  extension(string text)
  {
    /// <summary>Applies a custom ANSI color code to the text.</summary>
    /// <param name="colorCode">The ANSI escape code for the color (e.g., AnsiColors.Red or SyntaxColors.CommandColor).</param>
    public string WithStyle(string colorCode) => colorCode + text + AnsiColors.Reset;

    #region Basic Foreground Colors

    /// <summary>Applies black foreground color.</summary>
    public string Black() => text.WithStyle(AnsiColors.Black);

    /// <summary>Applies red foreground color.</summary>
    public string Red() => text.WithStyle(AnsiColors.Red);

    /// <summary>Applies green foreground color.</summary>
    public string Green() => text.WithStyle(AnsiColors.Green);

    /// <summary>Applies yellow foreground color.</summary>
    public string Yellow() => text.WithStyle(AnsiColors.Yellow);

    /// <summary>Applies blue foreground color.</summary>
    public string Blue() => text.WithStyle(AnsiColors.Blue);

    /// <summary>Applies magenta foreground color.</summary>
    public string Magenta() => text.WithStyle(AnsiColors.Magenta);

    /// <summary>Applies cyan foreground color.</summary>
    public string Cyan() => text.WithStyle(AnsiColors.Cyan);

    /// <summary>Applies white foreground color.</summary>
    public string White() => text.WithStyle(AnsiColors.White);

    /// <summary>Applies gray foreground color.</summary>
    public string Gray() => text.WithStyle(AnsiColors.Gray);

    #endregion

    #region Bright Foreground Colors

    /// <summary>Applies bright red foreground color.</summary>
    public string BrightRed() => text.WithStyle(AnsiColors.BrightRed);

    /// <summary>Applies bright green foreground color.</summary>
    public string BrightGreen() => text.WithStyle(AnsiColors.BrightGreen);

    /// <summary>Applies bright yellow foreground color.</summary>
    public string BrightYellow() => text.WithStyle(AnsiColors.BrightYellow);

    /// <summary>Applies bright blue foreground color.</summary>
    public string BrightBlue() => text.WithStyle(AnsiColors.BrightBlue);

    /// <summary>Applies bright magenta foreground color.</summary>
    public string BrightMagenta() => text.WithStyle(AnsiColors.BrightMagenta);

    /// <summary>Applies bright cyan foreground color.</summary>
    public string BrightCyan() => text.WithStyle(AnsiColors.BrightCyan);

    /// <summary>Applies bright white foreground color.</summary>
    public string BrightWhite() => text.WithStyle(AnsiColors.BrightWhite);

    #endregion

    #region Background Colors

    /// <summary>Applies black background color.</summary>
    public string OnBlack() => text.WithStyle(AnsiColors.BgBlack);

    /// <summary>Applies red background color.</summary>
    public string OnRed() => text.WithStyle(AnsiColors.BgRed);

    /// <summary>Applies green background color.</summary>
    public string OnGreen() => text.WithStyle(AnsiColors.BgGreen);

    /// <summary>Applies yellow background color.</summary>
    public string OnYellow() => text.WithStyle(AnsiColors.BgYellow);

    /// <summary>Applies blue background color.</summary>
    public string OnBlue() => text.WithStyle(AnsiColors.BgBlue);

    /// <summary>Applies magenta background color.</summary>
    public string OnMagenta() => text.WithStyle(AnsiColors.BgMagenta);

    /// <summary>Applies cyan background color.</summary>
    public string OnCyan() => text.WithStyle(AnsiColors.BgCyan);

    /// <summary>Applies white background color.</summary>
    public string OnWhite() => text.WithStyle(AnsiColors.BgWhite);

    #endregion

    #region Bright Background Colors

    /// <summary>Applies bright black (gray) background color.</summary>
    public string OnBrightBlack() => text.WithStyle(AnsiColors.BgBrightBlack);

    /// <summary>Applies bright red background color.</summary>
    public string OnBrightRed() => text.WithStyle(AnsiColors.BgBrightRed);

    /// <summary>Applies bright green background color.</summary>
    public string OnBrightGreen() => text.WithStyle(AnsiColors.BgBrightGreen);

    /// <summary>Applies bright yellow background color.</summary>
    public string OnBrightYellow() => text.WithStyle(AnsiColors.BgBrightYellow);

    /// <summary>Applies bright blue background color.</summary>
    public string OnBrightBlue() => text.WithStyle(AnsiColors.BgBrightBlue);

    /// <summary>Applies bright magenta background color.</summary>
    public string OnBrightMagenta() => text.WithStyle(AnsiColors.BgBrightMagenta);

    /// <summary>Applies bright cyan background color.</summary>
    public string OnBrightCyan() => text.WithStyle(AnsiColors.BgBrightCyan);

    /// <summary>Applies bright white background color.</summary>
    public string OnBrightWhite() => text.WithStyle(AnsiColors.BgBrightWhite);

    #endregion

    #region Text Formatting

    /// <summary>Applies bold formatting.</summary>
    public string Bold() => text.WithStyle(AnsiColors.Bold);

    /// <summary>Applies dim (faint) formatting.</summary>
    public string Dim() => text.WithStyle(AnsiColors.Dim);

    /// <summary>Applies italic formatting.</summary>
    public string Italic() => text.WithStyle(AnsiColors.Italic);

    /// <summary>Applies underline formatting.</summary>
    public string Underline() => text.WithStyle(AnsiColors.Underline);

    /// <summary>Applies blink formatting (not supported in all terminals).</summary>
    public string Blink() => text.WithStyle(AnsiColors.Blink);

    /// <summary>Applies reverse (inverted colors) formatting.</summary>
    public string Reverse() => text.WithStyle(AnsiColors.Reverse);

    /// <summary>Applies hidden (invisible) formatting.</summary>
    public string Hidden() => text.WithStyle(AnsiColors.Hidden);

    /// <summary>Applies strikethrough formatting.</summary>
    public string Strikethrough() => text.WithStyle(AnsiColors.Strikethrough);

    #endregion

    #region Common CSS Named Colors

    /// <summary>Applies orange foreground color.</summary>
    public string Orange() => text.WithStyle(AnsiColors.Orange);

    /// <summary>Applies pink foreground color.</summary>
    public string Pink() => text.WithStyle(AnsiColors.Pink);

    /// <summary>Applies purple foreground color.</summary>
    public string Purple() => text.WithStyle(AnsiColors.Purple);

    /// <summary>Applies gold foreground color.</summary>
    public string Gold() => text.WithStyle(AnsiColors.Gold);

    /// <summary>Applies coral foreground color.</summary>
    public string Coral() => text.WithStyle(AnsiColors.Coral);

    /// <summary>Applies crimson foreground color.</summary>
    public string Crimson() => text.WithStyle(AnsiColors.Crimson);

    /// <summary>Applies teal foreground color.</summary>
    public string Teal() => text.WithStyle(AnsiColors.Teal);

    /// <summary>Applies navy foreground color.</summary>
    public string Navy() => text.WithStyle(AnsiColors.Navy);

    /// <summary>Applies olive foreground color.</summary>
    public string Olive() => text.WithStyle(AnsiColors.Olive);

    /// <summary>Applies maroon foreground color.</summary>
    public string Maroon() => text.WithStyle(AnsiColors.Maroon);

    /// <summary>Applies lime foreground color.</summary>
    public string Lime() => text.WithStyle(AnsiColors.Lime);

    /// <summary>Applies aqua foreground color.</summary>
    public string Aqua() => text.WithStyle(AnsiColors.Aqua);

    /// <summary>Applies silver foreground color.</summary>
    public string Silver() => text.WithStyle(AnsiColors.Silver);

    /// <summary>Applies indigo foreground color.</summary>
    public string Indigo() => text.WithStyle(AnsiColors.Indigo);

    /// <summary>Applies violet foreground color.</summary>
    public string Violet() => text.WithStyle(AnsiColors.Violet);

    #endregion
  }
}
