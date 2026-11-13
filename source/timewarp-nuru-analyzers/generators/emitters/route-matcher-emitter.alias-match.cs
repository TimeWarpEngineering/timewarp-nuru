#region Purpose
// Emits alias matching and positional array construction for routes.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

internal static partial class RouteMatcherEmitter
{
  /// <summary>
  /// Offers the matched call to the route forwarder (AddServeRoute()). Argv parsing and type conversion
  /// have run, so their errors stay local; behaviors and the handler run on the server when it takes
  /// the call. The handler check comes after, so a CLI whose handler lives only in the server forwards.
  /// </summary>
  private static void EmitForward(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    string pattern = EmitterStringUtils.EscapeForStringLiteral(route.FullPattern);
    sb.AppendLine($"      if (!fromRepl && await global::TimeWarp.Nuru.NuruForwarding.TryForwardAsync(app, \"{pattern}\", args, null, __forwardMode, cancellationToken).ConfigureAwait(false) is int __forwardedExit_{routeIndex})");
    sb.AppendLine($"        return __forwardedExit_{routeIndex};");
  }

  /// <summary>
  /// Emits matching logic for an alias route using the same handler as the primary route.
  /// </summary>
  private static void EmitAliasMatch(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    string alias,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<CustomConverterDefinition> customConverters,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix,
    ImmutableArray<HttpClientConfiguration> httpClientConfigurations,
    bool useDirectInvoke,
    bool hasGeneratedMediator,
    bool forward,
    bool verbatimRendererOutput)
  {
    sb.AppendLine($"    // Alias: {EscapeXmlComment(alias)}");

    string[] aliasParts = alias.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // The alias is a complete replacement for groupPrefix + pattern literals.
    // Only non-literal segments (parameters, catch-all, end-of-options) need additional matching.
    int minPositionalArgs = aliasParts.Length +
      route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll) +
      route.Segments.Count(s => s is EndOfOptionsSeparatorDefinition);

    string aliasSkipLabel = $"alias_skip_{routeIndex}_{alias.GetHashCode(System.StringComparison.Ordinal):X}";

    sb.AppendLine($"    if (routeArgs.Length >= {minPositionalArgs})");
    sb.AppendLine("    {");

    EmitOptionFormsSet(sb, route, routeIndex);

    sb.AppendLine($"      int __endOfOptions_{routeIndex} = global::System.Array.IndexOf(routeArgs, \"--\");");
    sb.AppendLine($"      if (__endOfOptions_{routeIndex} < 0) __endOfOptions_{routeIndex} = routeArgs.Length;");
    sb.AppendLine($"      global::System.Collections.Generic.HashSet<int> __consumed_{routeIndex} = [];");

    string optionConversions = EmitOptionParsingWithIndexTracking(sb, route, routeIndex, customConverters);
    EmitPositionalArrayConstruction(sb, route, routeIndex, trackUnknownValues: aliasParts.Length == 0);
    if (aliasParts.Length > 0)
      EmitUnknownOptionPrefixCheck(sb, routeIndex, [.. aliasParts], aliasSkipLabel);
    else
      EmitUnknownOptionShapeCheck(sb, routeIndex, minPositionalArgs, exact: false, GetPositionalLiteralChecks(route, aliasParts, includeSegmentLiterals: false), aliasSkipLabel);
    EmitOptionGuardSkip(sb, route, routeIndex, aliasSkipLabel);
    sb.Append(optionConversions);

    sb.AppendLine($"      if (__positionalArgs_{routeIndex}.Length < {minPositionalArgs}) goto {aliasSkipLabel};");

    int positionalIndex = 0;

    // Match alias prefix instead of group prefix
    foreach (string word in aliasParts)
    {
      sb.AppendLine(
        $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"{EmitterStringUtils.EscapeForStringLiteral(word)}\") goto {aliasSkipLabel};");
      positionalIndex++;
    }

    // Match non-literal segments only (literals are already encoded in the alias string)
    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition:
          // Skip: literals are already part of the alias prefix
          break;

        case ParameterDefinition param when param.IsCatchAll:
          string catchAllVar = PositionalCaptureVar(param, routeIndex);
          sb.AppendLine(
            $"      string[] {catchAllVar} = __positionalArgs_{routeIndex}[{positionalIndex}..];");
          break;

        case ParameterDefinition param when param.IsOptional:
          string optVar = PositionalCaptureVar(param, routeIndex);
          sb.AppendLine(
            $"      string? {optVar} = __positionalArgs_{routeIndex}.Length > {positionalIndex} ? __positionalArgs_{routeIndex}[{positionalIndex}] : null;");
          positionalIndex++;
          break;

        case ParameterDefinition param:
          string reqVar = PositionalCaptureVar(param, routeIndex);
          sb.AppendLine(
            $"      string {reqVar} = __positionalArgs_{routeIndex}[{positionalIndex}];");
          positionalIndex++;
          break;

