// Emits capabilities JSON generation code for AI tool discovery.
// Generates the PrintCapabilities method for --capabilities flag handling.
// Outputs hierarchical JSON with groups containing nested groups and commands.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to generate JSON capabilities for AI tools.
/// Creates the PrintCapabilities method that outputs structured command information.
/// Commands are organized hierarchically: grouped commands appear only within their group,
/// ungrouped commands appear at the top level.
/// </summary>
internal static class CapabilitiesEmitter
{
  /// <summary>
  /// Emits the PrintCapabilities method for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="model">The application model containing routes and metadata.</param>
  /// <param name="methodSuffix">Suffix for method name (e.g., "_0" for multi-app assemblies).</param>
  public static void Emit(StringBuilder sb, AppModel model, string methodSuffix = "")
  {
    sb.AppendLine($"  private static void PrintCapabilities{methodSuffix}(ITerminal terminal)");
    sb.AppendLine("  {");

    // Build the JSON as a raw string literal
    sb.AppendLine("    terminal.WriteLine(\"\"\"");
    sb.AppendLine("    {");

    EmitMetadata(sb, model);

    // Build group hierarchy from routes
    GroupHierarchyResult hierarchy = GroupHierarchyBuilder.BuildHierarchy(model.Routes);

    // Emit groups if any exist
    if (hierarchy.RootGroups.Count > 0)
    {
      EmitGroups(sb, hierarchy.RootGroups, indent: 6, isLastProperty: hierarchy.UngroupedRoutes.Count == 0);
    }

    // Emit ungrouped commands at top level
    EmitUngroupedCommands(sb, hierarchy.UngroupedRoutes);

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
    sb.AppendLine($"      \"name\": \"{name}\",");

    // Version info (from assembly metadata, extracted at compile time)
    if (model.Version is not null)
    {
      string version = EscapeJsonString(model.Version);
      sb.AppendLine($"      \"version\": \"{version}\",");
    }

    if (model.CommitHash is not null)
    {
      string commitHash = EscapeJsonString(model.CommitHash);
      sb.AppendLine($"      \"commitHash\": \"{commitHash}\",");
    }

    if (model.CommitDate is not null)
    {
      string commitDate = EscapeJsonString(model.CommitDate);
      sb.AppendLine($"      \"commitDate\": \"{commitDate}\",");
    }

    if (model.Description is not null)
    {
      string description = EscapeJsonString(model.Description);
      sb.AppendLine($"      \"description\": \"{description}\",");
    }

    if (model.AiPrompt is not null)
    {
      string aiPrompt = EscapeJsonString(model.AiPrompt);
      sb.AppendLine($"      \"aiPrompt\": \"{aiPrompt}\",");
    }
  }

  /// <summary>
  /// Emits the groups array with nested groups and commands.
  /// </summary>
  private static void EmitGroups(StringBuilder sb, IReadOnlyList<GroupNode> groups, int indent, bool isLastProperty)
  {
    string indentStr = new(' ', indent);
    sb.AppendLine($"{indentStr}\"groups\": [");

    for (int i = 0; i < groups.Count; i++)
    {
      GroupNode group = groups[i];
      bool isLast = i == groups.Count - 1;
      EmitGroupEntry(sb, group, indent + 2, isLast);
    }

    string comma = isLastProperty ? "" : ",";
    sb.AppendLine($"{indentStr}]{comma}");
  }

  /// <summary>
  /// Emits a single group entry with its nested groups and commands.
  /// </summary>
  private static void EmitGroupEntry(StringBuilder sb, GroupNode group, int indent, bool isLast)
  {
    string indentStr = new(' ', indent);
    sb.AppendLine($"{indentStr}{{");

    // Group name
    sb.AppendLine($"{indentStr}  \"name\": \"{EscapeJsonString(group.Name)}\",");

    // Nested groups (if any)
    bool hasNestedGroups = group.Children.Count > 0;
    bool hasCommands = group.Routes.Count > 0;

    if (hasNestedGroups)
    {
      EmitGroups(sb, group.Children, indent + 2, isLastProperty: !hasCommands);
    }

    // Commands in this group
    if (hasCommands)
    {
      EmitGroupCommands(sb, group.Routes, indent + 2);
    }

    // If no nested groups and no commands, emit empty commands array for valid JSON
    if (!hasNestedGroups && !hasCommands)
    {
      sb.AppendLine($"{indentStr}  \"commands\": []");
    }

    string comma = isLast ? "" : ",";
    sb.AppendLine($"{indentStr}}}{comma}");
  }

  /// <summary>
  /// Emits commands within a group.
  /// </summary>
  private static void EmitGroupCommands(StringBuilder sb, List<RouteDefinition> routes, int indent)
  {
    string indentStr = new(' ', indent);
    sb.AppendLine($"{indentStr}\"commands\": [");

    for (int i = 0; i < routes.Count; i++)
    {
      RouteDefinition route = routes[i];
      bool isLast = i == routes.Count - 1;
      EmitCommandEntry(sb, route, indent + 2, isLast);
    }

    sb.AppendLine($"{indentStr}]");
  }

  /// <summary>
  /// Emits ungrouped commands at the top level.
  /// </summary>
  private static void EmitUngroupedCommands(StringBuilder sb, IReadOnlyList<RouteDefinition> routes)
  {
    sb.AppendLine("      \"commands\": [");

    for (int i = 0; i < routes.Count; i++)
    {
      RouteDefinition route = routes[i];
      bool isLast = i == routes.Count - 1;
      EmitCommandEntry(sb, route, indent: 8, isLast);
    }

    sb.AppendLine("      ]");
  }

