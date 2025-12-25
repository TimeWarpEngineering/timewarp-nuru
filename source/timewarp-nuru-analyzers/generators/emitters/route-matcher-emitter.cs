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
  public static void Emit(StringBuilder sb, RouteDefinition route)
  {
    // Comment showing the route pattern
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"    // Route: {EscapeXmlComment(route.FullPattern)}");

    // Determine the matching strategy based on route complexity
    if (route.HasOptions || route.HasCatchAll)
    {
      EmitComplexMatch(sb, route);
    }
    else
    {
      EmitSimpleMatch(sb, route);
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits simple pattern matching using C# list patterns.
  /// Used for routes with only literals and required parameters.
  /// </summary>
  private static void EmitSimpleMatch(StringBuilder sb, RouteDefinition route)
  {
    string pattern = BuildListPattern(route);

    sb.AppendLine(CultureInfo.InvariantCulture, $"    if (args is {pattern})");
    sb.AppendLine("    {");

    // Emit handler invocation
    HandlerInvokerEmitter.Emit(sb, route, indent: 6);

    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
  }

  /// <summary>
  /// Emits complex matching logic for routes with options or catch-all parameters.
  /// Uses length checks and manual parsing.
  /// </summary>
  private static void EmitComplexMatch(StringBuilder sb, RouteDefinition route)
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
        $"      if (args[{literalIndex}] != \"{EscapeString(literal.Value)}\") goto route_skip_{route.GetHashCode()};");
      literalIndex++;
    }

    // Emit parameter extraction
    EmitParameterExtraction(sb, route, literalIndex);

    // Emit option parsing
    EmitOptionParsing(sb, route);

    // Emit handler invocation
    HandlerInvokerEmitter.Emit(sb, route, indent: 6);

    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine(CultureInfo.InvariantCulture, $"    route_skip_{route.GetHashCode()}:;");
  }

  /// <summary>
  /// Builds a C# list pattern string for simple matching.
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
          parts.Add($"var {param.CamelCaseName}");
          break;

        case ParameterDefinition param:
          // Required parameters capture with var
          parts.Add($"var {param.CamelCaseName}");
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