        case EndOfOptionsSeparatorDefinition:
          sb.AppendLine(
            $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"--\") goto {aliasSkipLabel};");
          positionalIndex++;
          break;
      }
    }

    EmitJsonMemberAfterMatch(sb, route, routeIndex);
    EmitTypeConversions(sb, route, routeIndex, customConverters, indent: 6);
    if (forward)
      EmitForward(sb, route, routeIndex);
    InterceptorEmitter.EmitMissingHandlerOnDispatch(sb, route, useRuntimeDI, hasGeneratedMediator, "      ");
    InterceptorEmitter.EmitExclusiveGate(sb, route, "      ", openBlock: true);

    if (behaviors.Length > 0)
    {
      bool commandCreatedByBehavior = route.Handler.HandlerKind == HandlerKind.Command;

      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8, commandAlreadyCreated: commandCreatedByBehavior, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix, httpClientConfigurations: httpClientConfigurations, useDirectInvoke: useDirectInvoke, hasGeneratedMediator: hasGeneratedMediator, verbatimRendererOutput: verbatimRendererOutput),
        methodSuffix: runtimeDISuffix);

      sb.AppendLine("      return global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode;");
    }
    else
    {
      HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 6, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix, httpClientConfigurations: httpClientConfigurations, useDirectInvoke: useDirectInvoke, hasGeneratedMediator: hasGeneratedMediator, verbatimRendererOutput: verbatimRendererOutput);
      sb.AppendLine("      return global::TimeWarp.Nuru.NuruCallContext.CurrentExitCode;");
    }

    InterceptorEmitter.EmitExclusiveGateEnd(sb, route, "      ");
    sb.AppendLine("    }");
    sb.AppendLine($"    {aliasSkipLabel}:;");
  }

  /// <summary>
  /// Emits a HashSet containing all option forms for this route.
  /// Used to distinguish option flags from option values (e.g., negative numbers).
  /// </summary>
  private static void EmitOptionFormsSet(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    List<string> forms = [];
    foreach (OptionDefinition option in route.Options)
    {
      if (option.LongForm is not null)
        forms.Add($"\"--{option.LongForm}\"");
      if (option.ShortForm is not null)
        forms.Add($"\"-{option.ShortForm}\"");
    }

    if (forms.Count > 0)
    {
      sb.AppendLine($"      global::System.Collections.Generic.HashSet<string> __optionForms_{routeIndex} = [{string.Join(", ", forms)}];");
    }
    else
    {
      sb.AppendLine($"      global::System.Collections.Generic.HashSet<string> __optionForms_{routeIndex} = [];");
    }
  }

  /// <summary>
  /// Emits a HashSet of the option forms that take a value, so <see cref="EmitUnknownOptionShapeCheck"/>
  /// drops the token after an unknown option only when that option is a typo of one of them.
  /// </summary>
  private static void EmitValueOptionFormsSet(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    List<string> forms = [];
    foreach (OptionDefinition option in route.Options.Where(option => !option.IsFlag))
    {
      if (option.LongForm is not null)
        forms.Add($"\"--{option.LongForm}\"");
      if (option.ShortForm is not null)
        forms.Add($"\"-{option.ShortForm}\"");
    }

    sb.AppendLine($"      global::System.Collections.Generic.HashSet<string> __valueOptionForms_{routeIndex} = [{string.Join(", ", forms)}];");
  }

  /// <summary>
  /// Emits code to build the positional args array from routeArgs,
  /// excluding consumed indices (options and their values).
  /// The -- separator is included only if the route explicitly expects it.
  /// With <paramref name="trackUnknownValues"/>, also builds <c>__positionalNoValues_N</c>: the positional
  /// args without the token right after each unknown option that is a typo of a value-taking option, for
  /// <see cref="EmitUnknownOptionShapeCheck"/>.
  /// </summary>
  private static void EmitPositionalArrayConstruction(StringBuilder sb, RouteDefinition route, int routeIndex, bool trackUnknownValues)
  {
    sb.AppendLine($"      string? __unknownOption_{routeIndex} = null;");
    sb.AppendLine($"      global::System.Collections.Generic.List<string> __positionalList_{routeIndex} = [];");
    if (trackUnknownValues)
    {
      sb.AppendLine($"      global::System.Collections.Generic.List<string> __positionalNoValues_{routeIndex} = [];");
      sb.AppendLine($"      int __unknownAt_{routeIndex} = -2;");
      EmitValueOptionFormsSet(sb, route, routeIndex);
    }

    sb.AppendLine("      for (int __i = 0; __i < routeArgs.Length; __i++)");
    sb.AppendLine("      {");

    // Only skip -- from positional args if the route does NOT explicitly include it
    // If the route has EndOfOptionsSeparatorDefinition, we need -- in the positional array to match against
    if (!route.HasEndOfOptions)
    {
      sb.AppendLine($"        if (__i == __endOfOptions_{routeIndex} && routeArgs[__i] == \"--\") continue;");
    }

    sb.AppendLine($"        if (__consumed_{routeIndex}.Contains(__i)) continue;");

    // Option-shaped tokens before -- that no declared option consumed are unknown options, not positionals
    sb.AppendLine($"        if (__i < __endOfOptions_{routeIndex} && !__optionForms_{routeIndex}.Contains(routeArgs[__i]) && global::TimeWarp.Nuru.NuruUnknownOption.IsOptionLike(routeArgs[__i]))");
    sb.AppendLine("        {");
    sb.AppendLine($"          __unknownOption_{routeIndex} ??= routeArgs[__i];");
    // Only a typo of a value-taking option owns the next token; a flag typo leaves it positional
    if (trackUnknownValues)
      sb.AppendLine($"          __unknownAt_{routeIndex} = global::TimeWarp.Nuru.NuruUnknownOption.TakesValue(routeArgs[__i], __optionForms_{routeIndex}, __valueOptionForms_{routeIndex}) ? __i : -2;");
    sb.AppendLine("          continue;");
    sb.AppendLine("        }");
    sb.AppendLine($"        __positionalList_{routeIndex}.Add(routeArgs[__i]);");
    if (trackUnknownValues)
    {
      // A token after "--" is never an option value
      sb.AppendLine($"        if (__i != __unknownAt_{routeIndex} + 1 || __i >= __endOfOptions_{routeIndex}) __positionalNoValues_{routeIndex}.Add(routeArgs[__i]);");
    }

    sb.AppendLine("      }");
    sb.AppendLine($"      string[] __positionalArgs_{routeIndex} = [.. __positionalList_{routeIndex}];");
  }
}
