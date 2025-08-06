namespace TimeWarp.Nuru;

/// <summary>
/// Interned strings for common values used throughout the CLI framework.
/// Using interned strings reduces memory allocations by ensuring identical strings
/// share the same memory location.
/// </summary>
internal static class CommonStrings
{
  // Option prefixes
  public static readonly string SingleDash = string.Intern("-");
  public static readonly string DoubleDash = string.Intern("--");
  // Boolean values
  public static readonly string True = string.Intern("true");
  public static readonly string False = string.Intern("false");
  public static readonly string Yes = string.Intern("yes");
  public static readonly string No = string.Intern("no");
  public static readonly string One = string.Intern("1");
  public static readonly string Zero = string.Intern("0");
  public static readonly string On = string.Intern("on");
  public static readonly string Off = string.Intern("off");
  public static readonly string Enabled = string.Intern("enabled");
  public static readonly string Disabled = string.Intern("disabled");
  // Common characters and symbols
  public static readonly string Space = string.Intern(" ");
  public static readonly string Empty = string.Empty; // Already interned by .NET
  public static readonly string Pipe = string.Intern("|");
  public static readonly string OpenBrace = string.Intern("{");
  public static readonly string CloseBrace = string.Intern("}");
  public static readonly string Asterisk = string.Intern("*");
}
