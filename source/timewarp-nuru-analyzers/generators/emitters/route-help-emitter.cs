// Emits per-route help text generation code.
// Generates inline help output for "command --help" scenarios.
// Task #356: Per-route help support

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to generate help text for a specific route.
/// Used when a user types "command --help" to show help for just that command.
/// </summary>
internal static class RouteHelpEmitter
{
  /// <summary>
  /// Emits code to check for per-route help (command --help) and display route-specific help.
  /// This should be called at the start of each route's matching block.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route definition to emit help for.</param>
  /// <param name="routeIndex">The index of this route (used for unique label names).</param>
  /// <param name="indent">Indentation level (number of spaces).</param>
  public static void EmitPerRouteHelpCheck(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    int indent = 4)
  {
    string indentStr = new(' ', indent);

    // Get the literal prefix for this route (group prefix + pattern literals)
    List<string> literalPrefix = GetLiteralPrefix(route);

    // If no literals, this route can't have per-route help (e.g., "{*args}")
    if (literalPrefix.Count == 0)
      return;

    // Build the pattern match for: [literal1, literal2, ..., "--help" or "-h"]
    StringBuilder patternBuilder = new();
    patternBuilder.Append('[');
    foreach (string literal in literalPrefix)
    {
      patternBuilder.Append($"\"{EscapeString(literal)}\", ");
    }

    // Use BuiltInFlags constant for help forms
    string helpFormsPattern = string.Join(" or ", BuiltInFlags.HelpForms.Select(f => $"\"{f}\""));
    patternBuilder.Append($"{helpFormsPattern}]");
    string helpPattern = patternBuilder.ToString();

    // Emit the help check
    sb.AppendLine($"{indentStr}// Per-route help: {route.FullPattern} --help");
    sb.AppendLine($"{indentStr}if (routeArgs is {helpPattern})");
    sb.AppendLine($"{indentStr}" + "{");

    // Emit the help content inline
    EmitRouteHelpContent(sb, route, indent + 2);

    sb.AppendLine($"{indentStr}  return 0;");
    sb.AppendLine($"{indentStr}" + "}");
    sb.AppendLine();
  }

