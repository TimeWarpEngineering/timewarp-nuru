namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Utilities for working with C# identifiers in generated code.
/// </summary>
internal static class CSharpIdentifierUtils
{
  /// <summary>
  /// C# keywords that require @ prefix when used as identifiers.
  /// </summary>
  private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
  {
    // Keywords
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
    "checked", "class", "const", "continue", "decimal", "default", "delegate",
    "do", "double", "else", "enum", "event", "explicit", "extern", "false",
    "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
    "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
    "new", "null", "object", "operator", "out", "override", "params", "private",
    "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
    "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
    "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
    "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",

    // Contextual keywords (some can be used as identifiers, but safer to escape)
    "add", "alias", "ascending", "async", "await", "by", "descending", "dynamic",
    "equals", "from", "get", "global", "group", "into", "join", "let", "nameof",
    "on", "orderby", "partial", "remove", "select", "set", "value", "var",
    "when", "where", "yield"
  };

  /// <summary>
  /// Escapes a C# keyword by prefixing with @.
  /// If the identifier is not a keyword, returns it unchanged.
  /// </summary>
  /// <param name="identifier">The identifier to potentially escape.</param>
  /// <returns>The escaped identifier (with @ prefix) if it's a keyword, otherwise the original.</returns>
  public static string EscapeIfKeyword(string identifier)
  {
    if (string.IsNullOrEmpty(identifier))
      return identifier;

    return CSharpKeywords.Contains(identifier) ? $"@{identifier}" : identifier;
  }

  /// <summary>
  /// Checks if the given identifier is a C# keyword.
  /// </summary>
  public static bool IsKeyword(string identifier)
  {
    return !string.IsNullOrEmpty(identifier) && CSharpKeywords.Contains(identifier);
  }
}
