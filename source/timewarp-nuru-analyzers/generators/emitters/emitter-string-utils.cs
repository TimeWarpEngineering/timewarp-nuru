namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Shared string helpers for code emitters.
/// </summary>
internal static class EmitterStringUtils
{
  /// <summary>
  /// Escapes a string for safe embedding inside a double-quoted C# string literal.
  /// Consolidates the previously-duplicated (and inconsistently-covered) per-emitter
  /// EscapeString/EscapeCSharpString helpers into one correct implementation.
  /// Order matters: the backslash replacement must run first so the escapes it introduces
  /// for the other characters are not themselves re-escaped.
  /// Also escapes U+0085 (NEL), U+2028 (LINE SEPARATOR), and U+2029 (PARAGRAPH SEPARATOR),
  /// which are treated as line terminators by the C# language and would break a string literal
  /// if left unescaped.
  /// </summary>
  public static string EscapeForStringLiteral(string value) =>
    value
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal)
      .Replace("\n", "\\n", StringComparison.Ordinal)
      .Replace("\r", "\\r", StringComparison.Ordinal)
      .Replace("\t", "\\t", StringComparison.Ordinal)
      .Replace("\u0085", "\\u0085", StringComparison.Ordinal)
      .Replace("\u2028", "\\u2028", StringComparison.Ordinal)
      .Replace("\u2029", "\\u2029", StringComparison.Ordinal);
}
