namespace TimeWarp.Terminal;

/// <summary>
/// ANSI escape codes for colored terminal output.
/// Includes all standard CSS named colors for comprehensive terminal styling.
/// </summary>
public static class AnsiColors
{
  /// <summary>
  /// Resets all ANSI formatting.
  /// </summary>
  public const string Reset = "\x1b[0m";

  // Basic colors
  public const string Black = "\x1b[30m";
  public const string Red = "\x1b[31m";
  public const string Green = "\x1b[32m";
  public const string Yellow = "\x1b[33m";
  public const string Blue = "\x1b[34m";
  public const string Magenta = "\x1b[35m";
  public const string Cyan = "\x1b[36m";
  public const string White = "\x1b[37m";
  public const string Gray = "\x1b[90m";

  // Bright colors
  public const string BrightRed = "\x1b[91m";
  public const string BrightGreen = "\x1b[92m";
  public const string BrightYellow = "\x1b[93m";
  public const string BrightBlue = "\x1b[94m";
  public const string BrightMagenta = "\x1b[95m";
  public const string BrightCyan = "\x1b[96m";
  public const string BrightWhite = "\x1b[97m";

  // CSS Named Colors (using 256-color mode approximations)
  public const string AliceBlue = "\x1b[38;5;255m";
  public const string AntiqueWhite = "\x1b[38;5;230m";
  public const string Aqua = "\x1b[38;5;51m";
  public const string Aquamarine = "\x1b[38;5;122m";
  public const string Azure = "\x1b[38;5;159m";
  public const string Beige = "\x1b[38;5;230m";
  public const string Bisque = "\x1b[38;5;224m";
  public const string BlanchedAlmond = "\x1b[38;5;223m";
  public const string BlueViolet = "\x1b[38;5;141m";
  public const string Brown = "\x1b[38;5;124m";
  public const string BurlyWood = "\x1b[38;5;180m";
  public const string CadetBlue = "\x1b[38;5;73m";
  public const string Chartreuse = "\x1b[38;5;118m";
  public const string Chocolate = "\x1b[38;5;173m";
  public const string Coral = "\x1b[38;5;209m";
  public const string CornflowerBlue = "\x1b[38;5;111m";
  public const string Cornsilk = "\x1b[38;5;230m";
  public const string Crimson = "\x1b[38;5;161m";
  public const string DarkBlue = "\x1b[38;5;18m";
  public const string DarkCyan = "\x1b[38;5;30m";
  public const string DarkGoldenRod = "\x1b[38;5;136m";
  public const string DarkGray = "\x1b[38;5;248m";
  public const string DarkGreen = "\x1b[38;5;22m";
  public const string DarkGrey = "\x1b[38;5;248m";
  public const string DarkKhaki = "\x1b[38;5;143m";
  public const string DarkMagenta = "\x1b[38;5;90m";
  public const string DarkOliveGreen = "\x1b[38;5;58m";
  public const string DarkOrange = "\x1b[38;5;208m";
  public const string DarkOrchid = "\x1b[38;5;128m";
  public const string DarkRed = "\x1b[38;5;88m";
  public const string DarkSalmon = "\x1b[38;5;174m";
  public const string DarkSeaGreen = "\x1b[38;5;108m";
  public const string DarkSlateBlue = "\x1b[38;5;60m";
  public const string DarkSlateGray = "\x1b[38;5;59m";
  public const string DarkSlateGrey = "\x1b[38;5;59m";
  public const string DarkTurquoise = "\x1b[38;5;44m";
  public const string DarkViolet = "\x1b[38;5;128m";
  public const string DeepPink = "\x1b[38;5;198m";
  public const string DeepSkyBlue = "\x1b[38;5;39m";
  public const string DimGray = "\x1b[38;5;242m";
  public const string DimGrey = "\x1b[38;5;242m";
  public const string DodgerBlue = "\x1b[38;5;33m";
  public const string FireBrick = "\x1b[38;5;124m";
  public const string FloralWhite = "\x1b[38;5;255m";
  public const string ForestGreen = "\x1b[38;5;28m";
  public const string Fuchsia = "\x1b[38;5;201m";
  public const string Gainsboro = "\x1b[38;5;254m";
  public const string GhostWhite = "\x1b[38;5;231m";
  public const string Gold = "\x1b[38;5;220m";
  public const string GoldenRod = "\x1b[38;5;178m";
  public const string Grey = "\x1b[38;5;244m";
  public const string GreenYellow = "\x1b[38;5;154m";
  public const string HoneyDew = "\x1b[38;5;194m";
  public const string HotPink = "\x1b[38;5;205m";
  public const string IndianRed = "\x1b[38;5;167m";
  public const string Indigo = "\x1b[38;5;54m";
  public const string Ivory = "\x1b[38;5;255m";
  public const string Khaki = "\x1b[38;5;179m";
  public const string Lavender = "\x1b[38;5;255m";
  public const string LavenderBlush = "\x1b[38;5;255m";
  public const string LawnGreen = "\x1b[38;5;119m";
  public const string LemonChiffon = "\x1b[38;5;224m";
  public const string LightBlue = "\x1b[38;5;153m";
  public const string LightCoral = "\x1b[38;5;210m";
  public const string LightCyan = "\x1b[38;5;195m";
  public const string LightGoldenRodYellow = "\x1b[38;5;229m";
  public const string LightGray = "\x1b[38;5;252m";
  public const string LightGreen = "\x1b[38;5;120m";
  public const string LightGrey = "\x1b[38;5;252m";
  public const string LightPink = "\x1b[38;5;217m";
  public const string LightSalmon = "\x1b[38;5;216m";
  public const string LightSeaGreen = "\x1b[38;5;37m";
  public const string LightSkyBlue = "\x1b[38;5;117m";
  public const string LightSlateGray = "\x1b[38;5;145m";
  public const string LightSlateGrey = "\x1b[38;5;145m";
  public const string LightSteelBlue = "\x1b[38;5;152m";
  public const string LightYellow = "\x1b[38;5;229m";
  public const string Lime = "\x1b[38;5;46m";
  public const string LimeGreen = "\x1b[38;5;77m";
  public const string Linen = "\x1b[38;5;231m";
  public const string Maroon = "\x1b[38;5;88m";
  public const string MediumAquaMarine = "\x1b[38;5;79m";
  public const string MediumBlue = "\x1b[38;5;20m";
  public const string MediumOrchid = "\x1b[38;5;134m";
  public const string MediumPurple = "\x1b[38;5;140m";
  public const string MediumSeaGreen = "\x1b[38;5;72m";
  public const string MediumSlateBlue = "\x1b[38;5;104m";
  public const string MediumSpringGreen = "\x1b[38;5;48m";
  public const string MediumTurquoise = "\x1b[38;5;80m";
  public const string MediumVioletRed = "\x1b[38;5;162m";
  public const string MidnightBlue = "\x1b[38;5;17m";
  public const string MintCream = "\x1b[38;5;194m";
  public const string MistyRose = "\x1b[38;5;224m";
  public const string Moccasin = "\x1b[38;5;223m";
  public const string NavajoWhite = "\x1b[38;5;223m";
  public const string Navy = "\x1b[38;5;17m";
  public const string OldLace = "\x1b[38;5;230m";
  public const string Olive = "\x1b[38;5;58m";
  public const string OliveDrab = "\x1b[38;5;64m";
  public const string Orange = "\x1b[38;5;214m";
  public const string OrangeRed = "\x1b[38;5;202m";
  public const string Orchid = "\x1b[38;5;170m";
  public const string PaleGoldenRod = "\x1b[38;5;187m";
  public const string PaleGreen = "\x1b[38;5;114m";
  public const string PaleTurquoise = "\x1b[38;5;159m";
  public const string PaleVioletRed = "\x1b[38;5;168m";
  public const string PapayaWhip = "\x1b[38;5;224m";
  public const string PeachPuff = "\x1b[38;5;223m";
  public const string Peru = "\x1b[38;5;173m";
  public const string Pink = "\x1b[38;5;218m";
  public const string Plum = "\x1b[38;5;182m";
  public const string PowderBlue = "\x1b[38;5;152m";
  public const string Purple = "\x1b[38;5;90m";
  public const string RebeccaPurple = "\x1b[38;5;97m";
  public const string RosyBrown = "\x1b[38;5;138m";
  public const string RoyalBlue = "\x1b[38;5;68m";
  public const string SaddleBrown = "\x1b[38;5;130m";
  public const string Salmon = "\x1b[38;5;209m";
  public const string SandyBrown = "\x1b[38;5;215m";
  public const string SeaGreen = "\x1b[38;5;72m";
  public const string SeaShell = "\x1b[38;5;255m";
  public const string Sienna = "\x1b[38;5;130m";
  public const string Silver = "\x1b[38;5;250m";
  public const string SkyBlue = "\x1b[38;5;116m";
  public const string SlateBlue = "\x1b[38;5;99m";
  public const string SlateGray = "\x1b[38;5;66m";
  public const string SlateGrey = "\x1b[38;5;66m";
  public const string Snow = "\x1b[38;5;255m";
  public const string SpringGreen = "\x1b[38;5;48m";
  public const string SteelBlue = "\x1b[38;5;67m";
  public const string Tan = "\x1b[38;5;180m";
  public const string Teal = "\x1b[38;5;30m";
  public const string Thistle = "\x1b[38;5;182m";
  public const string Tomato = "\x1b[38;5;203m";
  public const string Turquoise = "\x1b[38;5;80m";
  public const string Violet = "\x1b[38;5;213m";
  public const string Wheat = "\x1b[38;5;223m";
  public const string WhiteSmoke = "\x1b[38;5;255m";
  public const string YellowGreen = "\x1b[38;5;106m";

