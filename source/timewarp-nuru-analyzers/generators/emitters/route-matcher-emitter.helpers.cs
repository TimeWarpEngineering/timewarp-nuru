#region Purpose
// Type-name, collection, array conversion, and JSON member helpers for the route matcher.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;
using TimeWarp.Nuru.Validation;

internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Gets the simple type name from a fully qualified type name.
  /// Handles both dot notation (MyApp.Types.EmailAddress) and global:: prefix (global::EmailAddress).
  /// E.g., "MyApp.Types.EmailAddress" -> "EmailAddress"
  /// E.g., "global::EmailAddress" -> "EmailAddress"
  /// E.g., "global::MyApp.Types.EmailAddress" -> "EmailAddress"
  /// </summary>
  private static string GetSimpleTypeName(string fullyQualifiedName) =>
    CustomConverterMatcher.SimpleTypeName(fullyQualifiedName);

  /// <summary>
  /// Extracts the target type name from a converter type name by convention.
  /// E.g., "EmailAddressConverter" -> "EmailAddress"
  /// E.g., "MyApp.Converters.HexColorConverter" -> "HexColor"
  /// </summary>
  private static string GetTargetTypeFromConverterName(string converterTypeName) =>
    CustomConverterMatcher.TargetTypeFromConverterName(converterTypeName);

  /// <summary>
  /// Emits type conversion code for a repeated option.
  /// Converts List&lt;string&gt; to a typed array (built-in or enum elements).
  /// </summary>
  private static void EmitRepeatedOptionTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    string varName,
    string listVarName,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    RouteDefinition route)
  {
    // NURU_S010 reports a constraint with no array conversion (custom converters have none yet)
    if (UnknownTypeConstraintValidator.IsUnresolved(route, option, customConverters))
    {
      EmitUnresolvedOption(sb, route, option, varName, "string[]", $"{listVarName}.ToArray()");
      return;
    }

    string typeConstraint = option.TypeConstraint ?? "";
    string baseType = typeConstraint.EndsWith('?', StringComparison.Ordinal) ? typeConstraint[..^1] : typeConstraint;
    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";

    // Built-in typed conversion when a type constraint is present (int, double, etc.)
    if (!string.IsNullOrEmpty(baseType))
    {
      string errorStatement =
        $"app.WriteArgumentError($\"Error: Invalid value in option '{optionDisplay}'. Expected: {baseType}\", \"{optionDisplay}\");";
      bool converted = EmitBuiltInArrayConversion(
        sb,
        "      ",
        baseType,
        varName,
        listVarName,
        $"{listVarName}.Count",
        $"{listVarName}_i",
        $"{listVarName}_s",
        $"{listVarName}_parsed",
        errorStatement);
      if (converted)
        return;
    }

    // Enum element type from handler parameter (MyEnum[], IEnumerable<MyEnum>, etc.)
    string optionName = option.LongForm ?? option.ShortForm!;
    ParameterBinding? handlerParam = route.Handler.Parameters
      .FirstOrDefault(p =>
        string.Equals(p.SourceName, optionName, StringComparison.OrdinalIgnoreCase) &&
        p.IsEnumType);

    if (handlerParam is not null)
    {
      EmitRepeatedOptionEnumTypeConversion(
        sb,
        option,
        handlerParam.ParameterTypeName,
        varName,
        listVarName,
        routeIndex);
      return;
    }

    // No type constraint / unknown type / custom converter arrays: string[]
    sb.AppendLine($"      string[] {varName} = {listVarName}.ToArray();");
  }

  /// <summary>
  /// Emits enum conversion for a repeated option using EnumTypeConverter per element.
  /// Always emits ElementType[] so IEnumerable&lt;MyEnum&gt; parameters accept the array.
  /// Matches single-enum option error UX (invalid value + GetValidValuesMessage).
  /// </summary>
  private static void EmitRepeatedOptionEnumTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    string parameterTypeName,
    string varName,
    string listVarName,
    int routeIndex)
  {
    string baseEnumType = GetEnumElementTypeName(parameterTypeName);
    string converterVarName = $"__enumConverter_{varName}_{routeIndex}";
    string tempVarName = $"__temp_{varName}_{routeIndex}";
    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";
    string indexVarName = $"__i_{varName}_{routeIndex}";

    sb.AppendLine($"      var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{baseEnumType}>();");
    sb.AppendLine($"      {baseEnumType}[] {varName} = new {baseEnumType}[{listVarName}.Count];");
    sb.AppendLine($"      for (int {indexVarName} = 0; {indexVarName} < {listVarName}.Count; {indexVarName}++)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if (!{converterVarName}.TryConvert({listVarName}[{indexVarName}], out object? {tempVarName}))");
    sb.AppendLine("        {");
    sb.AppendLine($"          app.WriteArgumentError($\"Error: Invalid value '{{{listVarName}[{indexVarName}]}}' for option '{optionDisplay}'. {{{converterVarName}.GetValidValuesMessage()}}\", \"{optionDisplay}\");");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine($"        {varName}[{indexVarName}] = ({baseEnumType}){tempVarName}!;");
    sb.AppendLine("      }");
  }

  /// <summary>
  /// Extracts the enum element type name from a handler parameter type name.
  /// Strips nullable markers, array suffixes, and collection wrappers
  /// (IEnumerable/IList/ICollection) so EnumTypeConverter&lt;T&gt; receives a scalar enum.
  /// </summary>
  private static string GetEnumElementTypeName(string parameterTypeName)
  {
    string typeName = parameterTypeName;

    while (true)
    {
      if (typeName.EndsWith('?', StringComparison.Ordinal))
      {
        typeName = typeName[..^1];
        continue;
      }

      if (typeName.EndsWith("[]", StringComparison.Ordinal))
      {
        typeName = typeName[..^2];
        continue;
      }

      if (TryUnwrapCollectionTypeArgument(typeName, out string elementType))
      {
        typeName = elementType;
        continue;
      }

      break;
    }

    return typeName;
  }

  /// <summary>
  /// If <paramref name="typeName"/> is IEnumerable&lt;T&gt;, IList&lt;T&gt;, or ICollection&lt;T&gt;,
  /// returns the type argument T.
  /// </summary>
  private static bool TryUnwrapCollectionTypeArgument(string typeName, out string elementType)
  {
    elementType = "";

    // Match fully-qualified and short collection interface names with a single type argument.
    string[] prefixes =
    [
      "global::System.Collections.Generic.IEnumerable<",
      "global::System.Collections.Generic.IList<",
      "global::System.Collections.Generic.ICollection<",
      "System.Collections.Generic.IEnumerable<",
      "System.Collections.Generic.IList<",
      "System.Collections.Generic.ICollection<",
      "IEnumerable<",
      "IList<",
      "ICollection<"
    ];

    foreach (string prefix in prefixes)
    {
      if (typeName.StartsWith(prefix, StringComparison.Ordinal) && typeName.EndsWith('>', StringComparison.Ordinal))
      {
        elementType = typeName[prefix.Length..^1];
        return elementType.Length > 0;
      }
    }

    return false;
  }

  /// <summary>
  /// Fills an array by converting each string element (BuiltInElementParse).
  /// Returns false when <paramref name="baseType"/> has no built-in element conversion.
  /// </summary>
  private static bool EmitBuiltInArrayConversion(
    StringBuilder sb,
    string indent,
    string baseType,
    string arrayVarName,
    string sourceVarName,
    string lengthExpression,
    string indexVarName,
    string itemVarName,
    string parsedVarName,
    string errorStatement)
  {
    string? clrType = BuiltInElementParse.GetClrType(baseType);
    if (clrType is null)
      return false;

    sb.AppendLine($"{indent}{clrType}[] {arrayVarName} = new {clrType}[{lengthExpression}];");
    sb.AppendLine($"{indent}for (int {indexVarName} = 0; {indexVarName} < {lengthExpression}; {indexVarName}++)");
    sb.AppendLine($"{indent}{{");
    sb.AppendLine($"{indent}  string {itemVarName} = {sourceVarName}[{indexVarName}];");
    BuiltInElementParse.TryEmit(sb, indent + "  ", baseType, itemVarName, parsedVarName, [errorStatement, "return 1;"]);
    sb.AppendLine($"{indent}  {arrayVarName}[{indexVarName}] = {parsedVarName};");
    sb.AppendLine($"{indent}}}");
    return true;
  }

  /// <summary>
  /// Declares a JSON member local and rejects argv. The value arrives through <c>--json-args</c>.
  /// </summary>
  private static void EmitJsonMemberGuard(StringBuilder sb, OptionDefinition option, string varName, int routeIndex)
  {
    string typeName = option.ResolvedClrTypeName ?? "object";
    string initializer = option.DefaultValueLiteral ?? "default!";
    string longForm = option.LongForm ?? option.ShortForm ?? "value";
    string rejectedFlag = JsonArgvRejectedFlag(varName, routeIndex);
    sb.AppendLine($"      bool {rejectedFlag} = false;");
    sb.AppendLine($"      {typeName} {varName} = {initializer};");
    sb.AppendLine($"      for (int __i = 0; __i < __endOfOptions_{routeIndex}; __i++)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if (routeArgs[__i] == \"--{longForm}\" || routeArgs[__i].StartsWith(\"--{longForm}=\", global::System.StringComparison.Ordinal))");
    sb.AppendLine("        {");
    sb.AppendLine($"          {rejectedFlag} = true;");
    // Consume the member and its value so the after-match rejection, not the exact-length check, answers
    sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
    sb.AppendLine($"          if (routeArgs[__i] == \"--{longForm}\" && __i + 1 < __endOfOptions_{routeIndex} && !__optionForms_{routeIndex}.Contains(routeArgs[__i + 1]))");
    sb.AppendLine($"            __consumed_{routeIndex}.Add(__i + 1);");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
  }

  /// <summary>
  /// After the route literals match, reject a JSON member passed on argv and a required
  /// JSON member that was omitted. <c>--json-args</c> is a separate dispatcher.
  /// </summary>
  private static void EmitJsonMemberAfterMatch(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    foreach (OptionDefinition option in route.Options)
    {
      if (!option.IsJsonValue)
        continue;

      string varName = option.ParameterName ?? ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
      string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
      string longForm = option.LongForm ?? option.ShortForm ?? "value";
      string rejectedFlag = JsonArgvRejectedFlag(escapedVarName, routeIndex);
      sb.AppendLine($"      if ({rejectedFlag})");
      sb.AppendLine("      {");
      sb.AppendLine($"        await app.WriteBindErrorAsync(\"Error: --{longForm} is JSON. Pass it with --json-args.\", global::TimeWarp.Nuru.NuruBindErrorKind.InvalidValue, \"{longForm}\").ConfigureAwait(false);");
      sb.AppendLine("        return 1;");
      sb.AppendLine("      }");
      // This path has no --json-args, so a required JSON member is always missing.
      if (option.CapabilityRequired == true)
      {
        sb.AppendLine($"      await app.WriteBindErrorAsync(\"Error: pass --{longForm} via --json-args.\", global::TimeWarp.Nuru.NuruBindErrorKind.MissingValue, \"{longForm}\").ConfigureAwait(false);");
        sb.AppendLine("      return 1;");
      }
    }
  }

  private static string JsonArgvRejectedFlag(string varName, int routeIndex)
  {
    if (varName.StartsWith('@', StringComparison.Ordinal))
      varName = varName[1..];

    return $"__jsonArgvRejected_{varName}_{routeIndex}";
  }

  private static string ToCamelCase(string value) => CSharpIdentifierUtils.ToCamelCase(value);

  /// <summary>
  /// Escapes text for use in XML comments.
  /// </summary>
  private static string EscapeXmlComment(string value)
  {
    return value
      .Replace("&", "&amp;", StringComparison.Ordinal)
      .Replace("<", "&lt;", StringComparison.Ordinal)
      .Replace(">", "&gt;", StringComparison.Ordinal);
  }
}
