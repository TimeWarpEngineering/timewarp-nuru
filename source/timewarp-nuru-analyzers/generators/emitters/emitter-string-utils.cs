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
  /// </summary>
  public static string EscapeForStringLiteral(string value) =>
    value
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal)
      .Replace("\n", "\\n", StringComparison.Ordinal)
      .Replace("\r", "\\r", StringComparison.Ordinal)
      .Replace("\t", "\\t", StringComparison.Ordinal);
}
