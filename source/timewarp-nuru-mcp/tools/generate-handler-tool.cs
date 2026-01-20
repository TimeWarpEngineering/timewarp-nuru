namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using System.Text;
using TimeWarp.Nuru;

internal sealed class GenerateHandlerTool
{
  [McpServerTool]
  [Description("Generate a handler function from a route pattern")]
  public static string GenerateHandler(
      [Description("Route pattern (e.g., 'deploy {env} --force')")] string pattern)
  {
    try
    {
      CompiledRoute route = PatternParser.Parse(pattern);
      return GenerateWithHandlerPattern(pattern, route);
    }
    catch (PatternException ex)
    {
      return $"// Error parsing pattern: {ex.Message}\n" +
             $"// Pattern: {pattern}\n" +
             "// Please check your route pattern syntax";
    }
  }

  private static string GenerateWithHandlerPattern(string pattern, CompiledRoute route)
  {
    StringBuilder sb = new();
    List<ParameterInfo> parameters = ExtractParameters(route);

    sb.AppendLine(CultureInfo.InvariantCulture, $"// Route: {pattern}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"// Specificity Score: {route.Specificity}");
    sb.AppendLine();

    // Show recommended V2 fluent DSL pattern
    sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
    sb.AppendLine("// V2 Fluent DSL Pattern with .WithHandler()");
    sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
    sb.AppendLine("// Features:");
    sb.AppendLine("//   - Source-generated at compile time (AOT compatible)");
    sb.AppendLine("//   - Type-safe parameter binding");
    sb.AppendLine("//   - Pipeline behaviors via .AddBehavior()");
    sb.AppendLine("//   - Endpoint classification via .AsCommand(), .AsQuery(), etc.");
    sb.AppendLine();
    sb.AppendLine("using TimeWarp.Nuru;");
    sb.AppendLine("using static System.Console;");
    sb.AppendLine();
    sb.AppendLine("NuruApp app = NuruApp.CreateBuilder(args)");

    // Generate the Map call with fluent DSL
    sb.Append(CultureInfo.InvariantCulture, $"  .Map(\"{pattern}\")");
    sb.AppendLine();
    sb.AppendLine("    .WithDescription(\"TODO: Add description\")");

    // Generate the WithHandler call
    sb.Append("    .WithHandler(");

    if (parameters.Count == 0)
    {
      sb.Append("() =>");
    }
    else
    {
      sb.Append('(');
      sb.AppendJoin(", ", parameters.Select(p => $"{p.Type} {p.Name}"));
      sb.Append(") =>");
    }

    sb.AppendLine();
    sb.AppendLine("    {");
    sb.AppendLine("      // TODO: Implement handler logic");

    if (parameters.Count > 0)
    {
      foreach (ParameterInfo param in parameters)
      {
        sb.AppendLine(CultureInfo.InvariantCulture, $"      WriteLine($\"{param.DisplayName}: {{{param.Name}}}\");");
      }
    }
    else
    {
      sb.AppendLine("      WriteLine(\"Command executed\");");
    }

    sb.AppendLine("    })");
    sb.AppendLine("    .AsCommand()  // or .AsQuery(), .AsIdempotentCommand()");
    sb.AppendLine("    .Done()");
    sb.AppendLine("  .Build();");
    sb.AppendLine();
    sb.AppendLine("return await app.RunAsync(args);");
    sb.AppendLine();

    // Show attributed route alternative
    sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
    sb.AppendLine("// Alternative: [NuruRoute] Attributed Pattern");
    sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
    sb.AppendLine("// For larger apps, use attributed routes for auto-discovery:");
    sb.AppendLine("//");

    string commandName = GenerateCommandName(pattern);
    sb.AppendLine(CultureInfo.InvariantCulture, $"// [NuruRoute(\"{pattern}\", Description = \"TODO: Add description\")]");
    sb.AppendLine(CultureInfo.InvariantCulture, $"// public sealed class {commandName}Endpoint");
    sb.AppendLine("// {");

    foreach (ParameterInfo param in parameters)
    {
      string nullableMarker = param.IsOptional ? "?" : "";
      string requiredMarker = param.IsOptional ? "" : "required ";
      sb.AppendLine(CultureInfo.InvariantCulture, $"//   public {requiredMarker}{param.Type}{nullableMarker} {param.PropertyName} {{ get; init; }}");
    }

    sb.AppendLine("//");
    sb.AppendLine("//   internal sealed class Handler(ITerminal terminal)");
    sb.AppendLine("//   {");
    sb.AppendLine(CultureInfo.InvariantCulture, $"//     public ValueTask<Unit> Handle({commandName}Endpoint request, CancellationToken ct)");
    sb.AppendLine("//     {");
    sb.AppendLine("//       // Handler logic here");
    sb.AppendLine("//       return ValueTask.FromResult(Unit.Value);");
    sb.AppendLine("//     }");
    sb.AppendLine("//   }");
    sb.AppendLine("// }");
    sb.AppendLine();

    // Show pipeline behaviors example
    sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
    sb.AppendLine("// Adding Pipeline Behaviors");
    sb.AppendLine("// ═══════════════════════════════════════════════════════════════");
    sb.AppendLine("// Add cross-cutting concerns with .AddBehavior():");
    sb.AppendLine("//");
    sb.AppendLine("// NuruApp app = NuruApp.CreateBuilder(args)");
    sb.AppendLine("//   .AddBehavior(typeof(LoggingBehavior))      // Global: applies to all routes");
    sb.AppendLine("//   .AddBehavior(typeof(PerformanceBehavior))  // Global: applies to all routes");
    sb.AppendLine("//   .AddBehavior(typeof(AuthBehavior))         // Filtered: INuruBehavior<IRequireAuth>");
    sb.AppendLine(CultureInfo.InvariantCulture, $"//   .Map(\"{pattern}\")");
    sb.AppendLine("//     .WithHandler(...)");
    sb.AppendLine("//     .Implements<IRequireAuth>()  // Opt-in to filtered behaviors");
    sb.AppendLine("//     .AsCommand()");
    sb.AppendLine("//     .Done()");
    sb.AppendLine("//   .Build();");

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
