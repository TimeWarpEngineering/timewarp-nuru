namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Utilities for working with C# identifiers in generated code.
/// </summary>
internal static class CSharpIdentifierUtils
{
  /// <summary>
  /// Escapes a C# keyword or contextual keyword by prefixing with @.
  /// Strips any existing leading @ before checking so the result is idempotent.
  /// If the identifier is not a keyword, returns it without an @ prefix.
  /// </summary>
  /// <param name="identifier">The identifier to potentially escape.</param>
  /// <returns>The escaped identifier (with @ prefix) if it's a keyword, otherwise the bare name.</returns>
  public static string EscapeIfKeyword(string identifier)
  {
    if (string.IsNullOrEmpty(identifier))
      return identifier;

    // Idempotent: strip any existing @ before classifying
    string name = identifier[0] == '@' ? identifier[1..] : identifier;
    if (name.Length == 0)
      return identifier;

    return IsKeyword(name) ? $"@{name}" : name;
  }

  /// <summary>
  /// Checks if the given identifier is a C# reserved or contextual keyword.
  /// Strips a leading @ before checking.
  /// </summary>
  public static bool IsKeyword(string identifier)
  {
    if (string.IsNullOrEmpty(identifier))
      return false;

    string name = identifier[0] == '@' ? identifier[1..] : identifier;
    if (name.Length == 0)
      return false;

    // Prefer Roslyn's maintained keyword tables over a hand-rolled set
    return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
        || SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None;
  }

  /// <summary>
  /// Converts a kebab-case string to camelCase.
  /// Handles simple strings (no hyphens) by lowering the first character.
  /// Examples: "dry-run" → "dryRun", "no-cache" → "noCache", "force" → "force"
  /// </summary>
  public static string ToCamelCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    string[] parts = value.Split('-');
    StringBuilder result = new();

    for (int i = 0; i < parts.Length; i++)
    {
      string part = parts[i];
      if (string.IsNullOrEmpty(part))
        continue;

      if (i == 0)
      {
        result.Append(char.ToLowerInvariant(part[0]));
      }
      else
      {
        result.Append(char.ToUpperInvariant(part[0]));
      }

      if (part.Length > 1)
      {
        result.Append(part[1..]);
      }
    }

    return result.ToString();
  }
}
