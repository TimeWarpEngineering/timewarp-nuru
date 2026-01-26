// Emits pattern matching code for individual routes.
// Generates C# pattern matching expressions for each route definition.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits pattern matching code for a single route.
/// Generates C# list patterns and conditional logic for route matching.
/// </summary>
internal static class RouteMatcherEmitter
{
  /// <summary>
  /// Emits the pattern matching code for a route.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route definition to emit.</param>
  /// <param name="routeIndex">The index of this route (used for unique handler names).</param>
  /// <param name="services">Registered services from ConfigureServices.</param>
  /// <param name="behaviors">Pipeline behaviors to wrap the handler with.</param>
  /// <param name="customConverters">Custom type converters registered via AddTypeConverter.</param>
  /// <param name="loggerFactoryFieldName">Logger factory source for ILogger injection, or null if no logging.</param>
  /// <param name="useRuntimeDI">When true, uses GetServiceProvider().GetRequiredService&lt;T&gt;() instead of static instantiation.</param>
  /// <param name="runtimeDISuffix">Suffix for per-app runtime DI methods (e.g., "_0" for multi-app assemblies).</param>
  public static void Emit(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors = default,
    ImmutableArray<CustomConverterDefinition> customConverters = default,
    string? loggerFactoryFieldName = null,
    bool useRuntimeDI = false,
    string runtimeDISuffix = "")
  {
    // Use empty array if optional parameters not provided
    if (behaviors.IsDefault)
      behaviors = [];
    if (customConverters.IsDefault)
      customConverters = [];

    // Comment showing the route pattern
    sb.AppendLine(
      $"    // Route: {EscapeXmlComment(route.FullPattern)}");
    // DEBUG: Show custom converter count
    sb.AppendLine(
      $"    // DEBUG: CustomConverters.Length={customConverters.Length}");

    // Per-route help: Check if args end with --help and match this route's literal prefix
    // This enables "command --help" to show help for just that command
    RouteHelpEmitter.EmitPerRouteHelpCheck(sb, route, routeIndex);

    // Determine the matching strategy based on route complexity
    // Use complex matching for routes with options, catch-all, or optional positional params
    if (route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams)
    {
      EmitComplexMatch(sb, route, routeIndex, services, behaviors, customConverters, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix);
    }
    else
    {
      EmitSimpleMatch(sb, route, routeIndex, services, behaviors, customConverters, loggerFactoryFieldName, useRuntimeDI, runtimeDISuffix);
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits simple pattern matching using C# list patterns.
  /// Used for routes with only literals and required parameters.
  /// </summary>
  private static void EmitSimpleMatch(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<CustomConverterDefinition> customConverters,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix)
  {
    string pattern = BuildListPattern(route, routeIndex);

    sb.AppendLine($"    if (routeArgs is {pattern})");
    sb.AppendLine("    {");

    // Emit variable aliases (from route-unique names to handler-expected names)
    EmitVariableAliases(sb, route, routeIndex, indent: 6);

    // Emit type conversions for typed parameters (emits error and returns 1 on failure)
    EmitTypeConversions(sb, route, routeIndex, customConverters, indent: 6);

    // Emit handler invocation (wrapped with behaviors if any)
    if (behaviors.Length > 0)
    {
      // For endpoints (Command), the command is created by BehaviorEmitter before the pipeline
      bool commandCreatedByBehavior = route.Handler.HandlerKind == HandlerKind.Command;

      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8, commandAlreadyCreated: commandCreatedByBehavior, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix));

      sb.AppendLine("      return 0;");
    }
    else
    {
      HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 6, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix);
      sb.AppendLine("      return 0;");
    }