  /// <summary>
  /// Gets the literal prefix for a route (group prefix literals + pattern literals).
  /// These are the literals that must match before --help for per-route help.
  /// </summary>
  private static List<string> GetLiteralPrefix(RouteDefinition route)
  {
    List<string> literals = [];

    // Add group prefix literals if present
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      literals.AddRange(route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    // Add pattern literals (stop at first non-literal segment)
    foreach (SegmentDefinition segment in route.Segments)
    {
      if (segment is LiteralDefinition literal)
      {
        literals.Add(literal.Value);
      }
      else
      {
        // Stop at first parameter/option - we only match the literal prefix
        break;
      }
    }

    return literals;
  }

  /// <summary>
  /// Emits the help content for a specific route.
  /// </summary>
  private static void EmitRouteHelpContent(StringBuilder sb, RouteDefinition route, int indent)
  {
    string indentStr = new(' ', indent);

    // Pattern line (e.g., "deploy {env} [--dry-run,-d] [--force,-f]")
    string pattern = HelpPatternHelper.BuildPatternDisplay(route);
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"{EscapeString(pattern)}\");");

    // Description (if present)
    if (!string.IsNullOrEmpty(route.Description))
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"  {EscapeString(route.Description)}\");");
    }

    // Parameters section
    IEnumerable<ParameterDefinition> parameters = route.Parameters.ToList();
    if (parameters.Any())
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"Parameters:\");");
      sb.AppendLine($"{indentStr}app.Terminal.WriteTable(table => table");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Name\")");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Required\")");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Type\")");

      foreach (ParameterDefinition param in parameters)
      {
        string name = param.Name;
        if (param.IsCatchAll)
        {
          name = $"*{name}";
        }

        string required = param.IsOptional ? "No" : "Yes";
        string type = param.HasTypeConstraint && param.TypeConstraint is not null
          ? param.TypeConstraint
          : "string";

        sb.AppendLine($"{indentStr}  .AddRow(\"{EscapeString(name)}\", \"{EscapeString(required)}\", \"{EscapeString(type)}\")");
      }

      sb.AppendLine($"{indentStr});");
    }

    // Options section
    IEnumerable<OptionDefinition> options = route.Options.ToList();
    if (options.Any())
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"Options:\");");
      sb.AppendLine($"{indentStr}app.Terminal.WriteTable(table => table");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Option\")");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Description\")");

      foreach (OptionDefinition option in options)
      {
        string optionDisplay = (option.LongForm, option.ShortForm) switch
        {
          (not null, not null) => $"--{option.LongForm}, -{option.ShortForm}",
          (not null, null) => $"--{option.LongForm}",
          (null, not null) => $"-{option.ShortForm}",
          _ => "[invalid]"
        };

        if (option.ExpectsValue)
        {
          string valueName = option.ParameterName ?? "value";
          if (option.ParameterIsOptional)
          {
            optionDisplay += $" [{valueName}]";
          }
          else
          {
            optionDisplay += $" <{valueName}>";
          }
        }

        string description = option.Description ?? (option.IsOptional ? "(optional)" : "");

        sb.AppendLine($"{indentStr}  .AddRow(\"{EscapeString(optionDisplay)}\", \"{EscapeString(description)}\")");
      }

      sb.AppendLine($"{indentStr});");
    }
  }

  /// <summary>
  /// Emits code to check for group-level help (e.g., "worktree --help") and display group-specific help.
  /// This should be called before user routes so groups get priority.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="routes">All routes to group by prefix.</param>
  /// <param name="indent">Indentation level (number of spaces).</param>
  public static void EmitGroupHelpChecks(
    StringBuilder sb,
    IEnumerable<RouteDefinition> routes,
    int indent = 4)
  {
    string indentStr = new(' ', indent);

    // Group routes by GroupPrefix using case-insensitive comparison
    IEnumerable<IGrouping<string, RouteDefinition>> groups = routes
      .Where(r => !string.IsNullOrEmpty(r.GroupPrefix))
      .GroupBy(r => r.GroupPrefix!, StringComparer.OrdinalIgnoreCase);

    foreach (IGrouping<string, RouteDefinition> group in groups)
    {
      string groupPrefix = group.Key;
      List<RouteDefinition> groupRoutes = [.. group];

      // Split prefix by spaces into words
      string[] words = groupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      // Build pattern: ["word1", "word2", ..., "--help" or "-h"]
      StringBuilder patternBuilder = new();
      patternBuilder.Append('[');
      foreach (string word in words)
      {
        patternBuilder.Append($"\"{EscapeString(word)}\", ");
      }

      // Use BuiltInFlags constant for help forms
      string helpFormsPattern = string.Join(" or ", BuiltInFlags.HelpForms.Select(f => $"\"{f}\""));
      patternBuilder.Append($"{helpFormsPattern}]");
      string helpPattern = patternBuilder.ToString();

      // Emit the group help check
      sb.AppendLine($"{indentStr}// Group-level help: {groupPrefix} --help");
      sb.AppendLine($"{indentStr}if (routeArgs is {helpPattern})");
      sb.AppendLine($"{indentStr}" + "{");

      // Emit the group help content inline
      EmitGroupHelpContent(sb, groupPrefix, groupRoutes, indent + 2);

      sb.AppendLine($"{indentStr}  return 0;");
      sb.AppendLine($"{indentStr}" + "}");
      sb.AppendLine();
    }
  }

  /// <summary>
  /// Emits the help content for a group of routes.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="groupPrefix">The group prefix (e.g., "worktree").</param>
  /// <param name="routes">The routes in this group.</param>
  /// <param name="indent">Indentation level (number of spaces).</param>
  private static void EmitGroupHelpContent(
    StringBuilder sb,
    string groupPrefix,
    List<RouteDefinition> routes,
    int indent)
  {
    string indentStr = new(' ', indent);

    // Header line
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"{EscapeString(groupPrefix)} commands:\");");
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");

    // Table of routes in the group
    sb.AppendLine($"{indentStr}app.Terminal.WriteTable(table => table");
    sb.AppendLine($"{indentStr}  .AddColumn(\"Command\")");
    sb.AppendLine($"{indentStr}  .AddColumn(\"Description\")");

    foreach (RouteDefinition route in routes)
    {
      // Build display pattern (show the original pattern, not the full pattern with group prefix)
      string pattern = HelpPatternHelper.BuildPatternDisplay(route);

      // Remove the group prefix from the beginning of the pattern for display
      // (since the header already shows the group)
      string displayPattern = pattern;
      if (displayPattern.StartsWith(groupPrefix, StringComparison.OrdinalIgnoreCase))
      {
        displayPattern = displayPattern[groupPrefix.Length..].TrimStart();
      }

      string description = route.Description ?? "";

      sb.AppendLine($"{indentStr}  .AddRow(\"{EscapeString(displayPattern)}\", \"{EscapeString(description)}\")");
    }

    sb.AppendLine($"{indentStr});");
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
}
