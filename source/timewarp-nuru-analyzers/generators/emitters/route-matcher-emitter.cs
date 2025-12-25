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
  public static void Emit(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    // Comment showing the route pattern
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"    // Route: {EscapeXmlComment(route.FullPattern)}");

    // Determine the matching strategy based on route complexity
    if (route.HasOptions || route.HasCatchAll)
    {
      EmitComplexMatch(sb, route, routeIndex);
    }
    else
    {
      EmitSimpleMatch(sb, route, routeIndex);
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits simple pattern matching using C# list patterns.
  /// Used for routes with only literals and required parameters.
  /// </summary>
  private static void EmitSimpleMatch(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    string pattern = BuildListPattern(route);

    sb.AppendLine(CultureInfo.InvariantCulture, $"    if (args is {pattern})");
    sb.AppendLine("    {");

    // Emit type conversions for typed parameters
    EmitTypeConversions(sb, route, indent: 6);

    // Emit handler invocation
    HandlerInvokerEmitter.Emit(sb, route, routeIndex, indent: 6);

    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
  }

  /// <summary>
  /// Emits complex matching logic for routes with options or catch-all parameters.
  /// Uses length checks and manual parsing.
  /// </summary>
  private static void EmitComplexMatch(StringBuilder sb, RouteDefinition route, int routeIndex)
  {
    // Calculate minimum required args
    int minArgs = route.Literals.Count() + route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll);

    sb.AppendLine(CultureInfo.InvariantCulture, $"    if (args.Length >= {minArgs})");
    sb.AppendLine("    {");

    // Emit literal matching
    int literalIndex = 0;
    foreach (LiteralDefinition literal in route.Literals)
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"      if (args[{literalIndex}] != \"{EscapeString(literal.Value)}\") goto route_skip_{routeIndex};");
      literalIndex++;
    }

    // Emit parameter extraction
    EmitParameterExtraction(sb, route, literalIndex);

    // Emit option parsing
    EmitOptionParsing(sb, route);

    // Emit handler invocation
    HandlerInvokerEmitter.Emit(sb, route, routeIndex, indent: 6);

    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine(CultureInfo.InvariantCulture, $"    route_skip_{routeIndex}:;");
  }

  /// <summary>
  /// Emits type conversion code for typed parameters.
  /// Pattern matching captures typed params as strings with __str suffix.
  /// This method creates the properly typed variable with the original name.
  /// </summary>
  private static void EmitTypeConversions(StringBuilder sb, RouteDefinition route, int indent)
  {
    string indentStr = new(' ', indent);

    foreach (ParameterDefinition param in route.Parameters)
    {
      if (!param.HasTypeConstraint)
        continue;

      string varName = param.CamelCaseName;
      string stringVarName = $"{varName}__str";

      // Parse from the __str variable to create the typed variable with the original name
      switch (param.TypeConstraint?.ToLowerInvariant())
      {
        case "int":
          sb.AppendLine(CultureInfo.InvariantCulture,
            $"{indentStr}int {varName} = int.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture);");
          break;

        case "long":
          sb.AppendLine(CultureInfo.InvariantCulture,
            $"{indentStr}long {varName} = long.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture);");
          break;

        case "double":
          sb.AppendLine(CultureInfo.InvariantCulture,
            $"{indentStr}double {varName} = double.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture);");
          break;

        case "decimal":
          sb.AppendLine(CultureInfo.InvariantCulture,
            $"{indentStr}decimal {varName} = decimal.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture);");
          break;

        case "bool":
          sb.AppendLine(CultureInfo.InvariantCulture,
            $"{indentStr}bool {varName} = bool.Parse({stringVarName});");
          break;

        case "guid":
          sb.AppendLine(CultureInfo.InvariantCulture,
            $"{indentStr}System.Guid {varName} = System.Guid.Parse({stringVarName});");
          break;

        case "datetime":
          sb.AppendLine(CultureInfo.InvariantCulture,
            $"{indentStr}System.DateTime {varName} = System.DateTime.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture);");
          break;

        default:
          // Unknown type constraint - use the resolved CLR type if available
          if (param.ResolvedClrTypeName is not null)
          {
            sb.AppendLine(CultureInfo.InvariantCulture,
              $"{indentStr}// TODO: Type conversion for {param.ResolvedClrTypeName}");
          }

          break;
      }
    }
  }

  /// <summary>
  /// Builds a C# list pattern string for simple matching.
  /// Typed parameters use __str suffix so we can parse them to the correct type.
  /// </summary>
  private static string BuildListPattern(RouteDefinition route)
  {
    List<string> parts = [];

    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          parts.Add($"\"{EscapeString(literal.Value)}\"");
          break;

        case ParameterDefinition param when param.IsOptional:
          // Optional parameters use pattern with default
          string optVarName = param.HasTypeConstraint ? $"{param.CamelCaseName}__str" : param.CamelCaseName;
          parts.Add($"var {optVarName}");
          break;

        case ParameterDefinition param:
          // Required parameters capture with var
          // Typed parameters get __str suffix so we can parse them
          string varName = param.HasTypeConstraint ? $"{param.CamelCaseName}__str" : param.CamelCaseName;
          parts.Add($"var {varName}");
          break;
      }
    }

    return $"[{string.Join(", ", parts)}]";
  }

  /// <summary>
  /// Emits code to extract parameter values from args.
  /// </summary>
  private static void EmitParameterExtraction(StringBuilder sb, RouteDefinition route, int startIndex)
  {
    int paramIndex = startIndex;
    foreach (ParameterDefinition param in route.Parameters)
    {
      if (param.IsCatchAll)
      {
        // Catch-all gets remaining args
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"      string[] {param.CamelCaseName} = args[{paramIndex}..];");
      }
      else if (param.IsOptional)
      {
        // Optional parameter with bounds check
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"      string {param.CamelCaseName} = args.Length > {paramIndex} ? args[{paramIndex}] : string.Empty;");
        paramIndex++;
      }
      else
      {
        // Required parameter
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"      string {param.CamelCaseName} = args[{paramIndex}];");
        paramIndex++;
      }
    }
  }

  /// <summary>
  /// Emits code to parse options from args.
  /// </summary>
  private static void EmitOptionParsing(StringBuilder sb, RouteDefinition route)
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
        EmitValueOptionParsing(sb, option);
      }
    }
  }

  /// <summary>
  /// Emits code to parse a boolean flag option.
  /// </summary>
  private static void EmitFlagParsing(StringBuilder sb, OptionDefinition option)
  {
    string varName = ToCamelCase(option.LongForm);

    if (option.ShortForm is not null)
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"      bool {varName} = Array.Exists(args, a => a == \"--{option.LongForm}\" || a == \"-{option.ShortForm}\");");
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"      bool {varName} = Array.Exists(args, a => a == \"--{option.LongForm}\");");
    }
  }

  /// <summary>
  /// Emits code to parse an option that takes a value.
  /// </summary>
  private static void EmitValueOptionParsing(StringBuilder sb, OptionDefinition option)
  {
    string varName = option.ParameterName ?? ToCamelCase(option.LongForm);
    string defaultValue = option.IsOptional ? "null" : "string.Empty";

    sb.AppendLine(CultureInfo.InvariantCulture,
      $"      string? {varName} = {defaultValue};");
    sb.AppendLine("      for (int i = 0; i < args.Length - 1; i++)");
    sb.AppendLine("      {");

    if (option.ShortForm is not null)
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"        if (args[i] == \"--{option.LongForm}\" || args[i] == \"-{option.ShortForm}\")");
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"        if (args[i] == \"--{option.LongForm}\")");
    }

    sb.AppendLine("        {");
    sb.AppendLine(CultureInfo.InvariantCulture, $"          {varName} = args[i + 1];");
    sb.AppendLine("          break;");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
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
