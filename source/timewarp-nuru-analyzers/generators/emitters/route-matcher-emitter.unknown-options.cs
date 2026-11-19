#region Purpose
// Emits unknown-option tracking and option guard code for routes.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Emits the unknown-option stop for a route with leading literals. The route never runs with an
  /// unknown option; when the positional args start with <paramref name="literalPrefix"/>, the option
  /// and the route's option forms are recorded for the no-match error. No-op for an empty prefix.
  /// </summary>
  private static void EmitUnknownOptionPrefixCheck(StringBuilder sb, int routeIndex, List<string> literalPrefix, string skipLabel)
  {
    if (literalPrefix.Count == 0)
      return;

    StringBuilder prefixCondition = new($"__positionalArgs_{routeIndex}.Length >= {literalPrefix.Count}");
    for (int i = 0; i < literalPrefix.Count; i++)
    {
      prefixCondition.Append($" && __positionalArgs_{routeIndex}[{i}] == \"{EmitterStringUtils.EscapeForStringLiteral(literalPrefix[i])}\"");
    }

    sb.AppendLine($"      if (__unknownOption_{routeIndex} is not null)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if ({prefixCondition})");
    sb.AppendLine("        {");
    EmitUnknownOptionRecord(sb, routeIndex, indent: 10);
    sb.AppendLine("        }");
    sb.AppendLine($"        goto {skipLabel};");
    sb.AppendLine("      }");
  }

  /// <summary>
  /// Emits the unknown-option stop for a route with no leading literal. The option is recorded when
  /// the positional args fit the route: at least <paramref name="minPositionalArgs"/>, at most that many
  /// when <paramref name="exact"/>, and every literal in place. The args are tried as built and without
  /// the token right after each unknown option that is a typo of a value-taking option, which is likely its value.
  /// </summary>
  private static void EmitUnknownOptionShapeCheck(
    StringBuilder sb,
    int routeIndex,
    int minPositionalArgs,
    bool exact,
    List<(int Index, string Value)> literalChecks,
    string skipLabel)
  {
    string Fits(string list)
    {
      StringBuilder condition = new($"{list}.Count >= {minPositionalArgs}");
      if (exact)
        condition.Append($" && {list}.Count == {minPositionalArgs}");
      foreach ((int index, string value) in literalChecks)
        condition.Append($" && {list}.Count > {index} && {list}[{index}] == \"{EmitterStringUtils.EscapeForStringLiteral(value)}\"");
      return condition.ToString();
    }

    sb.AppendLine($"      if (__unknownOption_{routeIndex} is not null)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if (({Fits($"__positionalList_{routeIndex}")}) || ({Fits($"__positionalNoValues_{routeIndex}")}))");
    sb.AppendLine("        {");
    EmitUnknownOptionRecord(sb, routeIndex, indent: 10);
    sb.AppendLine("        }");
    sb.AppendLine($"        goto {skipLabel};");
    sb.AppendLine("      }");
  }

  /// <summary>
  /// The positional literals a route checks, at the index the matcher checks them: the
  /// <paramref name="prefixWords"/>, then segment literals (when <paramref name="includeSegmentLiterals"/>)
  /// and the "--" separator. Stops at a catch-all, which takes the rest.
  /// </summary>
  private static List<(int Index, string Value)> GetPositionalLiteralChecks(RouteDefinition route, string[] prefixWords, bool includeSegmentLiterals)
  {
    List<(int Index, string Value)> checks = [];
    int positionalIndex = 0;
    foreach (string word in prefixWords)
      checks.Add((positionalIndex++, word));

    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal when includeSegmentLiterals:
          checks.Add((positionalIndex++, literal.Value));
          break;
        case ParameterDefinition param when param.IsCatchAll:
          return checks;
        case ParameterDefinition:
          positionalIndex++;
          break;
        case EndOfOptionsSeparatorDefinition:
          checks.Add((positionalIndex++, "--"));
          break;
      }
    }

    return checks;
  }

  private static void EmitUnknownOptionRecord(StringBuilder sb, int routeIndex, int indent)
  {
    string pad = new(' ', indent);
    sb.AppendLine($"{pad}__unknownOption ??= __unknownOption_{routeIndex};");
    sb.AppendLine($"{pad}__unknownOptionForms.UnionWith(__optionForms_{routeIndex});");
  }

  /// <summary>
  /// Variable name for a positional parameter capture before type conversion.
  /// Typed and catch-all params use a unique temp name; untyped params use the
  /// final identifier (escaped if it is a C# keyword/contextual keyword).
  /// </summary>
  private static string PositionalCaptureVar(ParameterDefinition param, int routeIndex)
  {
    if (param.HasTypeConstraint || param.IsCatchAll)
      return $"__{param.CamelCaseName}_{routeIndex}";

    return CSharpIdentifierUtils.EscapeIfKeyword(param.CamelCaseName);
  }

  /// <summary>
  /// Emits option parsing with index tracking for the two-pass approach.
  /// Extracts options and their values, marking consumed indices.
  /// Supports both space-separated (--opt value) and equals syntax (--opt=value).
  /// A failed match guard sets <c>__optionMissing_N</c>; <see cref="EmitOptionGuardSkip"/> acts on it.
  /// </summary>
  /// <returns>The option type conversions, emitted by the caller after the guard skip.</returns>
  private static string EmitOptionParsingWithIndexTracking(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    int requiredPositionalCount)
  {
    if (HasOptionGuard(route))
      sb.AppendLine($"      bool __optionMissing_{routeIndex} = false;");

    bool hasRepeatedValue = route.Options.Any(option => !option.IsFlag && option.IsRepeated);
    if (hasRepeatedValue)
    {
      sb.AppendLine($"      global::System.Collections.Generic.List<int> __greedyRun_{routeIndex} = [];");
      sb.AppendLine($"      global::System.Collections.Generic.List<string>? __greedyList_{routeIndex} = null;");
      sb.AppendLine($"      int __greedyStart_{routeIndex} = 0;");
      sb.AppendLine($"      int __greedyKeep_{routeIndex} = 0;");
    }

    StringBuilder conversions = new();
    foreach (OptionDefinition option in route.Options)
    {
      if (option.IsFlag)
      {
        EmitFlagParsingWithIndexTracking(sb, option, routeIndex, route);
      }
      else if (option.IsRepeated)
      {
        EmitRepeatedValueOptionParsingWithIndexTracking(sb, conversions, option, routeIndex, customConverters, route);
      }
      else
      {
        EmitValueOptionParsingWithIndexTracking(sb, conversions, option, routeIndex, customConverters, route);
      }
    }

    if (hasRepeatedValue)
      EmitGreedyPositionalGiveBack(sb, routeIndex, requiredPositionalCount, route.HasEndOfOptions);

    return conversions.ToString();
  }

  /// <summary>
  /// Moves trailing values from the rightmost greedy repeated-option run back to unbound required
  /// positionals. An optional positional adds nothing to <paramref name="requiredPositionalCount"/>.
  /// The run keeps one value when its element is required.
  /// </summary>
  private static void EmitGreedyPositionalGiveBack(
    StringBuilder sb,
    int routeIndex,
    int requiredPositionalCount,
    bool hasEndOfOptions)
  {
    sb.AppendLine($"      int __positionalHave_{routeIndex} = 0;");
    sb.AppendLine("      for (int __j = 0; __j < routeArgs.Length; __j++)");
    sb.AppendLine("      {");
    if (!hasEndOfOptions)
    {
      sb.AppendLine($"        if (__j == __endOfOptions_{routeIndex} && routeArgs[__j] == \"--\") continue;");
    }

    sb.AppendLine($"        if (__consumed_{routeIndex}.Contains(__j)) continue;");
    sb.AppendLine($"        if (__j < __endOfOptions_{routeIndex} && !__optionForms_{routeIndex}.Contains(routeArgs[__j]) && global::TimeWarp.Nuru.NuruUnknownOption.IsOptionLike(routeArgs[__j])) continue;");
    sb.AppendLine($"        __positionalHave_{routeIndex}++;");
    sb.AppendLine("      }");
    sb.AppendLine($"      int __positionalDeficit_{routeIndex} = {requiredPositionalCount} - __positionalHave_{routeIndex};");
    sb.AppendLine($"      if (__positionalDeficit_{routeIndex} > 0 && __greedyList_{routeIndex} is not null)");
    sb.AppendLine("      {");
    sb.AppendLine($"        int __canReturn_{routeIndex} = __greedyRun_{routeIndex}.Count - __greedyKeep_{routeIndex};");
    sb.AppendLine($"        if (__canReturn_{routeIndex} > __positionalDeficit_{routeIndex}) __canReturn_{routeIndex} = __positionalDeficit_{routeIndex};");
    sb.AppendLine($"        if (__canReturn_{routeIndex} > 0)");
    sb.AppendLine("        {");
    sb.AppendLine($"          __greedyList_{routeIndex}.RemoveRange(__greedyStart_{routeIndex} + __greedyRun_{routeIndex}.Count - __canReturn_{routeIndex}, __canReturn_{routeIndex});");
    sb.AppendLine($"          for (int __k = __greedyRun_{routeIndex}.Count - __canReturn_{routeIndex}; __k < __greedyRun_{routeIndex}.Count; __k++)");
    sb.AppendLine($"            __consumed_{routeIndex}.Remove(__greedyRun_{routeIndex}[__k]);");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
  }

  /// <summary>
  /// True when a route option has a match guard: an unbound discriminator flag, a required value
  /// option, an option whose value is required when the option is given, or a repeated option
  /// whose element is required (a bare flag took no value).
  /// </summary>
  private static bool HasOptionGuard(RouteDefinition route) =>
    route.Options.Any(option =>
    {
      if (option.IsJsonValue)
        return false;

      if (option.IsFlag)
        return !option.IsOptional && !option.IsFlagBound(route);

      if (option.IsRepeated)
        return !option.ParameterIsOptional;

      return !option.IsOptional || !option.ParameterIsOptional;
    });

  /// <summary>
  /// Skips the route when an option guard failed. Emitted after the unknown-option check so a typo
  /// of the missing form is recorded first.
  /// </summary>
  private static void EmitOptionGuardSkip(StringBuilder sb, RouteDefinition route, int routeIndex, string skipLabel)
  {
    if (HasOptionGuard(route))
      sb.AppendLine($"      if (__optionMissing_{routeIndex}) goto {skipLabel};");
  }
}
