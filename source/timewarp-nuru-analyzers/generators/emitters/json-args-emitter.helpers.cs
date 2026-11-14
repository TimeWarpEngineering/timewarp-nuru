#region Purpose
// Shared naming, type, and escaping helpers for the JSON args emitter.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class JsonArgsEmitter
{
  private static string Expected(string? constraint, ValueShape shape, string? enumType)
  {
    if (shape is ValueShape.Enum or ValueShape.EnumList)
      return enumType ?? constraint ?? "enum";

    if (shape == ValueShape.Bool)
      return "bool";

    if (shape is ValueShape.Number or ValueShape.NumberList)
      return constraint ?? "number";

    if (shape is ValueShape.TextList)
      return constraint is null ? "string array" : constraint + " array";

    return constraint ?? "string";
  }

  private static bool IsPlainString(Slot slot)
  {
    if (slot.Shape is not (ValueShape.Text or ValueShape.TextList))
      return false;

    if (slot.Converter is not null || slot.EnumTypeName is not null)
      return false;

    if (slot.BaseConstraint?.Equals("string", StringComparison.OrdinalIgnoreCase) == false)
      return false;

    return true;
  }

  private static bool IsSpecialParse(string constraint)
  {
    return constraint.ToLowerInvariant() is "uri" or "fileinfo" or "directoryinfo";
  }

  private static bool IsBoolName(string? name)
  {
    if (name is null)
      return false;

    string trimmed = StripQuestion(name) ?? name;
    return trimmed.Equals("bool", StringComparison.OrdinalIgnoreCase)
      || trimmed.Equals("boolean", StringComparison.OrdinalIgnoreCase)
      || trimmed.EndsWith("System.Boolean", StringComparison.Ordinal);
  }

  private static string? NumberClr(string? constraint)
  {
    if (constraint is null)
      return null;

    return constraint.ToLowerInvariant() switch
    {
      "int" or "int32" => "int",
      "long" or "int64" => "long",
      "short" or "int16" => "short",
      "byte" => "byte",
      "sbyte" => "sbyte",
      "ushort" or "uint16" => "ushort",
      "uint" or "uint32" => "uint",
      "ulong" or "uint64" => "ulong",
      "float" or "single" => "float",
      "double" => "double",
      "decimal" => "decimal",
      _ => null
    };
  }

  private static string? TryGetCall(Slot slot, string element, string outDecl)
  {
    string? keyword = slot.NumberKeyword;
    if (keyword is null)
      return null;

    string method = keyword switch
    {
      "int" => "TryGetInt32",
      "long" => "TryGetInt64",
      "short" => "TryGetInt16",
      "byte" => "TryGetByte",
      "sbyte" => "TryGetSByte",
      "ushort" => "TryGetUInt16",
      "uint" => "TryGetUInt32",
      "ulong" => "TryGetUInt64",
      "float" => "TryGetSingle",
      "double" => "TryGetDouble",
      "decimal" => "TryGetDecimal",
      _ => "TryGetDouble"
    };
    return $"{element}.{method}(out {outDecl})";
  }

  private static string TryGetDiscard(Slot slot, string element)
  {
    string? keyword = slot.NumberKeyword ?? "int";
    string method = keyword switch
    {
      "int" => "TryGetInt32",
      "long" => "TryGetInt64",
      "short" => "TryGetInt16",
      "byte" => "TryGetByte",
      "sbyte" => "TryGetSByte",
      "ushort" => "TryGetUInt16",
      "uint" => "TryGetUInt32",
      "ulong" => "TryGetUInt64",
      "float" => "TryGetSingle",
      "double" => "TryGetDouble",
      "decimal" => "TryGetDecimal",
      _ => "TryGetInt32"
    };
    return $"{element}.{method}(out _)";
  }

  private static CustomConverterDefinition? FindConverter(string? constraint, ImmutableArray<CustomConverterDefinition> converters) =>
    converters.IsDefaultOrEmpty ? null : CustomConverterMatcher.Find(constraint, converters);

  private static string? StripQuestion(string? typeName)
  {
    if (typeName is null)
      return null;

    return typeName.EndsWith('?', StringComparison.Ordinal) ? typeName[..^1] : typeName;
  }

  private static string UnwrapElement(string typeName)
  {
    string name = StripQuestion(typeName) ?? typeName;
    if (name.EndsWith("[]", StringComparison.Ordinal))
      return name[..^2];

    int start = name.IndexOf('<', StringComparison.Ordinal);
    int end = name.LastIndexOf('>', StringComparison.Ordinal);
    if (start >= 0 && end > start)
      return name[(start + 1)..end];

    return name;
  }

  /// <summary>
  /// Slot id for an option, using the same parameter-then-option allocation as <see cref="BuildSlots"/>.
  /// </summary>
  internal static string OptionSlotId(RouteDefinition route, OptionDefinition option)
  {
    HashSet<string> ids = new(StringComparer.Ordinal);
    foreach (ParameterDefinition param in route.Parameters)
      AllocId(param.Name, ids);

    foreach (OptionDefinition candidate in route.Options)
    {
      string key = candidate.LongForm ?? candidate.ShortForm ?? "option";
      string id = AllocId(key, ids);
      if (ReferenceEquals(candidate, option))
        return id;
    }

    return AllocId(option.LongForm ?? option.ShortForm ?? "option", ids);
  }

  private static string AllocId(string key, HashSet<string> ids)
  {
    StringBuilder sb = new();
    foreach (char c in key)
    {
      sb.Append(char.IsLetterOrDigit(c) ? c : '_');
    }

    if (sb.Length == 0 || char.IsDigit(sb[0]))
      sb.Insert(0, '_');

    string id = sb.ToString();
    if (ids.Add(id))
      return id;

    int n = 2;
    while (!ids.Add(id + "_" + n))
      n++;

    return id + "_" + n;
  }

  private static string[] GroupWords(RouteDefinition route)
  {
    if (string.IsNullOrEmpty(route.GroupPrefix))
      return [];

    return route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);
  }

  private static int ReservedAfter(List<SegmentDefinition> segments, int index, bool aliasMode)
  {
    int count = 0;
    for (int i = index + 1; i < segments.Count; i++)
    {
      if (!aliasMode && segments[i] is LiteralDefinition)
        count++;

      if (segments[i] is EndOfOptionsSeparatorDefinition)
        count++;
    }

    return count;
  }

  private static string FlagCondition(OptionDefinition option)
  {
    string? longCheck = option.LongForm is not null
      ? $"routeArgs[__i] == \"--{Escape(option.LongForm)}\""
      : null;
    string? shortCheck = option.ShortForm is not null
      ? $"routeArgs[__i] == \"-{Escape(option.ShortForm)}\""
      : null;
    return (longCheck, shortCheck) switch
    {
      (not null, not null) => $"{longCheck} || {shortCheck}",
      (not null, null) => longCheck,
      (null, not null) => shortCheck!,
      _ => "false"
    };
  }

  private static string Escape(string value) => EmitterStringUtils.EscapeForStringLiteral(value);

  // Pattern text is embedded in generated interpolated strings. Doubling braces keeps `{env}` literal.
  private static string ForInterpolation(string alreadyEscaped) =>
    alreadyEscaped.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal);
}
