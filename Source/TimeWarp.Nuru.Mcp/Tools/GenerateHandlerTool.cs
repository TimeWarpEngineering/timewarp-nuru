namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using System.Text;
using TimeWarp.Nuru.Parsing;

internal sealed class GenerateHandlerTool
{
  [McpServerTool]
  [Description("Generate a handler function from a route pattern")]
  public static string GenerateHandler(
      [Description("Route pattern (e.g., 'deploy {env} --force')")] string pattern,
      [Description("Use mediator pattern instead of direct delegates")] bool useMediator = false)
  {
    try
    {
      CompiledRoute route = PatternParser.Parse(pattern);

      if (useMediator)
      {
        return GenerateMediatorHandler(pattern, route);
      }
      else
      {
        return GenerateDirectHandler(pattern, route);
      }
    }
    catch (PatternException ex)
    {
      return $"// Error parsing pattern: {ex.Message}\n" +
             $"// Pattern: {pattern}\n" +
             "// Please check your route pattern syntax";
    }
  }

  private static string GenerateDirectHandler(string pattern, CompiledRoute route)
  {
    StringBuilder sb = new();
    List<ParameterInfo> parameters = ExtractParameters(route);

    sb.AppendLine(CultureInfo.InvariantCulture, $"// Route: {pattern}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"// Specificity Score: {route.Specificity}");
    sb.AppendLine();

    // Generate the AddRoute call
    sb.Append(CultureInfo.InvariantCulture, $".AddRoute(\"{pattern}\", ");

    if (parameters.Count == 0)
    {
      sb.AppendLine("() =>");
    }
    else
    {
      sb.Append('(');
      sb.AppendJoin(", ", parameters.Select(p => $"{p.Type} {p.Name}"));
      sb.AppendLine(") =>");
    }

    sb.AppendLine("{");
    sb.AppendLine("    // TODO: Implement handler logic");

    if (parameters.Count > 0)
    {
      sb.AppendLine();
      foreach (ParameterInfo param in parameters)
      {
        sb.AppendLine(CultureInfo.InvariantCulture, $"    Console.WriteLine(\"{param.DisplayName}: {{{param.Name}}}\");");
      }
    }

    sb.AppendLine("})");

    if (parameters.Any(p => p.Description is not null))
    {
      sb.AppendLine(".WithDescription(\"TODO: Add route description\")");
    }

    sb.AppendLine(";");

    return sb.ToString();
  }

  private static string GenerateMediatorHandler(string pattern, CompiledRoute route)
  {
    StringBuilder sb = new();
    List<ParameterInfo> parameters = ExtractParameters(route);

    // Generate a command name from the pattern
    string commandName = GenerateCommandName(pattern);

    sb.AppendLine(CultureInfo.InvariantCulture, $"// Route: {pattern}");
    sb.AppendLine("// Mediator Pattern Implementation");
    sb.AppendLine();

    // Generate the Command class
    sb.AppendLine(CultureInfo.InvariantCulture, $"public sealed class {commandName}Command : IRequest<int>");
    sb.AppendLine("{");

    foreach (ParameterInfo param in parameters)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"    public {param.Type} {param.PropertyName} {{ get; init; }}");
    }

    sb.AppendLine("}");
    sb.AppendLine();

    // Generate the Handler class
    sb.AppendLine(CultureInfo.InvariantCulture, $"public sealed class {commandName}Handler : IRequestHandler<{commandName}Command, int>");
    sb.AppendLine("{");
    sb.AppendLine(CultureInfo.InvariantCulture, $"    public Task<int> HandleAsync({commandName}Command command, CancellationToken cancellationToken = default)");
    sb.AppendLine("    {");
    sb.AppendLine("        // TODO: Implement handler logic");

    if (parameters.Count > 0)
    {
      sb.AppendLine();
      foreach (ParameterInfo param in parameters)
      {
        sb.AppendLine(CultureInfo.InvariantCulture, $"        Console.WriteLine(\"{param.DisplayName}: {{0}}\", command.{param.PropertyName});");
      }
    }

    sb.AppendLine();
    sb.AppendLine("        return Task.FromResult(0);");
    sb.AppendLine("    }");
    sb.AppendLine("}");
    sb.AppendLine();