  // Background colors (using standard 8-bit background codes)
  public const string BgBlack = "\x1b[40m";
  public const string BgRed = "\x1b[41m";
  public const string BgGreen = "\x1b[42m";
  public const string BgYellow = "\x1b[43m";
  public const string BgBlue = "\x1b[44m";
  public const string BgMagenta = "\x1b[45m";
  public const string BgCyan = "\x1b[46m";
  public const string BgWhite = "\x1b[47m";

  // Bright background colors
  public const string BgBrightBlack = "\x1b[100m";
  public const string BgBrightRed = "\x1b[101m";
  public const string BgBrightGreen = "\x1b[102m";
  public const string BgBrightYellow = "\x1b[103m";
  public const string BgBrightBlue = "\x1b[104m";
  public const string BgBrightMagenta = "\x1b[105m";
  public const string BgBrightCyan = "\x1b[106m";
  public const string BgBrightWhite = "\x1b[107m";

  // Text formatting
  public const string Bold = "\x1b[1m";
  public const string Dim = "\x1b[2m";
  public const string Italic = "\x1b[3m";
  public const string Underline = "\x1b[4m";
  public const string Blink = "\x1b[5m";
  public const string Reverse = "\x1b[7m";
  public const string Hidden = "\x1b[8m";
  public const string Strikethrough = "\x1b[9m";
}
