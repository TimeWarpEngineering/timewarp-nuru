// Emits help text generation code from route definitions.
// Generates the PrintHelp method for --help flag handling.

namespace TimeWarp.Nuru.Generators;

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Emits code to generate help text from routes.
/// Creates the PrintHelp method that displays usage information.
/// </summary>
internal static class HelpEmitter
{
  /// <summary>
  /// Emits the PrintHelp method for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="model">The application model containing routes and metadata.</param>
  /// <param name="methodSuffix">Suffix for method name (e.g., "_0" for multi-app assemblies).</param>
  public static void Emit(StringBuilder sb, AppModel model, string methodSuffix = "")
  {
    sb.AppendLine($"  private static void PrintHelp{methodSuffix}(ITerminal terminal)");
    sb.AppendLine("  {");

    // Emit runtime code to get app name with fallback to assembly name
    sb.AppendLine("    // Get app name: explicit > assembly name > \"app\"");
    if (model.Name is not null)
    {
      sb.AppendLine($"    string __appName = \"{EmitterStringUtils.EscapeForStringLiteral(model.Name)}\";");
    }
    else
    {
      sb.AppendLine("    string __appName = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? \"app\";");
    }

    EmitHeader(sb, model);
    EmitUsage(sb);
    EmitOptions(sb, model);   // ← OPTIONS first (Aspire style)
    EmitCommands(sb, model);  // ← Then COMMANDS

    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the help header with app name and description.
  /// </summary>
  private static void EmitHeader(StringBuilder sb, AppModel model)
  {
    // Version with colon format
    string version = model.Version ?? "1.0.0";
    sb.AppendLine("    terminal.WriteLine(\"Version:\".BrightCyan().Bold());");
    sb.AppendLine($"    terminal.WriteLine(\"  v{EmitterStringUtils.EscapeForStringLiteral(version)}\".BrightCyan().Bold());");
    sb.AppendLine("    terminal.WriteLine();");

    // App description with "Description:" header and indented value (default colors for light/dark mode compatibility)
    if (model.Description is not null)
    {
      sb.AppendLine("    terminal.WriteLine(\"Description:\");");
      sb.AppendLine($"    terminal.WriteLine(\"  {EmitterStringUtils.EscapeForStringLiteral(model.Description)}\");");
    }

    sb.AppendLine("    terminal.WriteLine();");
  }

  /// <summary>
  /// Emits the usage line.
  /// </summary>
  private static void EmitUsage(StringBuilder sb)
  {
    sb.AppendLine("    terminal.WriteLine(\"Usage:\".Yellow());");
    sb.AppendLine("    terminal.WriteLine($\"  {__appName} [command] [options]\".Yellow());");
    sb.AppendLine("    terminal.WriteLine();");
  }

  /// <summary>
  /// Emits the commands section as a table.
  /// </summary>
  private static void EmitCommands(StringBuilder sb, AppModel model)
  {
    HelpModel helpOptions = model.HelpOptions ?? HelpModel.Default;
    List<RouteDefinition> visibleRoutes = [.. model.Routes.Where(r => ShouldListRoute(r, helpOptions))];

    bool hasUserCommands = visibleRoutes.Count > 0;
    bool showReplCommands = helpOptions.ShowReplCommandsInCli && model.HasRepl;
    if (!hasUserCommands && !showReplCommands)
    {
      return;
    }

    // Group routes by GroupPrefix
    IEnumerable<IGrouping<string, RouteDefinition>> groups = visibleRoutes
      .GroupBy(r => r.GroupPrefix ?? "") // Empty string for no group
      .OrderBy(g => g.Key); // Ungrouped first, then alphabetically

    bool firstGroup = true;
    foreach (IGrouping<string, RouteDefinition> group in groups)
    {
      string categoryName = string.IsNullOrEmpty(group.Key)
        ? "Commands"
        : group.Key;  // Keep original case, not ToUpperInvariant()

      // Category header in cyan bold
      if (!firstGroup)
      {
        sb.AppendLine("    terminal.WriteLine();");
      }

      sb.AppendLine($"    terminal.WriteLine(\"{EmitterStringUtils.EscapeForStringLiteral(categoryName)}:\".Cyan().Bold());");

      // Table with command names only (not full patterns)
      sb.AppendLine("    terminal.WriteTable(table => table");
      sb.AppendLine("      .AddColumn(\"Command\")");
      sb.AppendLine("      .AddColumn(\"Description\")");

      foreach (RouteDefinition route in group)
      {
        string commandName = GetCommandName(route);
        string description = route.Description ?? "";
        sb.AppendLine($"      .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(commandName)}\", \"{EmitterStringUtils.EscapeForStringLiteral(description)}\")");

        if (helpOptions.ShowPerCommandHelpRoutes)
        {
          sb.AppendLine($"      .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(commandName)} --help\", \"Show help for this command\")");
        }
      }

      sb.AppendLine("      .HideHeaders()         // ← Remove headers");
      sb.AppendLine("    );");
      firstGroup = false;
    }

    if (showReplCommands)
    {
      if (!firstGroup)
      {
        sb.AppendLine("    terminal.WriteLine();");
      }

      sb.AppendLine("    terminal.WriteLine(\"REPL:\".Cyan().Bold());");
      sb.AppendLine("    terminal.WriteTable(table => table");
      sb.AppendLine("      .AddColumn(\"Command\")");
      sb.AppendLine("      .AddColumn(\"Description\")");
      sb.AppendLine("      .AddRow(\"exit\", \"Exit the REPL\")");
      sb.AppendLine("      .AddRow(\"quit\", \"Exit the REPL\")");
      sb.AppendLine("      .AddRow(\"q\", \"Exit the REPL\")");
      sb.AppendLine("      .AddRow(\"clear\", \"Clear the screen\")");
      sb.AppendLine("      .AddRow(\"cls\", \"Clear the screen\")");
      sb.AppendLine("      .AddRow(\"clear-history\", \"Clear command history\")");
      sb.AppendLine("      .AddRow(\"history\", \"Show command history\")");
      sb.AppendLine("      .AddRow(\"help\", \"Show REPL help\")");
      sb.AppendLine("      .HideHeaders()");
      sb.AppendLine("    );");
    }
  }

  // Helper to get just the command name
  private static string GetCommandName(RouteDefinition route)
  {
    // If has group prefix, use FullPattern
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      return route.FullPattern;
    }

    // Otherwise, get just the first literal segment
    foreach (SegmentDefinition segment in route.Segments)
    {
      if (segment is LiteralDefinition literal)
      {
        return literal.Value;
      }
    }

    // Fallback to FullPattern if no literal found
    return route.FullPattern;
  }

