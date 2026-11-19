#region Purpose
// Emits flag and value option parsing with index tracking.
#endregion

#region Design
// A flag means presence is true and does not consume the next token.
// A following positional keeps its own binding, including a boolean spelling such as 0 or false.
// --flag=true is not a flag form.
// A value option whose value is required skips when the flag is present and no value token was taken.
// The property default applies only when the option is absent. --opt= counts as a value.
// A repeated option takes every following non-option token after one space-separated flag.
// --opt=value stays one value. A bare repeated flag with a required element skips the route.
// The rightmost greedy run gives trailing values back to required non-catch-all positionals
// that are still unbound, and keeps one value when the element is required.
// An optional positional reserves nothing.
// When a required positional or a catch-all follows, a typed element stops the run at the
// first token that does not convert, and that token stays positional.
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

    // Every occurrence is consumed, so a repeated flag (--verbose --verbose) is not left as a positional.
    // The next token is not the flag's value. A boolean spelling stays a positional.
    string flagInitializer = option.DefaultValueLiteral is "true" or "false" ? option.DefaultValueLiteral : "false";
    sb.AppendLine($"      bool {escapedVarName} = {flagInitializer};");
    sb.AppendLine($"      for (int __i = 0; __i < __endOfOptions_{routeIndex}; __i++)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if ({condition})");
    sb.AppendLine("        {");
    sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
    sb.AppendLine($"          {escapedVarName} = true;");
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
    // A string constraint ({value:string}) needs no conversion: it takes the untyped path,
    // which keeps a property default as the raw initializer.
    bool needsConversion = (option.TypeConstraint is not null && !TypeConversionMap.IsStringConstraint(option.TypeConstraint)) || isEnumType;
    string rawVarName = needsConversion ? $"__{varName}_raw" : escapedVarName;

    sb.AppendLine($"      bool {flagFoundVar} = false;");
    string valueTakenVar = $"__{varName}_valueTaken_{routeIndex}";
    sb.AppendLine($"      bool {valueTakenVar} = false;");
    // Use property default value when available (e.g., string Output = "text").
    // The default applies only when the option is absent. A present flag with no value token
    // is a missing value when the value is required, even if this initializer makes raw non-null.
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
      sb.AppendLine($"          {valueTakenVar} = true;");
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
      sb.AppendLine($"          {valueTakenVar} = true;");
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
    sb.AppendLine($"            {valueTakenVar} = true;");
    sb.AppendLine($"            __consumed_{routeIndex}.Add(__i + 1);");
    sb.AppendLine("          }");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // A required option that is absent skips. A present option whose value is required skips
    // when no value token was taken. An optional value keeps the default, including null.
    if (!option.IsOptional)
      sb.AppendLine($"      if (!{flagFoundVar}) __optionMissing_{routeIndex} = true;");

    if (!option.ParameterIsOptional)
      sb.AppendLine($"      if ({flagFoundVar} && !{valueTakenVar}) __optionMissing_{routeIndex} = true;");

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

    bool leaveUnparsed = route.Parameters.Any(parameter => parameter.IsCatchAll || !parameter.IsOptional);
    string? accepts = leaveUnparsed ? RepeatedElementAccepts(option) : null;
    int keep = option.ParameterIsOptional ? 0 : 1;

    sb.AppendLine($"        if ({conditionRepeated})");
    sb.AppendLine("        {");
    sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
    sb.AppendLine("          bool __tookValue = false;");
    sb.AppendLine("          bool __trackRun = false;");
    sb.AppendLine("          // Take every following non-option token. --opt=value above stays one value.");
    sb.AppendLine($"          while (__i + 1 < __endOfOptions_{routeIndex} && !__optionForms_{routeIndex}.Contains(routeArgs[__i + 1]) && !global::TimeWarp.Nuru.NuruUnknownOption.IsOptionLike(routeArgs[__i + 1]))");
    sb.AppendLine("          {");
    if (accepts is not null)
      sb.AppendLine($"            if (!({accepts})) break;");
    sb.AppendLine("            if (!__trackRun)");
    sb.AppendLine("            {");
    sb.AppendLine($"              if (__greedyRun_{routeIndex}.Count == 0 || __i + 1 > __greedyRun_{routeIndex}[__greedyRun_{routeIndex}.Count - 1])");
    sb.AppendLine("              {");
    sb.AppendLine($"                __greedyList_{routeIndex} = {listVarName};");
    sb.AppendLine($"                __greedyStart_{routeIndex} = {listVarName}.Count;");
    sb.AppendLine($"                __greedyKeep_{routeIndex} = {keep};");
    sb.AppendLine($"                __greedyRun_{routeIndex}.Clear();");
    sb.AppendLine("                __trackRun = true;");
    sb.AppendLine("              }");
    sb.AppendLine("            }");
    sb.AppendLine($"            {listVarName}.Add(routeArgs[__i + 1]);");
    sb.AppendLine($"            __consumed_{routeIndex}.Add(__i + 1);");
    sb.AppendLine($"            if (__trackRun) __greedyRun_{routeIndex}.Add(__i + 1);");
    sb.AppendLine("            __i++;");
    sb.AppendLine("            __tookValue = true;");
    sb.AppendLine("          }");
    if (!option.ParameterIsOptional)
      sb.AppendLine("          if (!__tookValue) __optionMissing_" + routeIndex + " = true;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // Emit type conversion and array creation
    EmitRepeatedOptionTypeConversion(conversions, option, escapedVarName, listVarName, routeIndex, customConverters, route);
  }

  /// <summary>
  /// Expression that is true when the next token converts to the repeated element, or null when
  /// every non-option token is accepted (string, or a type with no built-in conversion).
  /// </summary>
  private static string? RepeatedElementAccepts(OptionDefinition option)
  {
    string typeConstraint = option.TypeConstraint ?? "";
    if (typeConstraint.Length == 0)
      return null;

    string baseType = typeConstraint.EndsWith('?', StringComparison.Ordinal)
      ? typeConstraint[..^1]
      : typeConstraint;
    if (baseType.Length == 0 || baseType.Equals("string", StringComparison.OrdinalIgnoreCase))
      return null;

    // The shared char conversion assigns the output. A length check needs no local.
    if (baseType.Equals("char", StringComparison.OrdinalIgnoreCase))
      return "routeArgs[__i + 1].Length == 1";

    return TypeConversionMap.GetBuiltInTryConversion(baseType, "routeArgs[__i + 1]", "_")?.TryParseCondition;
  }
}
