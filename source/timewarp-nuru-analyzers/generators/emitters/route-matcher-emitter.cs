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
  public static void Emit(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    ImmutableArray<ServiceDefinition> services,
    ImmutableArray<BehaviorDefinition> behaviors = default)
  {
    // Use empty array if behaviors not provided
    if (behaviors.IsDefault)
      behaviors = [];

    // Comment showing the route pattern
    sb.AppendLine(
      $"    // Route: {EscapeXmlComment(route.FullPattern)}");

    // Determine the matching strategy based on route complexity
    // Use complex matching for routes with options, catch-all, or optional positional params
    if (route.HasOptions || route.HasCatchAll || route.HasOptionalPositionalParams)
    {
      EmitComplexMatch(sb, route, routeIndex, services, behaviors);
    }
    else
    {
      EmitSimpleMatch(sb, route, routeIndex, services, behaviors);
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
    ImmutableArray<BehaviorDefinition> behaviors)
  {
    string pattern = BuildListPattern(route, routeIndex);

    sb.AppendLine($"    if (args is {pattern})");
    sb.AppendLine("    {");

    // Emit variable aliases (from route-unique names to handler-expected names)
    EmitVariableAliases(sb, route, routeIndex, indent: 6);

    // Emit type conversions for typed parameters
    EmitTypeConversions(sb, route, routeIndex, indent: 6);

    // Emit handler invocation (wrapped with behaviors if any)
    if (behaviors.Length > 0)
    {
      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8));

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
    ImmutableArray<BehaviorDefinition> behaviors)
  {
    // Calculate minimum required args
    int minArgs = route.Literals.Count() + route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll);

    sb.AppendLine($"    if (args.Length >= {minArgs})");
    sb.AppendLine("    {");

    // Emit literal matching
    int literalIndex = 0;
    foreach (LiteralDefinition literal in route.Literals)
    {
      sb.AppendLine(
        $"      if (args[{literalIndex}] != \"{EscapeString(literal.Value)}\") goto route_skip_{routeIndex};");
      literalIndex++;
    }

    // Emit parameter extraction (typed params use unique var names for later conversion)
    EmitParameterExtraction(sb, route, routeIndex, literalIndex);

    // Emit type conversions for typed parameters
    EmitTypeConversions(sb, route, routeIndex, indent: 6);

    // Emit option parsing
    EmitOptionParsing(sb, route, routeIndex);

    // Emit handler invocation (wrapped with behaviors if any)
    if (behaviors.Length > 0)
    {
      BehaviorEmitter.EmitPipelineWrapper(
        sb, route, routeIndex, behaviors, services, indent: 6,
        () => HandlerInvokerEmitter.Emit(sb, route, routeIndex, services, indent: 8));

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
      string uniqueVarName = $"__{varName}_{routeIndex}";

      // Create alias from unique name to handler-expected name
      sb.AppendLine(
        $"{indentStr}string {varName} = {uniqueVarName};");
    }
  }

  /// <summary>
  /// Emits type conversion code for typed parameters.
  /// Pattern matching captures typed params with route-unique names.
  /// This method creates the properly typed variable with the original name.
  /// </summary>
  private static void EmitTypeConversions(StringBuilder sb, RouteDefinition route, int routeIndex, int indent)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterDefinition param in route.Parameters)
    {
      if (!param.HasTypeConstraint)
        continue;

      string varName = param.CamelCaseName;
      string uniqueVarName = $"__{varName}_{routeIndex}";

      // Normalize type constraint: strip '?' suffix to get base type
      string typeConstraint = param.TypeConstraint ?? "";
      string baseType = typeConstraint.EndsWith('?') ? typeConstraint[..^1] : typeConstraint;

      // Map base type to CLR type name and parse expression
      (string? clrType, string? parseExpr) = baseType.ToLowerInvariant() switch
      {
        "int" => ("int", $"int.Parse({uniqueVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
        "long" => ("long", $"long.Parse({uniqueVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
        "double" => ("double", $"double.Parse({uniqueVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
        "decimal" => ("decimal", $"decimal.Parse({uniqueVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
        "bool" => ("bool", $"bool.Parse({uniqueVarName})"),
        "guid" => ("System.Guid", $"System.Guid.Parse({uniqueVarName})"),
        "datetime" => ("System.DateTime", $"System.DateTime.Parse({uniqueVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
        _ => (null, null)
      };

      if (clrType is not null && parseExpr is not null)
      {
        if (param.IsOptional)
        {
          // Optional param: check for null before parsing
          sb.AppendLine(
            $"{indentStr}{clrType}? {varName} = {uniqueVarName} is not null ? {parseExpr} : null;");
        }
        else
        {
          // Required param: direct parse
          sb.AppendLine(
            $"{indentStr}{clrType} {varName} = {parseExpr};");
        }
      }
      else if (param.ResolvedClrTypeName is not null)
      {
        // Unknown type constraint
        sb.AppendLine(
          $"{indentStr}// TODO: Type conversion for {param.ResolvedClrTypeName}");
      }
    }
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
          $"      string[] {varName} = args[{paramIndex}..];");
      }
      else if (param.IsOptional)
      {
        // Optional parameter with bounds check - null when not provided
        sb.AppendLine(
          $"      string? {varName} = args.Length > {paramIndex} ? args[{paramIndex}] : null;");
        paramIndex++;
      }
      else
      {
        // Required parameter
        sb.AppendLine(
          $"      string {varName} = args[{paramIndex}];");
        paramIndex++;
      }
    }
  }

  /// <summary>
  /// Emits code to parse options from args.
  /// </summary>
  private static void EmitOptionParsing(StringBuilder sb, RouteDefinition route, int routeIndex)
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
        EmitValueOptionParsing(sb, option, routeIndex);
      }
    }
  }

  /// <summary>
  /// Emits code to parse a boolean flag option.
  /// </summary>
  private static void EmitFlagParsing(StringBuilder sb, OptionDefinition option)
  {
    string varName = ToCamelCase(option.LongForm ?? option.ShortForm ?? "flag");

    string condition = (option.LongForm, option.ShortForm) switch
    {
      (not null, not null) => $"a == \"--{option.LongForm}\" || a == \"-{option.ShortForm}\"",
      (not null, null) => $"a == \"--{option.LongForm}\"",
      (null, not null) => $"a == \"-{option.ShortForm}\"",
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    sb.AppendLine(
      $"      bool {varName} = Array.Exists(args, a => {condition});");
  }

  /// <summary>
  /// Emits code to parse an option that takes a value.
  /// Handles type conversion for typed options (int, double, etc.).
  /// </summary>
  private static void EmitValueOptionParsing(StringBuilder sb, OptionDefinition option, int routeIndex)
  {
    string varName = option.ParameterName ?? ToCamelCase(option.LongForm ?? option.ShortForm ?? "value");
    string defaultValue = option.IsOptional ? "null" : "string.Empty";

    string condition = (option.LongForm, option.ShortForm) switch
    {
      (not null, not null) => $"args[__idx] == \"--{option.LongForm}\" || args[__idx] == \"-{option.ShortForm}\"",
      (not null, null) => $"args[__idx] == \"--{option.LongForm}\"",
      (null, not null) => $"args[__idx] == \"-{option.ShortForm}\"",
      _ => throw new InvalidOperationException("Option must have at least one form")
    };

    // For typed options, extract to a temp string first, then convert
    bool needsConversion = option.TypeConstraint is not null;
    string rawVarName = needsConversion ? $"__{varName}_raw" : varName;

    sb.AppendLine(
      $"      string? {rawVarName} = {defaultValue};");
    sb.AppendLine("      for (int __idx = 0; __idx < args.Length - 1; __idx++)");
    sb.AppendLine("      {");
    sb.AppendLine(
      $"        if ({condition})");
    sb.AppendLine("        {");
    sb.AppendLine($"          {rawVarName} = args[__idx + 1];");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");

    // For required options, skip route if option was not found
    if (!option.IsOptional)
    {
      sb.AppendLine(
        $"      if ({rawVarName} == string.Empty) goto route_skip_{routeIndex};");
    }

    // Emit type conversion if needed
    if (needsConversion)
    {
      EmitOptionTypeConversion(sb, option, varName, rawVarName);
    }
  }

  /// <summary>
  /// Emits type conversion code for a typed option.
  /// </summary>
  private static void EmitOptionTypeConversion(StringBuilder sb, OptionDefinition option, string varName, string rawVarName)
  {
    string typeConstraint = option.TypeConstraint ?? "";
    string baseType = typeConstraint.EndsWith('?') ? typeConstraint[..^1] : typeConstraint;

    // Map type constraint to CLR type and parse expression
    (string? clrType, string? parseExpr) = baseType.ToLowerInvariant() switch
    {
      "int" => ("int", $"int.Parse({rawVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
      "long" => ("long", $"long.Parse({rawVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
      "double" => ("double", $"double.Parse({rawVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
      "decimal" => ("decimal", $"decimal.Parse({rawVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
      "bool" => ("bool", $"bool.Parse({rawVarName})"),
      "guid" => ("System.Guid", $"System.Guid.Parse({rawVarName})"),
      "datetime" => ("System.DateTime", $"System.DateTime.Parse({rawVarName}, System.Globalization.CultureInfo.InvariantCulture)"),
      _ => (null, null)
    };

    if (clrType is not null && parseExpr is not null)
    {
      if (option.ParameterIsOptional)
      {
        // Optional option value: check for null before parsing
        sb.AppendLine(
          $"      {clrType}? {varName} = {rawVarName} is not null ? {parseExpr} : null;");
      }
      else
      {
        // Required option value: direct parse (will throw if null/empty, but route check should prevent)
        sb.AppendLine(
          $"      {clrType} {varName} = {rawVarName} is not null ? {parseExpr} : default;");
      }
    }
    else
    {
      // Unknown type - keep as string (fallback)
      sb.AppendLine(
        $"      string? {varName} = {rawVarName};");
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