    sb.AppendLine("    }");
  }

  /// <summary>
  /// Emits complex matching logic for routes with options or catch-all parameters.
  /// Uses a two-pass approach:
  /// 1. First pass: Extract options and their values, tracking consumed indices
  /// 2. Second pass: Build positional args array, match literals, extract parameters
  /// This allows options to appear anywhere (interleaved with positional args).
  /// </summary>
  private static void EmitComplexMatch(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<CustomConverterDefinition> customConverters,
    string? loggerFactoryFieldName,
    bool useRuntimeDI,
    string runtimeDISuffix)
  {
    // Calculate minimum required positional args (positional match segments + required non-catch-all params)
    // PositionalMatchSegments includes group prefix literals, pattern literals, and end-of-options
    int minPositionalArgs = route.PositionalMatchSegments.Count() + route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll);

    // Initial length check (minimum possible args)
    sb.AppendLine($"    if (routeArgs.Length >= {minPositionalArgs})");
    sb.AppendLine("    {");

    // Emit the set of all option forms for this route (used to distinguish options from values)
    EmitOptionFormsSet(sb, route, routeIndex);

    // Find end-of-options marker (--)
    sb.AppendLine($"      int __endOfOptions_{routeIndex} = global::System.Array.IndexOf(routeArgs, \"--\");");
    sb.AppendLine($"      if (__endOfOptions_{routeIndex} < 0) __endOfOptions_{routeIndex} = routeArgs.Length;");

    // Track consumed indices (options and their values)
    sb.AppendLine($"      global::System.Collections.Generic.HashSet<int> __consumed_{routeIndex} = [];");

    // First pass: Extract all options (with index tracking)
    EmitOptionParsingWithIndexTracking(sb, route, routeIndex, customConverters);

    // Build positional args array (excluding consumed indices; -- is included only if route expects it)
    EmitPositionalArrayConstruction(sb, route, routeIndex);

    // Check minimum positional args
    sb.AppendLine($"      if (__positionalArgs_{routeIndex}.Length < {minPositionalArgs}) goto route_skip_{routeIndex};");

    // Emit matching and parameter extraction in one pass through ALL segments
    // This keeps positionalIndex correctly aligned for interleaved patterns
    // (e.g., "execute {script} -- {*args}" where param appears before separator)
    int positionalIndex = 0;

    // First, handle group prefix literals
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      foreach (string word in route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        sb.AppendLine(
          $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"{EscapeString(word)}\") goto route_skip_{routeIndex};");
        positionalIndex++;
      }
    }

    // Then iterate through ALL segments, matching AND extracting at correct positions
    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          sb.AppendLine(
            $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"{EscapeString(literal.Value)}\") goto route_skip_{routeIndex};");
          positionalIndex++;
          break;

        case ParameterDefinition param when param.IsCatchAll:
          // Catch-all: slice from current position to end
          string catchAllVar = (param.HasTypeConstraint || param.IsCatchAll)
            ? $"__{param.CamelCaseName}_{routeIndex}"
            : param.CamelCaseName;
          sb.AppendLine(
            $"      string[] {catchAllVar} = __positionalArgs_{routeIndex}[{positionalIndex}..];");
          // Don't increment - catch-all consumes remaining
          break;

        case ParameterDefinition param when param.IsOptional:
          // Optional param: conditional extraction
          string optVar = param.HasTypeConstraint
            ? $"__{param.CamelCaseName}_{routeIndex}"
            : param.CamelCaseName;
          sb.AppendLine(
            $"      string? {optVar} = __positionalArgs_{routeIndex}.Length > {positionalIndex} ? __positionalArgs_{routeIndex}[{positionalIndex}] : null;");
          positionalIndex++;
          break;

        case ParameterDefinition param:
          // Required param: direct extraction
          string reqVar = param.HasTypeConstraint
            ? $"__{param.CamelCaseName}_{routeIndex}"
            : param.CamelCaseName;
          sb.AppendLine(
            $"      string {reqVar} = __positionalArgs_{routeIndex}[{positionalIndex}];");
          positionalIndex++;
          break;

        case EndOfOptionsSeparatorDefinition:
          sb.AppendLine(
            $"      if (__positionalArgs_{routeIndex}[{positionalIndex}] != \"--\") goto route_skip_{routeIndex};");
          positionalIndex++;
          break;

          // OptionDefinition: skip (handled by EmitOptionParsingWithIndexTracking)
      }
    }

    // Emit type conversions for typed parameters
    EmitTypeConversions(sb, route, routeIndex, customConverters, indent: 6);

    // Emit handler invocation (wrapped with behaviors if any)
    if (behaviors.Length > 0)
    {
      // For endpoints (Command), the command is created by BehaviorEmitter before the pipeline
      bool commandCreatedByBehavior = route.Handler.HandlerKind == HandlerKind.Command;

      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8, commandAlreadyCreated: commandCreatedByBehavior, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix));

      sb.AppendLine("      return 0;");
    }
    else
    {
      HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 6, loggerFactoryFieldName: loggerFactoryFieldName, useRuntimeDI: useRuntimeDI, runtimeDISuffix: runtimeDISuffix);
      sb.AppendLine("      return 0;");
    }

    sb.AppendLine("    }");
    sb.AppendLine($"    route_skip_{routeIndex}:;");
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
  /// Emits code to build the positional args array from routeArgs,
  /// excluding consumed indices (options and their values).
  /// The -- separator is included only if the route explicitly expects it.
  /// </summary>
  private static void EmitPositionalArrayConstruction(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    sb.AppendLine($"      global::System.Collections.Generic.List<string> __positionalList_{routeIndex} = [];");
    sb.AppendLine("      for (int __i = 0; __i < routeArgs.Length; __i++)");
    sb.AppendLine("      {");

    // Only skip -- from positional args if the route does NOT explicitly include it
    // If the route has EndOfOptionsSeparatorDefinition, we need -- in the positional array to match against
    if (!route.HasEndOfOptions)
    {
      sb.AppendLine($"        if (__i == __endOfOptions_{routeIndex} && routeArgs[__i] == \"--\") continue;");
    }

    sb.AppendLine($"        if (__consumed_{routeIndex}.Contains(__i)) continue;");
    sb.AppendLine($"        __positionalList_{routeIndex}.Add(routeArgs[__i]);");
    sb.AppendLine("      }");
    sb.AppendLine($"      string[] __positionalArgs_{routeIndex} = [.. __positionalList_{routeIndex}];");
  }

  /// <summary>
  /// Emits code to extract parameter values from the positional args array.
  /// </summary>
  private static void EmitParameterExtractionFromPositionalArgs(StringBuilder sb, RouteDefinition route, int routeIndex, int startIndex)
  {
    int paramIndex = startIndex;
    foreach (ParameterDefinition param in route.Parameters)
    {
      string varName = (param.HasTypeConstraint || param.IsCatchAll)
        ? $"__{param.CamelCaseName}_{routeIndex}"
        : param.CamelCaseName;

      if (param.IsCatchAll)
      {
        sb.AppendLine(
          $"      string[] {varName} = __positionalArgs_{routeIndex}[{paramIndex}..];");
      }
      else if (param.IsOptional)
      {
        sb.AppendLine(
          $"      string? {varName} = __positionalArgs_{routeIndex}.Length > {paramIndex} ? __positionalArgs_{routeIndex}[{paramIndex}] : null;");
        paramIndex++;
      }
      else
      {
        sb.AppendLine(
          $"      string {varName} = __positionalArgs_{routeIndex}[{paramIndex}];");
        paramIndex++;
      }
    }
  }

  /// <summary>
  /// Emits option parsing with index tracking for the two-pass approach.
  /// Extracts options and their values, marking consumed indices.
  /// Supports both space-separated (--opt value) and equals syntax (--opt=value).
  /// </summary>
  private static void EmitOptionParsingWithIndexTracking(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters)
  {
    foreach (OptionDefinition option in route.Options)
    {
      if (option.IsFlag)
      {
        EmitFlagParsingWithIndexTracking(sb, option, routeIndex);
      }
      else if (option.IsRepeated)
      {
        EmitRepeatedValueOptionParsingWithIndexTracking(sb, option, routeIndex, customConverters, route);
      }
      else
      {
        EmitValueOptionParsingWithIndexTracking(sb, option, routeIndex, customConverters, route);
      }
    }
  }

  /// <summary>
  /// Emits code to parse a boolean flag option with index tracking.
  /// </summary>
  private static void EmitFlagParsingWithIndexTracking(StringBuilder sb, OptionDefinition option, int routeIndex)
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

    sb.AppendLine($"      bool {escapedVarName} = false;");
    sb.AppendLine($"      for (int __i = 0; __i < __endOfOptions_{routeIndex}; __i++)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if ({condition})");
    sb.AppendLine("        {");
    sb.AppendLine($"          {escapedVarName} = true;");
    sb.AppendLine($"          __consumed_{routeIndex}.Add(__i);");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
  }

  /// <summary>
  /// Emits code to parse an option with a value, tracking consumed indices.
  /// Supports both --opt value and --opt=value syntax.
  /// </summary>
  private static void EmitValueOptionParsingWithIndexTracking(
    StringBuilder sb,
    OptionDefinition option,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    RouteDefinition route)
  {
    string varName = option.ParameterName ?? ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
    string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
    string flagFoundVar = $"__{varName}_flagFound_{routeIndex}";

    // For typed options, extract to a temp string first, then convert
    // Also check if the handler parameter is an enum type (implicit type inference)
    string optionName = option.LongForm ?? option.ShortForm!;
    bool isEnumType = route.Handler.Parameters
      .Any(p => string.Equals(p.SourceName, optionName, StringComparison.OrdinalIgnoreCase) && p.IsEnumType);
    bool needsConversion = option.TypeConstraint is not null || isEnumType;
    string rawVarName = needsConversion ? $"__{varName}_raw" : escapedVarName;

    sb.AppendLine($"      bool {flagFoundVar} = false;");
    sb.AppendLine($"      string? {rawVarName} = null;");
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
      sb.AppendLine($"      if (!{flagFoundVar}) goto route_skip_{routeIndex};");
      if (!option.ParameterIsOptional)
      {
        sb.AppendLine($"      if ({rawVarName} is null) goto route_skip_{routeIndex};");
      }
    }
    else
    {
      if (!option.ParameterIsOptional)
      {
        sb.AppendLine($"      if ({flagFoundVar} && {rawVarName} is null) goto route_skip_{routeIndex};");
      }
    }

    // Emit type conversion if needed
    if (needsConversion)
    {
      EmitOptionTypeConversion(sb, option, escapedVarName, rawVarName, routeIndex, customConverters, route);
    }
  }

  /// <summary>
  /// Emits code to parse a repeated option with index tracking.
  /// Supports both --opt value and --opt=value syntax for each occurrence.
  /// </summary>
  private static void EmitRepeatedValueOptionParsingWithIndexTracking(
    StringBuilder sb,
    OptionDefinition option,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    RouteDefinition route)
  {
    // Note: route parameter is available for future enum support in repeated options
    _ = route;
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
    EmitRepeatedOptionTypeConversion(sb, option, escapedVarName, listVarName, routeIndex, customConverters);
  }

  /// <summary>
  /// Emits variable aliases from route-unique names to handler-expected names.
  /// This is needed for untyped string parameters.
  /// </summary>
  private static void EmitVariableAliases(StringBuilder sb, RouteDefinition route, int routeIndex, int indent)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterDefinition param in route.Parameters)
    {
      // Skip typed parameters - they get aliases via type conversion
      if (param.HasTypeConstraint)
        continue;

      string varName = param.CamelCaseName;
      string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
      string uniqueVarName = $"__{varName}_{routeIndex}";

      // Create alias from unique name to handler-expected name
      sb.AppendLine(
        $"{indentStr}string {escapedVarName} = {uniqueVarName};");
    }
  }

  /// <summary>
  /// Emits type conversion code for typed parameters.
  /// Pattern matching captures typed params with route-unique names.
  /// This method creates the properly typed variable with the original name.
  /// Uses TryParse for graceful error handling - route skips on conversion failure.
  /// </summary>
  private static void EmitTypeConversions(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    int indent)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterDefinition param in route.Parameters)
    {
      if (!param.HasTypeConstraint)
        continue;

      string varName = param.CamelCaseName;
      string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
      string uniqueVarName = $"__{varName}_{routeIndex}";

      // Normalize type constraint: strip '?' suffix to get base type
      string typeConstraint = param.TypeConstraint ?? "";
      string baseType = typeConstraint.EndsWith('?') ? typeConstraint[..^1] : typeConstraint;

      // Handle catchall parameters differently - they're string[] not string
      if (param.IsCatchAll)
      {
        // Skip string type - catchall is already string[], no conversion needed
        if (baseType.Equals("string", StringComparison.OrdinalIgnoreCase))
          continue;

        EmitCatchAllTypeConversion(sb, param, escapedVarName, uniqueVarName, routeIndex, baseType, indentStr);
        continue;
      }

      // Use shared type conversion mapping with TryParse for graceful error handling
      (string ClrType, string TryParseCondition)? conversion = TypeConversionMap.GetBuiltInTryConversion(baseType, uniqueVarName, escapedVarName);

      if (conversion is var (clrType, tryParseCondition))
      {
        if (param.IsOptional)
        {
          // Optional param: declare as nullable, check for null before parsing, error on parse failure
          // Need a temp variable for TryParse's out parameter since we need nullable
          string tempVarName = $"__{varName}_parsed_{routeIndex}";
          string tryParseWithTemp = TypeConversionMap.GetBuiltInTryConversion(baseType, uniqueVarName, tempVarName)!.Value.TryParseCondition;
          sb.AppendLine($"{indentStr}{clrType}? {escapedVarName} = null;");
          sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
          sb.AppendLine($"{indentStr}{{");
          sb.AppendLine($"{indentStr}  {clrType} {tempVarName};");
          sb.AppendLine($"{indentStr}  if (!{tryParseWithTemp})");
          sb.AppendLine($"{indentStr}  {{");
          sb.AppendLine($"{indentStr}    app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: {baseType}\");");
          sb.AppendLine($"{indentStr}    return 1;");
          sb.AppendLine($"{indentStr}  }}");
          sb.AppendLine($"{indentStr}  {escapedVarName} = {tempVarName};");
          sb.AppendLine($"{indentStr}}}");
        }
        else
        {
          // Required param: TryParse with error message on failure
          sb.AppendLine($"{indentStr}{clrType} {escapedVarName};");
          sb.AppendLine($"{indentStr}if (!{tryParseCondition})");
          sb.AppendLine($"{indentStr}{{");
          sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: {baseType}\");");
          sb.AppendLine($"{indentStr}  return 1;");
          sb.AppendLine($"{indentStr}}}");
        }
      }
      else
      {
        // Special handling for types without TryParse (Uri, FileInfo, DirectoryInfo)
        // These need different patterns: Uri.TryCreate or constructor with try/catch

        // Check for Uri type - uses TryCreate instead of TryParse
        if (baseType.Equals("uri", StringComparison.OrdinalIgnoreCase))
        {
          if (param.IsOptional)
          {
            string tempVarName = $"__{varName}_parsed_{routeIndex}";
            sb.AppendLine($"{indentStr}global::System.Uri? {escapedVarName} = null;");
            sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  if (!global::System.Uri.TryCreate({uniqueVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {tempVarName}))");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: Uri\");");
            sb.AppendLine($"{indentStr}    return 1;");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}  {escapedVarName} = {tempVarName};");
            sb.AppendLine($"{indentStr}}}");
          }
          else
          {
            sb.AppendLine($"{indentStr}if (!global::System.Uri.TryCreate({uniqueVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {escapedVarName}) || {escapedVarName} is null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: Uri\");");
            sb.AppendLine($"{indentStr}  return 1;");
            sb.AppendLine($"{indentStr}}}");
          }

          continue;
        }

        // Check for FileInfo type - uses constructor with try/catch
        if (baseType.Equals("fileinfo", StringComparison.OrdinalIgnoreCase))
        {
          if (param.IsOptional)
          {
            sb.AppendLine($"{indentStr}global::System.IO.FileInfo? {escapedVarName} = null;");
            sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  try");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    {escapedVarName} = new global::System.IO.FileInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}  catch (global::System.ArgumentException)");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: FileInfo\");");
            sb.AppendLine($"{indentStr}    return 1;");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}}}");
          }
          else
          {
            sb.AppendLine($"{indentStr}global::System.IO.FileInfo {escapedVarName};");
            sb.AppendLine($"{indentStr}try");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  {escapedVarName} = new global::System.IO.FileInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}}}");
            sb.AppendLine($"{indentStr}catch (global::System.ArgumentException)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: FileInfo\");");
            sb.AppendLine($"{indentStr}  return 1;");
            sb.AppendLine($"{indentStr}}}");
          }

          continue;
        }

        // Check for DirectoryInfo type - uses constructor with try/catch
        if (baseType.Equals("directoryinfo", StringComparison.OrdinalIgnoreCase))
        {
          if (param.IsOptional)
          {
            sb.AppendLine($"{indentStr}global::System.IO.DirectoryInfo? {escapedVarName} = null;");
            sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  try");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    {escapedVarName} = new global::System.IO.DirectoryInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}  catch (global::System.ArgumentException)");
            sb.AppendLine($"{indentStr}  {{");
            sb.AppendLine($"{indentStr}    app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: DirectoryInfo\");");
            sb.AppendLine($"{indentStr}    return 1;");
            sb.AppendLine($"{indentStr}  }}");
            sb.AppendLine($"{indentStr}}}");
          }
          else
          {
            sb.AppendLine($"{indentStr}global::System.IO.DirectoryInfo {escapedVarName};");
            sb.AppendLine($"{indentStr}try");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  {escapedVarName} = new global::System.IO.DirectoryInfo({uniqueVarName});");
            sb.AppendLine($"{indentStr}}}");
            sb.AppendLine($"{indentStr}catch (global::System.ArgumentException)");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. Expected: DirectoryInfo\");");
            sb.AppendLine($"{indentStr}  return 1;");
            sb.AppendLine($"{indentStr}}}");
          }

          continue;
        }

        // Try to find a custom type converter for this constraint
        // Match by:
        // 1. Target type name (e.g., "EmailAddress")
        // 2. Simple target type name (e.g., "MyApp.Types.EmailAddress" -> "EmailAddress")
        // 3. Constraint alias (e.g., "email")
        // 4. Convention: ConverterTypeName minus "Converter" suffix (e.g., "EmailAddressConverter" -> "EmailAddress")
        // DEBUG: Emit lookup info
        sb.AppendLine($"{indentStr}// DEBUG: Looking for converter for baseType='{baseType}', customConverters.Length={customConverters.Length}");
        foreach (CustomConverterDefinition c in customConverters)
        {
          sb.AppendLine($"{indentStr}// DEBUG:   Converter: {c.ConverterTypeName}, TargetTypeName={c.TargetTypeName}, Alias={c.ConstraintAlias}, DerivedTarget={GetTargetTypeFromConverterName(c.ConverterTypeName)}");
        }

        CustomConverterDefinition? converter = customConverters.FirstOrDefault(c =>
          string.Equals(c.TargetTypeName, baseType, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(GetSimpleTypeName(c.TargetTypeName), baseType, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(c.ConstraintAlias, baseType, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(GetTargetTypeFromConverterName(c.ConverterTypeName), baseType, StringComparison.OrdinalIgnoreCase));

        if (converter is not null)
        {
          // Generate custom converter instantiation and TryConvert call
          EmitCustomTypeConversion(sb, param, converter, escapedVarName, uniqueVarName, routeIndex, indentStr);
        }
        else
        {
          // Check if the handler parameter is an enum type
          // Match by lowercase name (route segment name matches handler parameter name)
          ParameterBinding? handlerParam = route.Handler.Parameters
            .FirstOrDefault(p => string.Equals(p.SourceName, param.Name, StringComparison.OrdinalIgnoreCase) && p.IsEnumType);

          if (handlerParam is not null)
          {
            // Generate EnumTypeConverter<T> code for enum types
            EmitEnumTypeConversion(sb, param, handlerParam.ParameterTypeName, escapedVarName, uniqueVarName, routeIndex, indentStr);
          }
          else if (param.ResolvedClrTypeName is not null)
          {
            // No converter found - emit warning comment
            sb.AppendLine(
              $"{indentStr}// WARNING: No converter found for type constraint '{baseType}'");
            sb.AppendLine(
              $"{indentStr}// Register a converter with: builder.AddTypeConverter<YourConverter>();");
          }
        }
      }
    }
  }

  /// <summary>
  /// Emits type conversion code for a typed catch-all parameter.
  /// Catch-all parameters are string[] and need to be converted to typed arrays.
  /// </summary>
  private static void EmitCatchAllTypeConversion(
    StringBuilder sb,
    ParameterDefinition param,
    string varName,
    string uniqueVarName,
    int routeIndex,
    string baseType,
    string indentStr)
  {
    // Get the CLR type for this constraint
    (string ClrType, string _)? conversion = TypeConversionMap.GetBuiltInTryConversion(baseType, "x", "y");

    if (conversion is var (clrType, _))
    {
      // Get the parse expression for this type
      string parseExpr = GetParseExpression(baseType, "__s");

      // Emit array conversion with error handling
      sb.AppendLine($"{indentStr}{clrType}[] {varName};");
      sb.AppendLine($"{indentStr}try");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  {varName} = {uniqueVarName}.Select(__s => {parseExpr}).ToArray();");
      sb.AppendLine($"{indentStr}}}");
      sb.AppendLine($"{indentStr}catch (global::System.FormatException)");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value in '{param.Name}'. Expected: {baseType}[]\");");
      sb.AppendLine($"{indentStr}  return 1;");
      sb.AppendLine($"{indentStr}}}");
    }
    else
    {
      // Unknown type - emit as string[] (no conversion needed, just alias)
      sb.AppendLine($"{indentStr}string[] {varName} = {uniqueVarName};");
    }
  }

  /// <summary>
  /// Emits code for custom type conversion using a registered converter.
  /// Generates converter instantiation, TryConvert call, and error handling.
  /// </summary>
  private static void EmitCustomTypeConversion(
    StringBuilder sb,
    ParameterDefinition param,
    CustomConverterDefinition converter,
    string escapedVarName,
    string uniqueVarName,
    int routeIndex,
    string indentStr)
  {
    string converterVarName = $"__converter_{param.CamelCaseName}_{routeIndex}";
    string tempVarName = $"__temp_{param.CamelCaseName}_{routeIndex}";

    // Use the converter's target type (resolved via semantic model)
    // For non-generic converters, derive target type from converter name by convention
    string targetType = !string.IsNullOrEmpty(converter.TargetTypeName)
      ? converter.TargetTypeName
      : $"global::{GetTargetTypeFromConverterName(converter.ConverterTypeName)}";

    if (param.IsOptional)
    {
      // Optional param: check for null before conversion
      sb.AppendLine($"{indentStr}{targetType}? {escapedVarName} = null;");
      sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"{indentStr}  if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}  {{");
      sb.AppendLine($"{indentStr}    app.Terminal.WriteLine($\"Error: Invalid {GetSimpleTypeName(targetType)} value for parameter '{param.Name}': '{{{uniqueVarName}}}'\");");
      sb.AppendLine($"{indentStr}    return 1;");
      sb.AppendLine($"{indentStr}  }}");
      sb.AppendLine($"{indentStr}  {escapedVarName} = ({targetType}){tempVarName}!;");
      sb.AppendLine($"{indentStr}}}");
    }
    else
    {
      // Required param: direct conversion with error handling
      sb.AppendLine($"{indentStr}var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"{indentStr}if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid {GetSimpleTypeName(targetType)} value for parameter '{param.Name}': '{{{uniqueVarName}}}'\");");
      sb.AppendLine($"{indentStr}  return 1;");
      sb.AppendLine($"{indentStr}}}");
      sb.AppendLine($"{indentStr}{targetType} {escapedVarName} = ({targetType}){tempVarName}!;");
    }
  }

  /// <summary>
  /// Emits code for enum type conversion using EnumTypeConverter.
  /// Generates converter instantiation, TryConvert call, and error handling with valid values.
  /// </summary>
  private static void EmitEnumTypeConversion(
    StringBuilder sb,
    ParameterDefinition param,
    string enumTypeName,
    string escapedVarName,
    string uniqueVarName,
    int routeIndex,
    string indentStr)
  {
    string converterVarName = $"__enumConverter_{param.CamelCaseName}_{routeIndex}";
    string tempVarName = $"__temp_{param.CamelCaseName}_{routeIndex}";

    if (param.IsOptional)
    {
      // Optional param: check for null before conversion
      sb.AppendLine($"{indentStr}{enumTypeName}? {escapedVarName} = null;");
      sb.AppendLine($"{indentStr}if ({uniqueVarName} is not null)");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{enumTypeName}>();");
      sb.AppendLine($"{indentStr}  if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}  {{");
      sb.AppendLine($"{indentStr}    app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. {{{converterVarName}.GetValidValuesMessage()}}\");");
      sb.AppendLine($"{indentStr}    return 1;");
      sb.AppendLine($"{indentStr}  }}");
      sb.AppendLine($"{indentStr}  {escapedVarName} = ({enumTypeName}){tempVarName}!;");
      sb.AppendLine($"{indentStr}}}");
    }
    else
    {
      // Required param: direct conversion with error handling
      sb.AppendLine($"{indentStr}var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{enumTypeName}>();");
      sb.AppendLine($"{indentStr}if (!{converterVarName}.TryConvert({uniqueVarName}, out object? {tempVarName}))");
      sb.AppendLine($"{indentStr}{{");
      sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{uniqueVarName}}}' for parameter '{param.Name}'. {{{converterVarName}.GetValidValuesMessage()}}\");");
      sb.AppendLine($"{indentStr}  return 1;");
      sb.AppendLine($"{indentStr}}}");
      sb.AppendLine($"{indentStr}{enumTypeName} {escapedVarName} = ({enumTypeName}){tempVarName}!;");
    }
  }

  /// <summary>
  /// Gets the simple type name from a fully qualified type name.
  /// Handles both dot notation (MyApp.Types.EmailAddress) and global:: prefix (global::EmailAddress).
  /// E.g., "MyApp.Types.EmailAddress" -> "EmailAddress"
  /// E.g., "global::EmailAddress" -> "EmailAddress"
  /// E.g., "global::MyApp.Types.EmailAddress" -> "EmailAddress"
  /// </summary>
  private static string GetSimpleTypeName(string fullyQualifiedName)
  {
    // First strip global:: prefix if present
    const string globalPrefix = "global::";
    if (fullyQualifiedName.StartsWith(globalPrefix, StringComparison.Ordinal))
    {
      fullyQualifiedName = fullyQualifiedName[globalPrefix.Length..];
    }

    // Then get the part after the last dot
    int lastDot = fullyQualifiedName.LastIndexOf('.');
    return lastDot >= 0 ? fullyQualifiedName[(lastDot + 1)..] : fullyQualifiedName;
  }

  /// <summary>
  /// Extracts the target type name from a converter type name by convention.
  /// E.g., "EmailAddressConverter" -> "EmailAddress"
  /// E.g., "MyApp.Converters.HexColorConverter" -> "HexColor"
  /// </summary>
  private static string GetTargetTypeFromConverterName(string converterTypeName)
  {
    string simpleName = GetSimpleTypeName(converterTypeName);
    const string suffix = "Converter";
    if (simpleName.EndsWith(suffix, StringComparison.Ordinal) && simpleName.Length > suffix.Length)
    {
      return simpleName[..^suffix.Length];
    }

    return simpleName;
  }

  /// <summary>
  /// Builds a C# list pattern string for simple matching.
  /// Uses route-unique variable names to avoid conflicts between routes.
  /// </summary>
  private static string BuildListPattern(RouteDefinition route, int routeIndex)
  {
    List<string> parts = [];

    // Prepend group prefix literals if present
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      foreach (string word in route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        parts.Add($"\"{EscapeString(word)}\"");
      }
    }

    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          parts.Add($"\"{EscapeString(literal.Value)}\"");
          break;

        case ParameterDefinition param when param.IsOptional:
          // Optional parameters use route-unique variable names
          string optVarName = $"__{param.CamelCaseName}_{routeIndex}";
          parts.Add($"var {optVarName}");
          break;

        case ParameterDefinition param:
          // Required parameters use route-unique variable names
          string varName = $"__{param.CamelCaseName}_{routeIndex}";
          parts.Add($"var {varName}");
          break;
      }
    }

    return $"[{string.Join(", ", parts)}]";
  }

  /// <summary>
  /// Emits type conversion code for a repeated option.
  /// Converts List&lt;string&gt; to typed array using Select().ToArray().
  /// </summary>
  private static void EmitRepeatedOptionTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    string varName,
    string listVarName,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters)
  {
    string typeConstraint = option.TypeConstraint ?? "";
    string baseType = typeConstraint.EndsWith('?') ? typeConstraint[..^1] : typeConstraint;

    if (string.IsNullOrEmpty(baseType))
    {
      // No type constraint - just convert to string[]
      sb.AppendLine($"      string[] {varName} = {listVarName}.ToArray();");
      return;
    }

    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";

    // Use shared type conversion mapping
    (string ClrType, string TryParseCondition)? conversion = TypeConversionMap.GetBuiltInTryConversion(baseType, "__item", "__parsed");

    if (conversion is var (clrType, _))
    {
      // Get the parse expression for this type
      string parseExpr = GetParseExpression(baseType, "__s");

      // Emit array conversion with error handling
      sb.AppendLine($"      {clrType}[] {varName};");
      sb.AppendLine("      try");
      sb.AppendLine("      {");
      sb.AppendLine($"        {varName} = {listVarName}.Select(__s => {parseExpr}).ToArray();");
      sb.AppendLine("      }");
      sb.AppendLine("      catch (global::System.FormatException)");
      sb.AppendLine("      {");
      sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value in option '{optionDisplay}'. Expected: {baseType}\");");
      sb.AppendLine("        return 1;");
      sb.AppendLine("      }");
    }
    else
    {
      // Try custom converter - for now just fall back to string[]
      // TODO: Add custom converter support for arrays
      sb.AppendLine($"      string[] {varName} = {listVarName}.ToArray();");
    }
  }

  /// <summary>
  /// Gets the parse expression for a given type.
  /// </summary>
  private static string GetParseExpression(string baseType, string varName)
  {
    return baseType.ToLowerInvariant() switch
    {
      "int" => $"int.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "long" => $"long.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "short" => $"short.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "byte" => $"byte.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "double" => $"double.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "float" => $"float.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "decimal" => $"decimal.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "bool" => $"bool.Parse({varName})",
      "datetime" => $"global::System.DateTime.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)",
      "guid" => $"global::System.Guid.Parse({varName})",
      _ => varName // Unknown type - return as-is (string)
    };
  }

  /// <summary>
  /// Emits type conversion code for a typed option.
  /// Uses TryParse with clear error messages on conversion failure.
  /// </summary>
  private static void EmitOptionTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    string varName,
    string rawVarName,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters,
    RouteDefinition route)
  {
    string typeConstraint = option.TypeConstraint ?? "";
    string baseType = typeConstraint.EndsWith('?') ? typeConstraint[..^1] : typeConstraint;
    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";

    // Use shared type conversion mapping with TryParse for graceful error handling
    bool isNullableValueType = typeConstraint.EndsWith('?');
    (string ClrType, string TryParseCondition)? conversion = TypeConversionMap.GetBuiltInTryConversion(baseType, rawVarName, varName);

    if (conversion is var (clrType, tryParseCondition))
    {
      // Handle nullable value types (long?, int?, etc.) - declare as nullable and only parse if value provided
      if (isNullableValueType)
      {
        string tempVarName = $"__{varName}_parsed_{routeIndex}";
        sb.AppendLine($"      {clrType}? {varName} = null;");
        sb.AppendLine($"      if ({rawVarName} is not null)");
        sb.AppendLine("      {");
        sb.AppendLine($"        if ({tryParseCondition.Replace($"out {varName}", $"out {clrType} {tempVarName}", StringComparison.Ordinal)})");
        sb.AppendLine("        {");
        sb.AppendLine($"          {varName} = {tempVarName};");
        sb.AppendLine("        }");
        sb.AppendLine("        else");
        sb.AppendLine("        {");
        sb.AppendLine($"          app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: {baseType}\");");
        sb.AppendLine("          return 1;");
        sb.AppendLine("        }");
        sb.AppendLine("      }");
      }
      else
      {
        // Non-nullable value types - existing logic
        // Determine the default value to use
        string defaultValue = option.DefaultValueLiteral ?? "default";

        if (option.ParameterIsOptional || option.DefaultValueLiteral is not null)
        {
          // Optional option value OR has default: declare with default, error only if value provided but invalid
          sb.AppendLine($"      {clrType} {varName} = {defaultValue};");
          sb.AppendLine($"      if ({rawVarName} is not null && !{tryParseCondition})");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: {baseType}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }
        else
        {
          // Required option value (no default): TryParse with error message on failure
          sb.AppendLine($"      {clrType} {varName} = default;");
          sb.AppendLine($"      if (!{tryParseCondition})");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName} ?? \"(missing)\"}}' for option '{optionDisplay}'. Expected: {baseType}\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }
      }
    }
    else
    {
      // Special handling for types without TryParse (Uri, FileInfo, DirectoryInfo)
      // These need different patterns: Uri.TryCreate or constructor with try/catch

      // Check for Uri type - uses TryCreate instead of TryParse
      if (baseType.Equals("uri", StringComparison.OrdinalIgnoreCase))
      {
        if (option.ParameterIsOptional)
        {
          string tempVarName = $"__{varName}_parsed_{routeIndex}";
          sb.AppendLine($"      global::System.Uri? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is not null)");
          sb.AppendLine("      {");
          sb.AppendLine($"        if (!global::System.Uri.TryCreate({rawVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {tempVarName}))");
          sb.AppendLine("        {");
          sb.AppendLine($"          app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: Uri\");");
          sb.AppendLine("          return 1;");
          sb.AppendLine("        }");
          sb.AppendLine($"        {varName} = {tempVarName};");
          sb.AppendLine("      }");
        }
        else
        {
          sb.AppendLine($"      if (!global::System.Uri.TryCreate({rawVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {varName}) || {varName} is null)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName} ?? \"(missing)\"}}' for option '{optionDisplay}'. Expected: Uri\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }

        return;
      }

      // Check for FileInfo type - uses constructor with try/catch
      if (baseType.Equals("fileinfo", StringComparison.OrdinalIgnoreCase))
      {
        if (option.ParameterIsOptional)
        {
          sb.AppendLine($"      global::System.IO.FileInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is not null)");
          sb.AppendLine("      {");
          sb.AppendLine("        try");
          sb.AppendLine("        {");
          sb.AppendLine($"          {varName} = new global::System.IO.FileInfo({rawVarName});");
          sb.AppendLine("        }");
          sb.AppendLine("        catch (global::System.ArgumentException)");
          sb.AppendLine("        {");
          sb.AppendLine($"          app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: FileInfo\");");
          sb.AppendLine("          return 1;");
          sb.AppendLine("        }");
          sb.AppendLine("      }");
        }
        else
        {
          sb.AppendLine($"      global::System.IO.FileInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is null)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Missing value for option '{optionDisplay}'. Expected: FileInfo\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
          sb.AppendLine("      try");
          sb.AppendLine("      {");
          sb.AppendLine($"        {varName} = new global::System.IO.FileInfo({rawVarName});");
          sb.AppendLine("      }");
          sb.AppendLine("      catch (global::System.ArgumentException)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: FileInfo\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }

        return;
      }

      // Check for DirectoryInfo type - uses constructor with try/catch
      if (baseType.Equals("directoryinfo", StringComparison.OrdinalIgnoreCase))
      {
        if (option.ParameterIsOptional)
        {
          sb.AppendLine($"      global::System.IO.DirectoryInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is not null)");
          sb.AppendLine("      {");
          sb.AppendLine("        try");
          sb.AppendLine("        {");
          sb.AppendLine($"          {varName} = new global::System.IO.DirectoryInfo({rawVarName});");
          sb.AppendLine("        }");
          sb.AppendLine("        catch (global::System.ArgumentException)");
          sb.AppendLine("        {");
          sb.AppendLine($"          app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: DirectoryInfo\");");
          sb.AppendLine("          return 1;");
          sb.AppendLine("        }");
          sb.AppendLine("      }");
        }
        else
        {
          sb.AppendLine($"      global::System.IO.DirectoryInfo? {varName} = null;");
          sb.AppendLine($"      if ({rawVarName} is null)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Missing value for option '{optionDisplay}'. Expected: DirectoryInfo\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
          sb.AppendLine("      try");
          sb.AppendLine("      {");
          sb.AppendLine($"        {varName} = new global::System.IO.DirectoryInfo({rawVarName});");
          sb.AppendLine("      }");
          sb.AppendLine("      catch (global::System.ArgumentException)");
          sb.AppendLine("      {");
          sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. Expected: DirectoryInfo\");");
          sb.AppendLine("        return 1;");
          sb.AppendLine("      }");
        }

        return;
      }

      // Try to find a custom type converter for this constraint
      CustomConverterDefinition? converter = customConverters.FirstOrDefault(c =>
        string.Equals(c.TargetTypeName, baseType, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(GetSimpleTypeName(c.TargetTypeName), baseType, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(c.ConstraintAlias, baseType, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(GetTargetTypeFromConverterName(c.ConverterTypeName), baseType, StringComparison.OrdinalIgnoreCase));

      if (converter is not null)
      {
        // Generate custom converter code for option
        EmitOptionCustomTypeConversion(sb, option, converter, varName, rawVarName, routeIndex);
      }
      else
      {
        // Check if the handler parameter is an enum type
        // Match by option name (LongForm or ShortForm)
        string optionName = option.LongForm ?? option.ShortForm!;
        ParameterBinding? handlerParam = route.Handler.Parameters
          .FirstOrDefault(p =>
            string.Equals(p.SourceName, optionName, StringComparison.OrdinalIgnoreCase) &&
            p.IsEnumType);

        if (handlerParam is not null)
        {
          // Generate EnumTypeConverter<T> code for enum types
          EmitOptionEnumTypeConversion(sb, option, handlerParam.ParameterTypeName, varName, rawVarName, routeIndex);
        }
        else
        {
          // Unknown type - keep as string (fallback)
          sb.AppendLine(
            $"      string? {varName} = {rawVarName};");
        }
      }
    }
  }

  /// <summary>
  /// Emits custom type conversion code for a typed option.
  /// </summary>
  private static void EmitOptionCustomTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    CustomConverterDefinition converter,
    string varName,
    string rawVarName,
    int routeIndex)
  {
    string converterVarName = $"__converter_{varName}_{routeIndex}";
    string tempVarName = $"__temp_{varName}_{routeIndex}";

    // Use the converter's target type (resolved via semantic model)
    // For non-generic converters, derive target type from converter name by convention
    string targetType = !string.IsNullOrEmpty(converter.TargetTypeName)
      ? converter.TargetTypeName
      : $"global::{GetTargetTypeFromConverterName(converter.ConverterTypeName)}";

    if (option.ParameterIsOptional)
    {
      // Optional option: check for null before conversion
      sb.AppendLine($"      {targetType}? {varName} = null;");
      sb.AppendLine($"      if ({rawVarName} is not null)");
      sb.AppendLine("      {");
      sb.AppendLine($"        var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"        if (!{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("        {");
      sb.AppendLine($"          app.Terminal.WriteLine($\"Error: Invalid {targetType} value for option '{option.LongForm ?? option.ShortForm}': '{{{rawVarName}}}'\");");
      sb.AppendLine("          return 1;");
      sb.AppendLine("        }");
      sb.AppendLine($"        {varName} = ({targetType}){tempVarName}!;");
      sb.AppendLine("      }");
    }
    else
    {
      // Required option: direct conversion with error handling
      sb.AppendLine($"      var {converterVarName} = new {converter.ConverterTypeName}();");
      sb.AppendLine($"      if (!{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("      {");
      sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid {targetType} value for option '{option.LongForm ?? option.ShortForm}': '{{{rawVarName}}}'\");");
      sb.AppendLine("        return 1;");
      sb.AppendLine("      }");
      sb.AppendLine($"      {targetType} {varName} = ({targetType}){tempVarName}!;");
    }
  }

  /// <summary>
  /// Emits enum type conversion code for a typed option.
  /// Uses EnumTypeConverter with helpful error messages showing valid values.
  /// </summary>
  private static void EmitOptionEnumTypeConversion(
    StringBuilder sb,
    OptionDefinition option,
    string enumTypeName,
    string varName,
    string rawVarName,
    int routeIndex)
  {
    string converterVarName = $"__enumConverter_{varName}_{routeIndex}";
    string tempVarName = $"__temp_{varName}_{routeIndex}";
    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";

    // Strip nullable suffix from the type name for the converter and casts
    // EnumTypeConverter<T> requires the non-nullable enum type
    string baseEnumType = enumTypeName.EndsWith('?') ? enumTypeName[..^1] : enumTypeName;

    if (option.ParameterIsOptional || option.DefaultValueLiteral is not null)
    {
      // Optional option or has default: check for null before conversion
      sb.AppendLine($"      {baseEnumType}? {varName} = null;");
      sb.AppendLine($"      if ({rawVarName} is not null)");
      sb.AppendLine("      {");
      sb.AppendLine($"        var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{baseEnumType}>();");
      sb.AppendLine($"        if (!{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("        {");
      sb.AppendLine($"          app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName}}}' for option '{optionDisplay}'. {{{converterVarName}.GetValidValuesMessage()}}\");");
      sb.AppendLine("          return 1;");
      sb.AppendLine("        }");
      sb.AppendLine($"        {varName} = ({baseEnumType}){tempVarName}!;");
      sb.AppendLine("      }");
    }
    else
    {
      // Required option: direct conversion with error handling
      sb.AppendLine($"      var {converterVarName} = new global::TimeWarp.Nuru.EnumTypeConverter<{baseEnumType}>();");
      sb.AppendLine($"      if (!{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("      {");
      sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName} ?? \"(missing)\"}}' for option '{optionDisplay}'. {{{converterVarName}.GetValidValuesMessage()}}\");");
      sb.AppendLine("        return 1;");
      sb.AppendLine("      }");
      sb.AppendLine($"      {baseEnumType} {varName} = ({baseEnumType}){tempVarName}!;");
    }
  }

  /// <summary>
  /// Converts a string to camelCase.
  /// </summary>
  private static string ToCamelCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    // Handle kebab-case by converting to PascalCase first, then camelCase
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

  /// <summary>
  /// Escapes a string for use in C# source code.
  /// </summary>
  private static string EscapeString(string value)
  {
    return value
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal)
      .Replace("\n", "\\n", StringComparison.Ordinal)
      .Replace("\r", "\\r", StringComparison.Ordinal)
      .Replace("\t", "\\t", StringComparison.Ordinal);
  }

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
