#region Purpose
// Emits flag and value option parsing with index tracking.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Emits code to parse a boolean flag option with index tracking.
  /// </summary>
  private static void EmitFlagParsingWithIndexTracking(
    StringBuilder sb,
    OptionDefinition option,
    int routeIndex,
    RouteDefinition route)
  {
    string varName = ToCamelCase(option.LongForm ?? option.ShortForm ?? "flag");
    string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);

    string? longCheck = option.LongForm is not null ? $"routeArgs[__i] == \"--{option.LongForm}\"" : null;
    string? shortCheck = option.ShortForm is not null ? $"routeArgs[__i] == \"-{option.ShortForm}\"" : null;
    string condition = (longCheck, shortCheck) switch
    {
      (not null, not null) => $"{longCheck} || {shortCheck}",
      (not null, null) => longCheck,
      (null, not null) => shortCheck,
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    // Every occurrence is consumed, so a repeated flag (--verbose --verbose) is not left as a positional
    sb.AppendLine($"      bool {escapedVarName} = false;");
    sb.AppendLine($"      for (int __i = 0; __i < __endOfOptions_{routeIndex}; __i++)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if ({condition})");
    sb.AppendLine("        {");
    sb.AppendLine($"          {escapedVarName} = true;");
    sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // Effectively-required flag: an unbound, non-optional boolean flag is a route
    // discriminator (e.g. "commit --amend" with no bool parameter). It must be
    // present to match, mirroring the required-value-option guard. Bound flags
    // and explicitly-optional flags (--flag?) stay optional at match time.
    if (!option.IsOptional && !option.IsFlagBound(route))
    {
      sb.AppendLine($"      if (!{escapedVarName}) __optionMissing_{routeIndex} = true;");
    }
  }

  /// <summary>
  /// Emits code to parse an option with a value, tracking consumed indices.
  /// Supports both --opt value and --opt=value syntax.
  /// </summary>
  private static void EmitValueOptionParsingWithIndexTracking(
    StringBuilder sb,
    StringBuilder conversions,
    OptionDefinition option,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    RouteDefinition route)
  {
    string varName = option.ParameterName ?? ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
    string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
    if (option.IsJsonValue)
    {
      EmitJsonMemberGuard(sb, option, escapedVarName, routeIndex);
      return;
    }

    string flagFoundVar = $"__{varName}_flagFound_{routeIndex}";

    // For typed options, extract to a temp string first, then convert
    // Also check if the handler parameter is an enum type (implicit type inference)
    string optionName = option.LongForm ?? option.ShortForm!;
    bool isEnumType = route.Handler.Parameters
      .Any(p => string.Equals(p.SourceName, optionName, StringComparison.OrdinalIgnoreCase) && p.IsEnumType);
    bool needsConversion = option.TypeConstraint is not null || isEnumType;
    string rawVarName = needsConversion ? $"__{varName}_raw" : escapedVarName;

    sb.AppendLine($"      bool {flagFoundVar} = false;");
    // Use property default value when available (e.g., string Output = "text")
    string rawInitializer = !needsConversion && option.DefaultValueLiteral is not null
      ? option.DefaultValueLiteral
      : "null";
    sb.AppendLine($"      string? {rawVarName} = {rawInitializer};");
    sb.AppendLine($"      for (int __i = 0; __i < __endOfOptions_{routeIndex}; __i++)");
    sb.AppendLine("      {");

    // Check for --option=value or -o=value syntax
    if (option.LongForm is not null)
    {
      sb.AppendLine($"        if (routeArgs[__i].StartsWith(\"--{option.LongForm}=\", global::System.StringComparison.Ordinal))");
      sb.AppendLine("        {");
      sb.AppendLine($"          {flagFoundVar} = true;");
      sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
      sb.AppendLine($"          {rawVarName} = routeArgs[__i].Substring({3 + option.LongForm.Length});");
      sb.AppendLine("          break;");
      sb.AppendLine("        }");
    }

    if (option.ShortForm is not null)
    {
      sb.AppendLine($"        if (routeArgs[__i].StartsWith(\"-{option.ShortForm}=\", global::System.StringComparison.Ordinal))");
      sb.AppendLine("        {");
      sb.AppendLine($"          {flagFoundVar} = true;");
      sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
      sb.AppendLine($"          {rawVarName} = routeArgs[__i].Substring({2 + option.ShortForm.Length});");
      sb.AppendLine("          break;");
      sb.AppendLine("        }");
    }

    // Check for --option value or -o value syntax
    string? longCheckValue = option.LongForm is not null ? $"routeArgs[__i] == \"--{option.LongForm}\"" : null;
    string? shortCheckValue = option.ShortForm is not null ? $"routeArgs[__i] == \"-{option.ShortForm}\"" : null;
    string conditionValue = (longCheckValue, shortCheckValue) switch
    {
      (not null, not null) => $"{longCheckValue} || {shortCheckValue}",
      (not null, null) => longCheckValue,
      (null, not null) => shortCheckValue,
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    sb.AppendLine($"        if ({conditionValue})");
    sb.AppendLine("        {");
    sb.AppendLine($"          {flagFoundVar} = true;");
    sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
    sb.AppendLine("          // Check if next arg exists, is before end-of-options, and is NOT a defined option");
    sb.AppendLine($"          if (__i + 1 < __endOfOptions_{routeIndex} && !__optionForms_{routeIndex}.Contains(routeArgs[__i + 1]))");
    sb.AppendLine("          {");
    sb.AppendLine($"            {rawVarName} = routeArgs[__i + 1];");
    sb.AppendLine($"            __consumed_{routeIndex}.Add(__i + 1);");
    sb.AppendLine("          }");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // Handle required vs optional flag/value
    if (!option.IsOptional)
    {
      sb.AppendLine($"      if (!{flagFoundVar}) __optionMissing_{routeIndex} = true;");
      if (!option.ParameterIsOptional)
      {
        sb.AppendLine($"      if ({rawVarName} is null) __optionMissing_{routeIndex} = true;");
      }
    }
    else
    {
      if (!option.ParameterIsOptional)
      {
        sb.AppendLine($"      if ({flagFoundVar} && {rawVarName} is null) __optionMissing_{routeIndex} = true;");
      }
    }

    // Emit type conversion if needed
    if (needsConversion)
    {
      EmitOptionTypeConversion(conversions, option, escapedVarName, rawVarName, routeIndex, customConverters, route);
    }
  }

  /// <summary>
  /// Emits code to parse a repeated option with index tracking.
  /// Supports both --opt value and --opt=value syntax for each occurrence.
  /// </summary>
  private static void EmitRepeatedValueOptionParsingWithIndexTracking(
    StringBuilder sb,
    StringBuilder conversions,
    OptionDefinition option,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    RouteDefinition route)
  {
    string varName = option.ParameterName ?? ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
    string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
    string listVarName = $"__{varName}_list_{routeIndex}";

    sb.AppendLine($"      global::System.Collections.Generic.List<string> {listVarName} = [];");
    sb.AppendLine($"      for (int __i = 0; __i < __endOfOptions_{routeIndex}; __i++)");
    sb.AppendLine("      {");

    // Check for --option=value or -o=value syntax
    if (option.LongForm is not null)
    {
      sb.AppendLine($"        if (routeArgs[__i].StartsWith(\"--{option.LongForm}=\", global::System.StringComparison.Ordinal))");
      sb.AppendLine("        {");
      sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
      sb.AppendLine($"          {listVarName}.Add(routeArgs[__i].Substring({3 + option.LongForm.Length}));");
      sb.AppendLine("          continue;");
      sb.AppendLine("        }");
    }

    if (option.ShortForm is not null)
    {
      sb.AppendLine($"        if (routeArgs[__i].StartsWith(\"-{option.ShortForm}=\", global::System.StringComparison.Ordinal))");
      sb.AppendLine("        {");
      sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
      sb.AppendLine($"          {listVarName}.Add(routeArgs[__i].Substring({2 + option.ShortForm.Length}));");
      sb.AppendLine("          continue;");
      sb.AppendLine("        }");
    }

    // Check for --option value or -o value syntax
    string? longCheckRepeated = option.LongForm is not null ? $"routeArgs[__i] == \"--{option.LongForm}\"" : null;
    string? shortCheckRepeated = option.ShortForm is not null ? $"routeArgs[__i] == \"-{option.ShortForm}\"" : null;
    string conditionRepeated = (longCheckRepeated, shortCheckRepeated) switch
    {
      (not null, not null) => $"{longCheckRepeated} || {shortCheckRepeated}",
      (not null, null) => longCheckRepeated,
      (null, not null) => shortCheckRepeated,
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    sb.AppendLine($"        if ({conditionRepeated})");
    sb.AppendLine("        {");
    sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
    sb.AppendLine("          // Check if next arg exists, is before end-of-options, and is NOT a defined option");
    sb.AppendLine($"          if (__i + 1 < __endOfOptions_{routeIndex} && !__optionForms_{routeIndex}.Contains(routeArgs[__i + 1]))");
    sb.AppendLine("          {");
    sb.AppendLine($"            {listVarName}.Add(routeArgs[__i + 1]);");
    sb.AppendLine($"            __consumed_{routeIndex}.Add(__i + 1);");
    sb.AppendLine("            __i++;");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // Emit type conversion and array creation
    EmitRepeatedOptionTypeConversion(conversions, option, escapedVarName, listVarName, routeIndex, customConverters, route);
  }
}
