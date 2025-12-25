// Emits capabilities JSON generation code for AI tool discovery.
// Generates the PrintCapabilities method for --capabilities flag handling.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to generate JSON capabilities for AI tools.
/// Creates the PrintCapabilities method that outputs structured command information.
/// </summary>
internal static class CapabilitiesEmitter
{
  /// <summary>
  /// Emits the PrintCapabilities method for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="model">The application model containing routes and metadata.</param>
  public static void Emit(StringBuilder sb, AppModel model)
  {
    sb.AppendLine("  private static void PrintCapabilities(ITerminal terminal)");
    sb.AppendLine("  {");

    // Build the JSON as a raw string literal
    sb.AppendLine("    terminal.WriteLine(\"\"\"");
    sb.AppendLine("    {");

    EmitMetadata(sb, model);
    EmitCommands(sb, model);

    sb.AppendLine("    }");
    sb.AppendLine("    \"\"\");");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the metadata section of the capabilities JSON.
  /// </summary>
  private static void EmitMetadata(StringBuilder sb, AppModel model)
  {
    string name = EscapeJsonString(model.Name ?? "app");
    sb.AppendLine(CultureInfo.InvariantCulture, $"      \"name\": \"{name}\",");

    if (model.Description is not null)
    {
      string description = EscapeJsonString(model.Description);
      sb.AppendLine(CultureInfo.InvariantCulture, $"      \"description\": \"{description}\",");
    }

    if (model.AiPrompt is not null)
    {
      string aiPrompt = EscapeJsonString(model.AiPrompt);
      sb.AppendLine(CultureInfo.InvariantCulture, $"      \"aiPrompt\": \"{aiPrompt}\",");
    }
  }

  /// <summary>
  /// Emits the commands array of the capabilities JSON.
  /// </summary>
  private static void EmitCommands(StringBuilder sb, AppModel model)
  {
    sb.AppendLine("      \"commands\": [");

    IReadOnlyList<RouteDefinition> routes = [.. model.Routes];
    for (int i = 0; i < routes.Count; i++)
    {
      RouteDefinition route = routes[i];
      bool isLast = i == routes.Count - 1;
      EmitCommandEntry(sb, route, isLast);
    }

    sb.AppendLine("      ]");
  }

  /// <summary>
  /// Emits a single command entry in the capabilities JSON.
  /// </summary>
  private static void EmitCommandEntry(StringBuilder sb, RouteDefinition route, bool isLast)
  {
    sb.AppendLine("        {");

    // Pattern
    string pattern = EscapeJsonString(route.FullPattern);
    sb.AppendLine(CultureInfo.InvariantCulture, $"          \"pattern\": \"{pattern}\",");

    // Description
    if (route.Description is not null)
    {
      string description = EscapeJsonString(route.Description);
      sb.AppendLine(CultureInfo.InvariantCulture, $"          \"description\": \"{description}\",");
    }

    // Message type (maps to AI safety level)
    string messageType = route.MessageType.ToLowerInvariant();
    sb.AppendLine(CultureInfo.InvariantCulture, $"          \"type\": \"{messageType}\",");

    // Parameters
    if (route.Parameters.Any())
    {
      EmitParameters(sb, route);
    }

    // Options
    if (route.Options.Any())
    {
      EmitOptions(sb, route);
    }

    // Aliases
    if (route.Aliases.Length > 0)
    {
      EmitAliases(sb, route);
    }

    // Remove trailing comma from last property (type is always last if no params/options/aliases)
    // We handle this by always ending with type which has no trailing comma logic needed
    // Actually let's make "type" always the last non-array property

    string comma = isLast ? "" : ",";
    sb.AppendLine(CultureInfo.InvariantCulture, $"        }}{comma}");
  }

  /// <summary>
  /// Emits the parameters array for a command.
  /// </summary>
  private static void EmitParameters(StringBuilder sb, RouteDefinition route)
  {
    sb.AppendLine("          \"parameters\": [");

    IReadOnlyList<ParameterDefinition> parameters = [.. route.Parameters];
    for (int i = 0; i < parameters.Count; i++)
    {
      ParameterDefinition param = parameters[i];
      bool isLast = i == parameters.Count - 1;
      string comma = isLast ? "" : ",";

      sb.AppendLine("            {");
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"              \"name\": \"{EscapeJsonString(param.Name)}\",");

      if (param.Description is not null)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"              \"description\": \"{EscapeJsonString(param.Description)}\",");
      }

      sb.AppendLine(CultureInfo.InvariantCulture,
        $"              \"required\": {(param.IsOptional ? "false" : "true")},");

      if (param.IsCatchAll)
      {
        sb.AppendLine("              \"catchAll\": true,");
      }

      string typeConstraint = param.TypeConstraint ?? "string";
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"              \"type\": \"{EscapeJsonString(typeConstraint)}\"");

      sb.AppendLine(CultureInfo.InvariantCulture, $"            }}{comma}");
    }

    sb.AppendLine("          ],");
  }

  /// <summary>
  /// Emits the options array for a command.
  /// </summary>
  private static void EmitOptions(StringBuilder sb, RouteDefinition route)
  {
    sb.AppendLine("          \"options\": [");

    IReadOnlyList<OptionDefinition> options = [.. route.Options];
    for (int i = 0; i < options.Count; i++)
    {
      OptionDefinition option = options[i];
      bool isLast = i == options.Count - 1;
      string comma = isLast ? "" : ",";

      sb.AppendLine("            {");
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"              \"long\": \"--{EscapeJsonString(option.LongForm)}\",");

      if (option.ShortForm is not null)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"              \"short\": \"-{EscapeJsonString(option.ShortForm)}\",");
      }

      if (option.Description is not null)
      {
        sb.AppendLine(CultureInfo.InvariantCulture,
          $"              \"description\": \"{EscapeJsonString(option.Description)}\",");
      }

      sb.AppendLine(CultureInfo.InvariantCulture,
        $"              \"required\": {(option.IsOptional ? "false" : "true")},");
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"              \"isFlag\": {(option.IsFlag ? "true" : "false")}");

      sb.AppendLine(CultureInfo.InvariantCulture, $"            }}{comma}");
    }

    sb.AppendLine("          ],");
  }

  /// <summary>
  /// Emits the aliases array for a command.
  /// </summary>
  private static void EmitAliases(StringBuilder sb, RouteDefinition route)
  {
    sb.Append("          \"aliases\": [");

    IReadOnlyList<string> aliases = [.. route.Aliases];
    for (int i = 0; i < aliases.Count; i++)
    {
      string alias = aliases[i];
      bool isLast = i == aliases.Count - 1;
      string comma = isLast ? "" : ", ";
      sb.Append(CultureInfo.InvariantCulture, $"\"{EscapeJsonString(alias)}\"{comma}");
    }

    sb.AppendLine("],");
  }

  /// <summary>
  /// Escapes a string for use in JSON.
  /// </summary>
  private static string EscapeJsonString(string value)
  {
    return value
      .Replace("\\", "\\\\", StringComparison.Ordinal)
      .Replace("\"", "\\\"", StringComparison.Ordinal)
      .Replace("\n", "\\n", StringComparison.Ordinal)
      .Replace("\r", "\\r", StringComparison.Ordinal)
      .Replace("\t", "\\t", StringComparison.Ordinal);
  }
}
