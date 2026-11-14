#region Purpose
// Emits the parse of one string element into a built-in type, for the list paths: a typed catch-all,
// a repeated option, and a --json-args list.
#endregion

#region Design
// Built-in types are the TypeConversionMap set. Most have a TryParse condition
// (GetBuiltInTryConversion). Uri, FileInfo, and DirectoryInfo do not: Uri converts with
// Uri.TryCreate (RelativeOrAbsolute), and FileInfo and DirectoryInfo with their constructor inside a
// try/catch of Exception, the same construction the scalar emitters use. Before task 584 review M2
// the list paths only knew TryParse types, so those three reported NURU_S010 on a list.
// The caller supplies the failure statements. They must leave the loop (return or break), so the
// parsed local is definitely assigned after the parse.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits the per-element conversion of a built-in list element type.
/// </summary>
internal static class BuiltInElementParse
{
  /// <summary>
  /// Gets the CLR element type for a built-in list element constraint, or null when there is no
  /// built-in element conversion (string needs none and returns null too).
  /// </summary>
  public static string? GetClrType(string baseType)
  {
    (string ClrType, string TryParseCondition)? tryParse =
      TypeConversionMap.GetBuiltInTryConversion(baseType, "__value", "__parsed");
    if (tryParse is not null)
      return tryParse.Value.ClrType;

    return baseType.ToLowerInvariant() switch
    {
      "uri" => "global::System.Uri",
      "fileinfo" => "global::System.IO.FileInfo",
      "directoryinfo" => "global::System.IO.DirectoryInfo",
      _ => null
    };
  }

  /// <summary>
  /// Declares <paramref name="parsedVarName"/> and converts <paramref name="inputExpression"/> into it.
  /// Runs <paramref name="failStatements"/> when the value does not convert.
  /// Returns false and emits nothing when <paramref name="baseType"/> has no built-in element conversion.
  /// </summary>
  public static bool TryEmit(
    StringBuilder sb,
    string indent,
    string baseType,
    string inputExpression,
    string parsedVarName,
    IReadOnlyList<string> failStatements)
  {
    string? clrType = GetClrType(baseType);
    if (clrType is null)
      return false;

    sb.AppendLine($"{indent}{clrType} {parsedVarName};");

    (string ClrType, string TryParseCondition)? tryParse =
      TypeConversionMap.GetBuiltInTryConversion(baseType, inputExpression, parsedVarName);
    string lowered = baseType.ToLowerInvariant();
    if (tryParse is not null || lowered == "uri")
    {
      string condition = tryParse?.TryParseCondition
        ?? $"global::System.Uri.TryCreate({inputExpression}, global::System.UriKind.RelativeOrAbsolute, out {parsedVarName}) && {parsedVarName} is not null";
      sb.AppendLine($"{indent}if (!({condition}))");
      EmitFailBlock(sb, indent, failStatements);
      return true;
    }

    // FileInfo / DirectoryInfo: the constructor throws on an invalid path
    sb.AppendLine($"{indent}try");
    sb.AppendLine($"{indent}{{");
    sb.AppendLine($"{indent}  {parsedVarName} = new {clrType}({inputExpression});");
    sb.AppendLine($"{indent}}}");
    sb.AppendLine($"{indent}catch (global::System.Exception)");
    EmitFailBlock(sb, indent, failStatements);
    return true;
  }

  private static void EmitFailBlock(StringBuilder sb, string indent, IReadOnlyList<string> failStatements)
  {
    sb.AppendLine($"{indent}{{");
    foreach (string statement in failStatements)
      sb.AppendLine($"{indent}  {statement}");

    sb.AppendLine($"{indent}}}");
  }
}
