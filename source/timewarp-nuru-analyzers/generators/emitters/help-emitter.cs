#region Purpose
// Emits help text generation code from route definitions.
// Generates the PrintHelp method for --help flag handling.
#endregion

#region Design
// A root default route has no command literal, so per-route help never runs for it
// (RouteHelpEmitter skips a route whose literal prefix is empty). Root help prints that
// route's parameters, options, examples, and description on the root page and omits the
// blank command row. A description-only default route stays under Commands, which is the
// only place its description is shown. A named command stays a command row; its parameters
// stay on that command's help page.
#endregion

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

    HelpModel helpOptions = model.HelpOptions ?? HelpModel.Default;
    List<RouteDefinition> visibleRoutes = [.. model.Routes.Where(route => ShouldListRoute(route, helpOptions))];
    List<RouteDefinition> promotedRoutes = [.. visibleRoutes.Where(IsPromotedRootDefault)];
    bool hasOtherCommands = visibleRoutes.Count > promotedRoutes.Count;

    EmitHeader(sb, model);
    EmitUsage(sb, promotedRoutes, hasOtherCommands);
    EmitPromotedDescriptions(sb, promotedRoutes);
    EmitPromotedParameters(sb, promotedRoutes);
    EmitOptions(sb, model, promotedRoutes);   // ← OPTIONS first (Aspire style)
    EmitPromotedExamples(sb, promotedRoutes);
    EmitCommands(sb, model, visibleRoutes);  // ← Then COMMANDS

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
  /// Emits the usage line. A promoted default route adds its own pattern. The pattern is
  /// concatenated, not interpolated, because parameter braces are not format holes.
  /// </summary>
  private static void EmitUsage(StringBuilder sb, List<RouteDefinition> promotedRoutes, bool hasOtherCommands)
  {
    sb.AppendLine("    terminal.WriteLine(\"Usage:\".Yellow());");
    foreach (RouteDefinition route in promotedRoutes)
    {
      string pattern = HelpPatternHelper.BuildPatternDisplay(route);
      if (pattern.Length == 0)
      {
        continue;
      }

      string escaped = EmitterStringUtils.EscapeForStringLiteral(pattern);
      sb.AppendLine($"    terminal.WriteLine((\"  \" + __appName + \" {escaped}\").Yellow());");
    }

    if (promotedRoutes.Count == 0 || hasOtherCommands)
    {
      sb.AppendLine("    terminal.WriteLine($\"  {__appName} [command] [options]\".Yellow());");
    }
    else
    {
      sb.AppendLine("    terminal.WriteLine($\"  {__appName} [options]\".Yellow());");
    }

    sb.AppendLine("    terminal.WriteLine();");
  }

  /// <summary>
  /// Emits the commands section as a table.
  /// </summary>
  private static void EmitCommands(StringBuilder sb, AppModel model, List<RouteDefinition> visibleRoutes)
  {
    HelpModel helpOptions = model.HelpOptions ?? HelpModel.Default;
    List<RouteDefinition> commandRoutes = [.. visibleRoutes.Where(route => !IsPromotedRootDefault(route))];

    bool hasUserCommands = commandRoutes.Count > 0;
    bool showReplCommands = helpOptions.ShowReplCommandsInCli && model.HasRepl;
    if (!hasUserCommands && !showReplCommands)
    {
      return;
    }

    // Separate the Options table from the first command heading.
    sb.AppendLine("    terminal.WriteLine();");

    // Group routes by GroupPrefix
    IEnumerable<IGrouping<string, RouteDefinition>> groups = commandRoutes
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
      sb.AppendLine("      .AddRow(\"key-bindings\", \"Show REPL key bindings\")");
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
  /// A root default route has no group prefix and no command literal, so its command
  /// cell would be blank and per-route help cannot address it.
  /// </summary>
  private static bool IsRootDefaultRoute(RouteDefinition route)
  {
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      return false;
    }

    return string.IsNullOrWhiteSpace(GetCommandName(route));
  }

  /// <summary>
  /// A default route with parameters, options, or examples is printed on the root page.
  /// A description-only default route stays in the command list.
  /// </summary>
  private static bool IsPromotedRootDefault(RouteDefinition route)
  {
    if (!IsRootDefaultRoute(route))
    {
      return false;
    }

    return route.Parameters.Any() || route.Options.Any() || route.HasExamples;
  }

  private static void EmitPromotedDescriptions(StringBuilder sb, List<RouteDefinition> promotedRoutes)
  {
    bool wroteDescription = false;
    foreach (RouteDefinition route in promotedRoutes)
    {
      if (string.IsNullOrEmpty(route.Description))
      {
        continue;
      }

      sb.AppendLine($"    terminal.WriteLine(\"  {EmitterStringUtils.EscapeForStringLiteral(route.Description)}\");");
      wroteDescription = true;
    }

    if (wroteDescription)
    {
      sb.AppendLine("    terminal.WriteLine();");
    }
  }

  private static void EmitPromotedParameters(StringBuilder sb, List<RouteDefinition> promotedRoutes)
  {
    List<ParameterDefinition> parameters = [.. promotedRoutes.SelectMany(route => route.Parameters)];
    if (parameters.Count == 0)
    {
      return;
    }

    bool anyDescriptions = parameters.Any(parameter => !string.IsNullOrEmpty(parameter.Description));

    sb.AppendLine("    terminal.WriteLine(\"Parameters:\".Cyan().Bold());");
    sb.AppendLine("    terminal.WriteTable(table => table");
    sb.AppendLine("      .AddColumn(\"Name\")");
    sb.AppendLine("      .AddColumn(\"Required\")");
    sb.AppendLine("      .AddColumn(\"Type\")");
    if (anyDescriptions)
    {
      sb.AppendLine("      .AddColumn(\"Description\")");
    }

    foreach (ParameterDefinition parameter in parameters)
    {
      string name;
      if (parameter.IsRaw)
        name = $"**{parameter.Name}";
      else if (parameter.IsCatchAll)
        name = $"*{parameter.Name}";
      else
        name = parameter.Name;

      string required = parameter.IsOptional ? "No" : "Yes";
      string type;
      if (parameter.IsRaw && !parameter.HasTypeConstraint)
        type = "string[]";
      else if (parameter.HasTypeConstraint && parameter.TypeConstraint is not null)
        type = parameter.TypeConstraint;
      else
        type = "string";

      if (anyDescriptions)
      {
        string description = parameter.Description ?? "";
        sb.AppendLine($"      .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(name)}\", \"{EmitterStringUtils.EscapeForStringLiteral(required)}\", \"{EmitterStringUtils.EscapeForStringLiteral(type)}\", \"{EmitterStringUtils.EscapeForStringLiteral(description)}\")");
      }
      else
      {
        sb.AppendLine($"      .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(name)}\", \"{EmitterStringUtils.EscapeForStringLiteral(required)}\", \"{EmitterStringUtils.EscapeForStringLiteral(type)}\")");
      }
    }

    sb.AppendLine("    );");
    sb.AppendLine("    terminal.WriteLine();");
  }

  private static void EmitPromotedExamples(StringBuilder sb, List<RouteDefinition> promotedRoutes)
  {
    bool anyExamples = promotedRoutes.Any(route => route.HasExamples);
    if (!anyExamples)
    {
      return;
    }

    sb.AppendLine("    terminal.WriteLine();");
    sb.AppendLine("    terminal.WriteLine(\"Examples:\".Cyan().Bold());");
    foreach (RouteDefinition route in promotedRoutes)
    {
      if (!route.HasExamples)
      {
        continue;
      }

      foreach (ExampleDefinition example in route.Examples)
      {
        sb.AppendLine($"    terminal.WriteLine(\"  {EmitterStringUtils.EscapeForStringLiteral(example.Command)}\");");
        if (!string.IsNullOrEmpty(example.Description))
        {
          sb.AppendLine($"    terminal.WriteLine(\"    \" + \"{EmitterStringUtils.EscapeForStringLiteral(example.Description)}\".Dim());");
        }
      }
    }
  }

  private static string FormatOptionDisplay(OptionDefinition option)
  {
    string optionDisplay = (option.LongForm, option.ShortForm) switch
    {
      (not null, not null) => $"--{option.LongForm}, -{option.ShortForm}",
      (not null, null) => $"--{option.LongForm}",
      (null, not null) => $"-{option.ShortForm}",
      _ => "[invalid]"
    };

    if (!option.ExpectsValue)
    {
      return optionDisplay;
    }

    string valueName = option.ParameterName ?? "value";
    return option.ParameterIsOptional
      ? $"{optionDisplay} [{valueName}]"
      : $"{optionDisplay} <{valueName}>";
  }

  /// <summary>
  /// Emits the global options section as a table.
  /// </summary>
  private static void EmitOptions(StringBuilder sb, AppModel model, List<RouteDefinition> promotedRoutes)
  {
    HelpModel helpOptions = model.HelpOptions ?? HelpModel.Default;

    sb.AppendLine("    terminal.WriteLine(\"Options:\".Cyan().Bold());");
    sb.AppendLine("    terminal.WriteTable(table => table");
    sb.AppendLine("      .AddColumn(\"Option\")");
    sb.AppendLine("      .AddColumn(\"Description\")");
    foreach (RouteDefinition route in promotedRoutes)
    {
      foreach (OptionDefinition option in route.Options)
      {
        string optionDisplay = FormatOptionDisplay(option);
        string description = option.Description ?? (option.IsOptional ? "(optional)" : "");
        sb.AppendLine($"      .AddRow(\"{EmitterStringUtils.EscapeForStringLiteral(optionDisplay)}\", \"{EmitterStringUtils.EscapeForStringLiteral(description)}\")");
      }
    }

    sb.AppendLine("      .AddRow(\"--help, -h\", \"Show this help message\")");
    sb.AppendLine("      .AddRow(\"--version\", \"Show version information\")");
    sb.AppendLine("      .AddRow(\"--capabilities\", \"Show capabilities for AI tools\")");
    sb.AppendLine("      .AddRow(\"--capabilities --group-filter <group>\", \"Filter capabilities by group prefix\")");
    sb.AppendLine("      .AddRow(\"--capabilities --search <query>\", \"Search capabilities using nuru\")");
    sb.AppendLine("      .AddRow(\"--json-args <value>\", \"Bind arguments from -, @path, or an inline JSON object\")");
    if (model.McpRoute is not null)
    {
      sb.AppendLine($"      .AddRow(\"{BuiltInFlags.McpRouteForm}\", \"Serve the routes as MCP tools over stdio\")");
    }

    if (model.ServeRoute is not null)
    {
      sb.AppendLine($"      .AddRow(\"{BuiltInFlags.ServeRouteForm}\", \"Serve the routes from a local server that later calls forward to\")");
      sb.AppendLine($"      .AddRow(\"{BuiltInFlags.ServeRouteForm} --status | --stop\", \"Report or stop the running server\")");
      sb.AppendLine("      .AddRow(\"--daemon | --no-daemon\", \"Forward this call to the server (starting it) or run it here\")");
    }

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
