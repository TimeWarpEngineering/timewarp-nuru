namespace TimeWarp.Nuru;

/// <summary>
/// Type and identifier validation methods for the parser.
/// </summary>
internal sealed partial class Parser
{
  private static bool IsBuiltInType(string type)
  {
    return type switch
    {
      "string" => true,
      "int" => true,
      "byte" => true,
      "sbyte" => true,
      "short" => true,
      "ushort" => true,
      "uint" => true,
      "ulong" => true,
      "float" => true,
      "char" => true,
      "long" => true,
      "double" => true,
      "decimal" => true,
      "bool" => true,
      "DateTime" => true,
      "Guid" => true,
      "TimeSpan" => true,
      "uri" or "Uri" => true,
      "fileinfo" or "FileInfo" => true,
      "directoryinfo" or "DirectoryInfo" => true,
      "ipaddress" or "IPAddress" => true,
      "dateonly" or "DateOnly" => true,
      "timeonly" or "TimeOnly" => true,
      _ => false
    };
  }

  private static bool IsValidTypeConstraint(string type)
  {
    // Accept known built-in types
    if (IsBuiltInType(type)) return true;

    // Accept any valid identifier format for custom types
    return IsValidIdentifierFormat(type);
  }

  private static bool IsValidIdentifierFormat(string identifier)
  {
    if (string.IsNullOrWhiteSpace(identifier))
    {
      return false;
    }

    // First character must be letter or underscore
    char first = identifier[0];
    if (!char.IsLetter(first) && first != '_')
    {
      return false;
    }

    // Remaining characters must be letters, digits, or underscores
    for (int i = 1; i < identifier.Length; i++)
    {
      char c = identifier[i];
      if (!char.IsLetterOrDigit(c) && c != '_')
      {
        return false;
      }
    }

    return true;
  }

  private static bool IsValidIdentifier(string identifier)
  {
    if (string.IsNullOrEmpty(identifier))
    {
      return false;
    }

    // First character must be letter or underscore
    char first = identifier[0];
    return char.IsLetter(first) || first == '_';
  }
}
