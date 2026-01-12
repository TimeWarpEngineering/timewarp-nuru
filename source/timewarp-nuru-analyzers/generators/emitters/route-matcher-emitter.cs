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
  public static void Emit(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors = default,
    ImmutableArray<CustomConverterDefinition> customConverters = default)
  {
    // Use empty array if optional parameters not provided
    if (behaviors.IsDefault)
      behaviors = [];
    if (customConverters.IsDefault)
      customConverters = [];

    // Comment showing the route pattern
    sb.AppendLine(
      $"    // Route: {EscapeXmlComment(route.FullPattern)}");

    // Determine the matching strategy based on route complexity
    // Use complex matching for routes with options, catch-all, or optional positional params
    if (route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams)
    {
      EmitComplexMatch(sb, route, routeIndex, services, behaviors, customConverters);
    }
    else
    {
      EmitSimpleMatch(sb, route, routeIndex, services, behaviors, customConverters);
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
    ImmutableArray<CustomConverterDefinition> customConverters)
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
      // For attributed routes (Command), the command is created by BehaviorEmitter before the pipeline
      bool commandCreatedByBehavior = route.Handler.HandlerKind == HandlerKind.Command;

      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8, commandAlreadyCreated: commandCreatedByBehavior));

      sb.AppendLine("      return 0;");
    }
    else
    {
      HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 6);
      sb.AppendLine("      return 0;");
    }

    sb.AppendLine("    }");
  }

  /// <summary>
  /// Emits complex matching logic for routes with options or catch-all parameters.
  /// Uses length checks and manual parsing.
  /// </summary>
  private static void EmitComplexMatch(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors,
    ImmutableArray<CustomConverterDefinition> customConverters)
  {
    // Calculate minimum required args
    int minArgs = route.Literals.Count() + route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll);

    sb.AppendLine($"    if (routeArgs.Length >= {minArgs})");
    sb.AppendLine("    {");

    // Emit literal matching
    int literalIndex = 0;
    foreach (LiteralDefinition literal in route.Literals)
    {
      sb.AppendLine(
        $"      if (routeArgs[{literalIndex}] != \"{EscapeString(literal.Value)}\") goto route_skip_{routeIndex};");
      literalIndex++;
    }

    // Emit parameter extraction (typed params use unique var names for later conversion)
    EmitParameterExtraction(sb, route, routeIndex, literalIndex);

    // Emit type conversions for typed parameters
    EmitTypeConversions(sb, route, routeIndex, customConverters, indent: 6);

    // Emit option parsing
    EmitOptionParsing(sb, route, routeIndex, customConverters);

    // Emit handler invocation (wrapped with behaviors if any)
    if (behaviors.Length > 0)
    {
      // For attributed routes (Command), the command is created by BehaviorEmitter before the pipeline
      bool commandCreatedByBehavior = route.Handler.HandlerKind == HandlerKind.Command;

      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8, commandAlreadyCreated: commandCreatedByBehavior));

      sb.AppendLine("      return 0;");
    }
    else
    {
      HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 6);
      sb.AppendLine("      return 0;");
    }

    sb.AppendLine("    }");
    sb.AppendLine($"    route_skip_{routeIndex}:;");
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
        // Try to find a custom type converter for this constraint
        // Match by:
        // 1. Target type name (e.g., "EmailAddress")
        // 2. Simple target type name (e.g., "MyApp.Types.EmailAddress" -> "EmailAddress")
        // 3. Constraint alias (e.g., "email")
        // 4. Convention: ConverterTypeName minus "Converter" suffix (e.g., "EmailAddressConverter" -> "EmailAddress")
        CustomConverterDefinition? converter = customConverters.FirstOrDefault(c =>
          string.Equals(c.TargetTypeName, baseType, StringComparison.Ordinal) ||
          string.Equals(GetSimpleTypeName(c.TargetTypeName), baseType, StringComparison.Ordinal) ||
          string.Equals(c.ConstraintAlias, baseType, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(GetTargetTypeFromConverterName(c.ConverterTypeName), baseType, StringComparison.Ordinal));

        if (converter is not null)
        {
          // Generate custom converter instantiation and TryConvert call
          EmitCustomTypeConversion(sb, param, converter, escapedVarName, uniqueVarName, routeIndex, indentStr);
        }
        else if (param.ResolvedClrTypeName is not null)
        {
          // No converter found - emit warning comment
          sb.AppendLine(
            $"{indentStr}// WARNING: No converter found for type constraint '{baseType}' (resolved: {param.ResolvedClrTypeName})");
          sb.AppendLine(
            $"{indentStr}// Register a converter with: builder.AddTypeConverter<YourConverter>();");
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

    // Use the parameter's resolved CLR type (from handler signature) as the target type
    // Fall back to deriving from converter name if not available
    string targetType = !string.IsNullOrEmpty(param.ResolvedClrTypeName)
      ? param.ResolvedClrTypeName
      : !string.IsNullOrEmpty(converter.TargetTypeName)
        ? converter.TargetTypeName
        : GetTargetTypeFromConverterName(converter.ConverterTypeName);

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
  /// Gets the simple type name from a fully qualified type name.
  /// E.g., "MyApp.Types.EmailAddress" -> "EmailAddress"
  /// </summary>
  private static string GetSimpleTypeName(string fullyQualifiedName)
  {
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
  /// Emits code to extract parameter values from args.
  /// For typed parameters and catch-all, extracts to unique variable names to avoid collision with 'args' parameter.
  /// </summary>
  private static void EmitParameterExtraction(StringBuilder sb, RouteDefinition route, int routeIndex, int startIndex)
  {
    int paramIndex = startIndex;
    foreach (ParameterDefinition param in route.Parameters)
    {
      // For typed parameters and catch-all, use unique var name to avoid collision with 'args' parameter
      // Catch-all uses unique name because property "Args" -> "args" would shadow method parameter
      string varName = (param.HasTypeConstraint || param.IsCatchAll)
        ? $"__{param.CamelCaseName}_{routeIndex}"
        : param.CamelCaseName;

      if (param.IsCatchAll)
      {
        // Catch-all gets remaining args - uses unique variable name
        sb.AppendLine(
          $"      string[] {varName} = routeArgs[{paramIndex}..];");
      }
      else if (param.IsOptional)
      {
        // Optional parameter with bounds check - null when not provided
        sb.AppendLine(
          $"      string? {varName} = routeArgs.Length > {paramIndex} ? routeArgs[{paramIndex}] : null;");
        paramIndex++;
      }
      else
      {
        // Required parameter
        sb.AppendLine(
          $"      string {varName} = routeArgs[{paramIndex}];");
        paramIndex++;
      }
    }
  }

  /// <summary>
  /// Emits code to parse options from args.
  /// </summary>
  private static void EmitOptionParsing(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters)
  {
    foreach (OptionDefinition option in route.Options)
    {
      if (option.IsFlag)
      {
        // Boolean flag - check presence
        EmitFlagParsing(sb, option);
      }
      else
      {
        // Option with value
        EmitValueOptionParsing(sb, option, routeIndex, customConverters);
      }
    }
  }

  /// <summary>
  /// Emits code to parse a boolean flag option.
  /// </summary>
  private static void EmitFlagParsing(StringBuilder sb, OptionDefinition option)
  {
    string varName = ToCamelCase(option.LongForm ?? option.ShortForm ?? "flag");
    string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);

    string condition = (option.LongForm, option.ShortForm) switch
    {
      (not null, not null) => $"a == \"--{option.LongForm}\" || a == \"-{option.ShortForm}\"",
      (not null, null) => $"a == \"--{option.LongForm}\"",
      (null, not null) => $"a == \"-{option.ShortForm}\"",
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    sb.AppendLine(
      $"      bool {escapedVarName} = Array.Exists(routeArgs, a => {condition});");
  }

  /// <summary>
  /// Emits code to parse an option that takes a value.
  /// Handles type conversion for typed options (int, double, etc.).
  /// Properly handles:
  /// - Required flag + required value: flag and value must both be present
  /// - Required flag + optional value: flag must be present, value can be null
  /// - Optional flag + required value: if flag present, value must follow
  /// - Optional flag + optional value: flag and value both optional
  /// - Repeated options (*): collect all occurrences into an array
  /// </summary>
  private static void EmitValueOptionParsing(
    StringBuilder sb,
    OptionDefinition option,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters)
  {
    // Handle repeated options separately
    if (option.IsRepeated)
    {
      EmitRepeatedValueOptionParsing(sb, option, routeIndex, customConverters);
      return;
    }

    string varName = option.ParameterName ?? ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
    string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
    string flagFoundVar = $"__{varName}_flagFound_{routeIndex}";

    string condition = (option.LongForm, option.ShortForm) switch
    {
      (not null, not null) => $"routeArgs[__idx] == \"--{option.LongForm}\" || routeArgs[__idx] == \"-{option.ShortForm}\"",
      (not null, null) => $"routeArgs[__idx] == \"--{option.LongForm}\"",
      (null, not null) => $"routeArgs[__idx] == \"-{option.ShortForm}\"",
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    // For typed options, extract to a temp string first, then convert
    bool needsConversion = option.TypeConstraint is not null;
    string rawVarName = needsConversion ? $"__{varName}_raw" : escapedVarName;

    // Track whether the flag was found and extract value if present
    sb.AppendLine($"      bool {flagFoundVar} = false;");
    sb.AppendLine($"      string? {rawVarName} = null;");
    sb.AppendLine("      for (int __idx = 0; __idx < routeArgs.Length; __idx++)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if ({condition})");
    sb.AppendLine("        {");
    sb.AppendLine($"          {flagFoundVar} = true;");
    sb.AppendLine("          // Check if there's a value following the flag (and it's not another flag)");
    sb.AppendLine("          if (__idx + 1 < routeArgs.Length && !routeArgs[__idx + 1].StartsWith(\"-\"))");
    sb.AppendLine("          {");
    sb.AppendLine($"            {rawVarName} = routeArgs[__idx + 1];");
    sb.AppendLine("          }");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // Handle the four cases based on flag optionality and value optionality
    if (!option.IsOptional)
    {
      // Required flag: must be found
      sb.AppendLine($"      if (!{flagFoundVar}) goto route_skip_{routeIndex};");

      if (!option.ParameterIsOptional)
      {
        // Required flag + Required value: value must be present
        sb.AppendLine($"      if ({rawVarName} is null) goto route_skip_{routeIndex};");
      }
      // else: Required flag + Optional value: flag found, value can be null - OK
    }
    else
    {
      // Optional flag
      if (!option.ParameterIsOptional)
      {
        // Optional flag + Required value: if flag present, value must be present
        sb.AppendLine($"      if ({flagFoundVar} && {rawVarName} is null) goto route_skip_{routeIndex};");
      }
      // else: Optional flag + Optional value: everything optional - OK
    }

    // Emit type conversion if needed
    if (needsConversion)
    {
      EmitOptionTypeConversion(sb, option, escapedVarName, rawVarName, routeIndex, customConverters);
    }
  }

  /// <summary>
  /// Emits code to parse a repeated option that can appear multiple times.
  /// Collects all values into a List, then converts to array (with type conversion if needed).
  /// </summary>
  private static void EmitRepeatedValueOptionParsing(
    StringBuilder sb,
    OptionDefinition option,
    int routeIndex,
    ImmutableArray<CustomConverterDefinition> customConverters)
  {
    string varName = option.ParameterName ?? ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
    string escapedVarName = CSharpIdentifierUtils.EscapeIfKeyword(varName);
    string listVarName = $"__{varName}_list_{routeIndex}";

    string condition = (option.LongForm, option.ShortForm) switch
    {
      (not null, not null) => $"routeArgs[__idx] == \"--{option.LongForm}\" || routeArgs[__idx] == \"-{option.ShortForm}\"",
      (not null, null) => $"routeArgs[__idx] == \"--{option.LongForm}\"",
      (null, not null) => $"routeArgs[__idx] == \"-{option.ShortForm}\"",
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    // Collect all values into a list
    sb.AppendLine($"      global::System.Collections.Generic.List<string> {listVarName} = [];");
    sb.AppendLine("      for (int __idx = 0; __idx < routeArgs.Length; __idx++)");
    sb.AppendLine("      {");
    sb.AppendLine($"        if ({condition})");
    sb.AppendLine("        {");
    sb.AppendLine("          // Check if there's a value following the flag (and it's not another flag)");
    sb.AppendLine("          if (__idx + 1 < routeArgs.Length && !routeArgs[__idx + 1].StartsWith(\"-\"))");
    sb.AppendLine("          {");
    sb.AppendLine($"            {listVarName}.Add(routeArgs[__idx + 1]);");
    sb.AppendLine("            __idx++; // Skip the value we just consumed");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // Note: Repeated options (*) allow zero occurrences by definition.
    // The asterisk means "zero or more", so we don't require any values.
    // Only non-repeated required options would check for presence.

    // Emit type conversion and array creation
    EmitRepeatedOptionTypeConversion(sb, option, escapedVarName, listVarName, routeIndex, customConverters);
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
    ImmutableArray<CustomConverterDefinition> customConverters)
  {
    string typeConstraint = option.TypeConstraint ?? "";
    string baseType = typeConstraint.EndsWith('?') ? typeConstraint[..^1] : typeConstraint;
    string optionDisplay = option.LongForm is not null ? $"--{option.LongForm}" : $"-{option.ShortForm}";

    // Use shared type conversion mapping with TryParse for graceful error handling
    (string ClrType, string TryParseCondition)? conversion = TypeConversionMap.GetBuiltInTryConversion(baseType, rawVarName, varName);

    if (conversion is var (clrType, tryParseCondition))
    {
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
        sb.AppendLine($"      if ({rawVarName} is null || !{tryParseCondition})");
        sb.AppendLine("      {");
        sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid value '{{{rawVarName} ?? \"(missing)\"}}' for option '{optionDisplay}'. Expected: {baseType}\");");
        sb.AppendLine("        return 1;");
        sb.AppendLine("      }");
      }
    }
    else
    {
      // Try to find a custom type converter for this constraint
      CustomConverterDefinition? converter = customConverters.FirstOrDefault(c =>
        string.Equals(c.TargetTypeName, baseType, StringComparison.Ordinal) ||
        string.Equals(GetSimpleTypeName(c.TargetTypeName), baseType, StringComparison.Ordinal) ||
        string.Equals(c.ConstraintAlias, baseType, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(GetTargetTypeFromConverterName(c.ConverterTypeName), baseType, StringComparison.Ordinal));

      if (converter is not null)
      {
        // Generate custom converter code for option
        EmitOptionCustomTypeConversion(sb, option, converter, varName, rawVarName, routeIndex);
      }
      else
      {
        // Unknown type - keep as string (fallback)
        sb.AppendLine(
          $"      string? {varName} = {rawVarName};");
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

    // Derive target type from converter name (convention: XxxConverter -> Xxx)
    string targetType = GetTargetTypeFromConverterName(converter.ConverterTypeName);

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
      sb.AppendLine($"      if ({rawVarName} is null || !{converterVarName}.TryConvert({rawVarName}, out object? {tempVarName}))");
      sb.AppendLine("      {");
      sb.AppendLine($"        app.Terminal.WriteLine($\"Error: Invalid {targetType} value for option '{option.LongForm ?? option.ShortForm}': '{{{rawVarName}}}'\");");
      sb.AppendLine("        return 1;");
      sb.AppendLine("      }");
      sb.AppendLine($"      {targetType} {varName} = ({targetType}){tempVarName}!;");
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
