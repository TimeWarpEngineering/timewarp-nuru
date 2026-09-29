// Emits per-route help text generation code.
// Generates inline help output for "command --help" scenarios.
// Task #356: Per-route help support
// Task #370: several routes with the same leading literals share one help invocation.

#region Purpose
// Emit help text for "command --help", including every route that shares that literal prefix.
#endregion

#region Design
// A help invocation matches routes whose leading literal segments equal the typed words.
// One match prints that route. Two or more print each route, most specific first, in the
// same per-route layout. Longer commands (deploy status) and near-prefixes (deployment)
// are different literal sequences, so they stay out. Group-prefix words are part of the
// leading literals, which keeps another group's routes out of this invocation.
// Shared-prefix help is emitted before group-summary help so an empty subcommand and
// {env} under the same group prefix still print per-route blocks.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to generate help text for a route, or for every route that shares a literal prefix.
/// </summary>
internal static class RouteHelpEmitter
{
  /// <summary>
  /// Emits code to check for per-route help (command --help) and display route-specific help.
  /// This should be called at the start of each route's matching block.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="route">The route definition to emit help for.</param>
  /// <param name="routeIndex">The index of this route, written into the generated comment.</param>
  /// <param name="sharedPrefixHelpRoutes">
  /// Routes already printed by <see cref="EmitSharedPrefixHelpChecks"/>. Those routes skip
  /// the single-route check so the same arguments are not handled twice.
  /// </param>
  /// <param name="indent">Indentation level (number of spaces).</param>
  public static void EmitPerRouteHelpCheck(
    StringBuilder sb,
    RouteDefinition route,
    int routeIndex,
    HashSet<RouteDefinition>? sharedPrefixHelpRoutes = null,
    int indent = 4)
  {
    if (sharedPrefixHelpRoutes?.Contains(route) == true)
      return;

    string indentStr = new(' ', indent);

    // Get the literal prefix for this route (group prefix + pattern literals)
    List<string> literalPrefix = GetLiteralPrefix(route);

    // If no literals, this route can't have per-route help (e.g., "{*args}")
    if (literalPrefix.Count == 0)
      return;

    string helpPattern = BuildHelpArgsPattern(literalPrefix);

    // Emit the help check
    sb.AppendLine($"{indentStr}// Per-route help [{routeIndex}]: {route.FullPattern} --help");
    sb.AppendLine($"{indentStr}if (routeArgs is {helpPattern})");
    sb.AppendLine($"{indentStr}" + "{");

    // Emit the help content inline
    EmitRouteHelpContent(sb, route, indent + 2);

    sb.AppendLine($"{indentStr}  return 0;");
    sb.AppendLine($"{indentStr}" + "}");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits one help check per literal prefix shared by two or more routes.
  /// Each check prints every sharing route, highest specificity first.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="routes">Routes that can answer a help invocation.</param>
  /// <param name="indent">Indentation level (number of spaces).</param>
  /// <returns>Routes covered by a shared-prefix check.</returns>
  public static HashSet<RouteDefinition> EmitSharedPrefixHelpChecks(
    StringBuilder sb,
    IEnumerable<RouteDefinition> routes,
    int indent = 4)
  {
    HashSet<RouteDefinition> covered = new(ReferenceEqualityComparer.Instance);
    string indentStr = new(' ', indent);

    List<(RouteDefinition Route, string Key, List<string> Prefix)> prefixed = [];
    foreach (RouteDefinition route in routes)
    {
      List<string> prefix = GetLiteralPrefix(route);
      if (prefix.Count == 0)
        continue;

      prefixed.Add((route, string.Join('\u001f', prefix), prefix));
    }

    foreach (IGrouping<string, (RouteDefinition Route, string Key, List<string> Prefix)> group in
      prefixed.GroupBy(entry => entry.Key, StringComparer.Ordinal))
    {
      List<(RouteDefinition Route, string Key, List<string> Prefix)> matches = [.. group];
      if (matches.Count < 2)
        continue;

      List<RouteDefinition> ordered =
      [
        .. matches
          .Select(entry => entry.Route)
          .OrderByDescending(route => route.ComputedSpecificity)
      ];

      string helpPattern = BuildHelpArgsPattern(matches[0].Prefix);
      string patterns = string.Join(", ", ordered.Select(route => route.FullPattern));

      sb.AppendLine($"{indentStr}// Shared-prefix help: {patterns}");
      sb.AppendLine($"{indentStr}if (routeArgs is {helpPattern})");
      sb.AppendLine($"{indentStr}" + "{");

      for (int index = 0; index < ordered.Count; index++)
      {
        if (index > 0)
          sb.AppendLine($"{indentStr}  app.Terminal.WriteLine();");

        EmitRouteHelpContent(sb, ordered[index], indent + 2);
        covered.Add(ordered[index]);
      }

      sb.AppendLine($"{indentStr}  return 0;");
      sb.AppendLine($"{indentStr}" + "}");
      sb.AppendLine();
    }

    return covered;
  }

  /// <summary>
  /// Builds the list pattern for leading literals followed by a help flag.
  /// </summary>
  private static string BuildHelpArgsPattern(IReadOnlyList<string> literalPrefix)
  {
    StringBuilder patternBuilder = new();
    patternBuilder.Append('[');
    foreach (string literal in literalPrefix)
    {
      patternBuilder.Append($"\"{EmitterStringUtils.EscapeForStringLiteral(literal)}\", ");
    }

    string helpFormsPattern = string.Join(" or ", BuiltInFlags.HelpForms.Select(form => $"\"{form}\""));
    patternBuilder.Append($"{helpFormsPattern}]");
    return patternBuilder.ToString();
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
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"{EmitterStringUtils.EscapeForStringLiteral(pattern)}\");");

    // Description (if present)
    if (!string.IsNullOrEmpty(route.Description))
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"  {EmitterStringUtils.EscapeForStringLiteral(route.Description)}\");");
    }

    // Parameters section
    List<ParameterDefinition> parameters = [.. route.Parameters];
    if (parameters.Count > 0)
    {
      // Only show the Description column when at least one parameter has a description.
      bool anyDescriptions = parameters.Any(p => !string.IsNullOrEmpty(p.Description));

      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"Parameters:\");");
      sb.AppendLine($"{indentStr}app.Terminal.WriteTable(table => table");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Name\")");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Required\")");
      sb.AppendLine($"{indentStr}  .AddColumn(\"Type\")");
      if (anyDescriptions)
      {
        sb.AppendLine($"{indentStr}  .AddColumn(\"Description\")");
      }

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

        if (anyDescriptions)
        {
          string description = param.Description ?? "";
          sb.AppendLine($"{indentStr}  .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(name)}\", \"{EmitterStringUtils.EscapeForStringLiteral(required)}\", \"{EmitterStringUtils.EscapeForStringLiteral(type)}\", \"{EmitterStringUtils.EscapeForStringLiteral(description)}\")");
        }
        else
        {
          sb.AppendLine($"{indentStr}  .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(name)}\", \"{EmitterStringUtils.EscapeForStringLiteral(required)}\", \"{EmitterStringUtils.EscapeForStringLiteral(type)}\")");
        }
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

        sb.AppendLine($"{indentStr}  .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(optionDisplay)}\", \"{EmitterStringUtils.EscapeForStringLiteral(description)}\")");
      }

      sb.AppendLine($"{indentStr});");
    }

    // Examples section (full-width lines, not a table - help tables truncate cells)
    if (route.HasExamples)
    {
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
      sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"Examples:\");");

      foreach (ExampleDefinition example in route.Examples)
      {
        sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"  {EmitterStringUtils.EscapeForStringLiteral(example.Command)}\");");

        if (!string.IsNullOrEmpty(example.Description))
        {
          sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"    \" + \"{EmitterStringUtils.EscapeForStringLiteral(example.Description)}\".Dim());");
        }
      }
    }

    sb.AppendLine($"{indentStr}app.Terminal.WriteLine();");
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"Values may come from --json-args.\");");
  }

  /// <summary>
  /// Emits code to check for group-level help (e.g., "worktree --help") and display group-specific help.
  /// Call this after shared-prefix help and before route matching. A single route under the
  /// group prefix still reaches this summary; two or more routes that share the prefix are
  /// already printed by <see cref="EmitSharedPrefixHelpChecks"/>.
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

      string helpPattern = BuildHelpArgsPattern(words);

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
    sb.AppendLine($"{indentStr}app.Terminal.WriteLine(\"{EmitterStringUtils.EscapeForStringLiteral(groupPrefix)} commands:\");");
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

      sb.AppendLine($"{indentStr}  .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(displayPattern)}\", \"{EmitterStringUtils.EscapeForStringLiteral(description)}\")");
    }

    sb.AppendLine($"{indentStr});");
  }

}