  /// <summary>
  /// Emits a single command entry in the capabilities JSON.
  /// </summary>
  private static void EmitCommandEntry(StringBuilder sb, RouteDefinition route, int indent, bool isLast)
  {
    string indentStr = new(' ', indent);
    string propIndent = new(' ', indent + 2);

    sb.AppendLine($"{indentStr}{{");

    // Pattern
    string pattern = EscapeJsonString(route.FullPattern);
    sb.AppendLine($"{propIndent}\"pattern\": \"{pattern}\",");

    // Description
    if (route.Description is not null)
    {
      string description = EscapeJsonString(route.Description);
      sb.AppendLine($"{propIndent}\"description\": \"{description}\",");
    }

    // Message type (maps to AI safety level) - convert to kebab-case
    string messageType = ToKebabCase(route.MessageType);
    sb.AppendLine($"{propIndent}\"messageType\": \"{messageType}\",");

    // Parameters
    if (route.Parameters.Any())
    {
      EmitParameters(sb, route, indent + 2);
    }

    // Options
    if (route.Options.Any())
    {
      EmitOptions(sb, route, indent + 2);
    }

    // Aliases
    if (route.Aliases.Length > 0)
    {
      EmitAliases(sb, route, indent + 2);
    }

    string comma = isLast ? "" : ",";
    sb.AppendLine($"{indentStr}}}{comma}");
  }

  /// <summary>
  /// Emits the parameters array for a command.
  /// </summary>
  private static void EmitParameters(StringBuilder sb, RouteDefinition route, int indent)
  {
    string indentStr = new(' ', indent);
    string itemIndent = new(' ', indent + 2);
    string propIndent = new(' ', indent + 4);

    sb.AppendLine($"{indentStr}\"parameters\": [");

    IReadOnlyList<ParameterDefinition> parameters = [.. route.Parameters];
    for (int i = 0; i < parameters.Count; i++)
    {
      ParameterDefinition param = parameters[i];
      bool isLast = i == parameters.Count - 1;
      string comma = isLast ? "" : ",";

      sb.AppendLine($"{itemIndent}{{");
      sb.AppendLine($"{propIndent}\"name\": \"{EscapeJsonString(param.Name)}\",");

      if (param.Description is not null)
      {
        sb.AppendLine($"{propIndent}\"description\": \"{EscapeJsonString(param.Description)}\",");
      }

      sb.AppendLine($"{propIndent}\"required\": {(param.IsOptional ? "false" : "true")},");

      if (param.IsCatchAll)
      {
        sb.AppendLine($"{propIndent}\"catchAll\": true,");
      }

      string typeConstraint = param.TypeConstraint ?? "string";
      sb.AppendLine($"{propIndent}\"type\": \"{EscapeJsonString(typeConstraint)}\"");

      sb.AppendLine($"{itemIndent}}}{comma}");
    }

    sb.AppendLine($"{indentStr}],");
  }

  /// <summary>
  /// Emits the options array for a command.
  /// </summary>
  private static void EmitOptions(StringBuilder sb, RouteDefinition route, int indent)
  {
    string indentStr = new(' ', indent);
    string itemIndent = new(' ', indent + 2);
    string propIndent = new(' ', indent + 4);

    sb.AppendLine($"{indentStr}\"options\": [");

    IReadOnlyList<OptionDefinition> options = [.. route.Options];
    for (int i = 0; i < options.Count; i++)
    {
      OptionDefinition option = options[i];
      bool isLast = i == options.Count - 1;
      string comma = isLast ? "" : ",";

      sb.AppendLine($"{itemIndent}{{");

      if (option.LongForm is not null)
      {
        sb.AppendLine($"{propIndent}\"name\": \"{EscapeJsonString(option.LongForm)}\",");
      }

      if (option.ShortForm is not null)
      {
        sb.AppendLine($"{propIndent}\"alias\": \"{EscapeJsonString(option.ShortForm)}\",");
      }

      if (option.Description is not null)
      {
        sb.AppendLine($"{propIndent}\"description\": \"{EscapeJsonString(option.Description)}\",");
      }

      sb.AppendLine($"{propIndent}\"required\": {(option.IsOptional ? "false" : "true")},");
      sb.AppendLine($"{propIndent}\"isFlag\": {(option.IsFlag ? "true" : "false")}");

      sb.AppendLine($"{itemIndent}}}{comma}");
    }

    sb.AppendLine($"{indentStr}],");
  }

  /// <summary>
  /// Emits the aliases array for a command.
  /// </summary>
  private static void EmitAliases(StringBuilder sb, RouteDefinition route, int indent)
  {
    string indentStr = new(' ', indent);
    sb.Append($"{indentStr}\"aliases\": [");

    IReadOnlyList<string> aliases = [.. route.Aliases];
    for (int i = 0; i < aliases.Count; i++)
    {
      string alias = aliases[i];
      bool isLast = i == aliases.Count - 1;
      string comma = isLast ? "" : ", ";
      sb.Append($"\"{EscapeJsonString(alias)}\"{comma}");
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

  /// <summary>
  /// Converts a PascalCase string to kebab-case.
  /// Example: "IdempotentCommand" -> "idempotent-command"
  /// </summary>
  private static string ToKebabCase(string value)
  {
    if (string.IsNullOrEmpty(value))
      return value;

    StringBuilder result = new();
    for (int i = 0; i < value.Length; i++)
    {
      char c = value[i];
      if (char.IsUpper(c))
      {
        if (i > 0)
          result.Append('-');
        result.Append(char.ToLowerInvariant(c));
      }
      else
      {
        result.Append(c);
      }
    }

    return result.ToString();
  }
}