  /// <summary>
  /// Emits the global options section as a table.
  /// </summary>
  private static void EmitOptions(StringBuilder sb, AppModel model)
  {
    HelpModel helpOptions = model.HelpOptions ?? HelpModel.Default;

    sb.AppendLine("    terminal.WriteLine(\"Options:\".Cyan().Bold());");
    sb.AppendLine("    terminal.WriteTable(table => table");
    sb.AppendLine("      .AddColumn(\"Option\")");
    sb.AppendLine("      .AddColumn(\"Description\")");
    sb.AppendLine("      .AddRow(\"--help, -h\", \"Show this help message\")");
    sb.AppendLine("      .AddRow(\"--version\", \"Show version information\")");
    sb.AppendLine("      .AddRow(\"--capabilities\", \"Show capabilities for AI tools\")");
    sb.AppendLine("      .AddRow(\"--capabilities --group-filter <group>\", \"Filter capabilities by group prefix\")");
    sb.AppendLine("      .AddRow(\"--capabilities --search <query>\", \"Search capabilities using nuru\")");
    if (helpOptions.ShowCompletionRoutes && model.HasCompletion)
    {
      sb.AppendLine("      .AddRow(\"__complete\", \"Shell completion callback\")");
      sb.AppendLine("      .AddRow(\"--generate-completion\", \"Generate a shell completion script\")");
      sb.AppendLine("      .AddRow(\"--install-completion\", \"Install shell completion\")");
    }

    sb.AppendLine("      .HideHeaders()         // ← Remove headers");
    sb.AppendLine("    );");
  }

  /// <summary>
  /// Returns true when a user route should appear in CLI help listings.
  /// </summary>
  private static bool ShouldListRoute(RouteDefinition route, HelpModel helpOptions)
  {
    if (!helpOptions.ShowPerCommandHelpRoutes && IsPerCommandHelpRoute(route))
    {
      return false;
    }

    string commandName = GetCommandName(route);
    foreach (string pattern in helpOptions.ExcludePatterns)
    {
      if (MatchesWildcard(commandName, pattern) || MatchesWildcard(route.FullPattern, pattern) || MatchesWildcard(route.OriginalPattern, pattern))
      {
        return false;
      }
    }

    return true;
  }

  private static bool IsPerCommandHelpRoute(RouteDefinition route)
  {
    foreach (SegmentDefinition segment in route.Segments)
    {
      if (segment is OptionDefinition option
        && !option.ExpectsValue
        && option.LongForm is "help")
      {
        return true;
      }
    }

    return false;
  }

  private static bool MatchesWildcard(string value, string pattern)
  {
    if (string.IsNullOrEmpty(pattern))
    {
      return false;
    }

    string regex = "^" + Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal) + "$";
    return Regex.IsMatch(value, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
  }

}