    // Generate the route registration
    sb.AppendLine("// Route registration:");
    sb.Append(CultureInfo.InvariantCulture, $".AddRoute<{commandName}Command>(\"{pattern}\"");

    if (parameters.Count > 0)
    {
      sb.Append(", (");
      sb.AppendJoin(", ", parameters.Select(p => $"{p.Type} {p.Name}"));
      sb.Append(") => new() { ");
      sb.AppendJoin(", ", parameters.Select(p => $"{p.PropertyName} = {p.Name}"));
      sb.Append(" }");
    }

    sb.AppendLine(")");
    sb.AppendLine(".WithDescription(\"TODO: Add route description\");");

    return sb.ToString();
  }

  private static List<ParameterInfo> ExtractParameters(CompiledRoute route)
  {
    List<ParameterInfo> parameters = [];

    // Extract positional parameters
    foreach (RouteMatcher matcher in route.PositionalMatchers)
    {
      if (matcher is ParameterMatcher param)
      {
        parameters.Add(new ParameterInfo
        {
          Name = ToCamelCase(param.Name),
          PropertyName = ToPascalCase(param.Name),
          DisplayName = param.Name,
          Type = GetParameterType(param),
          Description = param.Description,
          IsOptional = param.IsOptional,
          IsCatchAll = param.IsCatchAll
        });
      }
    }

    // Extract option parameters
    foreach (OptionMatcher option in route.OptionMatchers)
    {
      string optionName = option.MatchPattern.TrimStart('-');
      string paramName = ToCamelCase(option.ParameterName ?? optionName);

      if (option.ExpectsValue)
      {
        // Option with value
        parameters.Add(new ParameterInfo
        {
          Name = paramName,
          PropertyName = ToPascalCase(option.ParameterName ?? optionName),
          DisplayName = option.MatchPattern,
          Type = "string", // Options with values are always strings unless we parse constraint
          Description = option.Description,
          IsOptional = true, // Options are always optional
          IsOption = true
        });
      }
      else
      {
        // Boolean flag
        parameters.Add(new ParameterInfo
        {
          Name = paramName,
          PropertyName = ToPascalCase(optionName),
          DisplayName = option.MatchPattern,
          Type = "bool",
          Description = option.Description,
          IsOption = true,
          IsFlag = true
        });
      }
    }

    return parameters;
  }

  private static string GetParameterType(ParameterMatcher matcher)
  {
    if (matcher.IsCatchAll)
    {
      return "string[]";
    }

    string baseType = matcher.Constraint?.ToLowerInvariant() switch
    {
      "int" => "int",
      "double" => "double",
      "bool" => "bool",
      "datetime" => "DateTime",
      "guid" => "Guid",
      "long" => "long",
      "float" => "float",
      "decimal" => "decimal",
      _ => "string"
    };

    if (matcher.IsOptional && baseType != "string")
    {
      return $"{baseType}?";
    }

    if (matcher.IsOptional && baseType == "string")
    {
      return "string?";
    }

    return baseType;
  }

  private static string GenerateCommandName(string pattern)
  {
    // Extract the first word or two as the command name
    string[] parts = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    StringBuilder name = new();

    for (int i = 0; i < Math.Min(2, parts.Length); i++)
    {
      string part = parts[i];
      if (part.StartsWith('{') || part.StartsWith('-'))
        break;

      name.Append(ToPascalCase(part));
    }

    if (name.Length == 0)
    {
      name.Append("Custom");
    }

    return name.ToString();
  }

  private static string ToCamelCase(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    string cleaned = input.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
    if (cleaned.Length == 0)
      return input;

    return char.ToLowerInvariant(cleaned[0]) + cleaned[1..];
  }

  private static string ToPascalCase(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    string cleaned = input.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
    if (cleaned.Length == 0)
      return input;

    return char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
  }

  private sealed class ParameterInfo
  {
    public required string Name { get; init; }
    public required string PropertyName { get; init; }
    public required string DisplayName { get; init; }
    public required string Type { get; init; }
    public string? Description { get; init; }
    public bool IsOptional { get; init; }
    public bool IsCatchAll { get; init; }
    public bool IsOption { get; init; }
    public bool IsFlag { get; init; }
  }
}
