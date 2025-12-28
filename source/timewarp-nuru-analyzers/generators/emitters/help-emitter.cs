// Emits help text generation code from route definitions.
// Generates the PrintHelp method for --help flag handling.

namespace TimeWarp.Nuru.Generators;

using System.Text;

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
  public static void Emit(StringBuilder sb, AppModel model)
  {
    sb.AppendLine("  private static void PrintHelp(ITerminal terminal)");
    sb.AppendLine("  {");

    EmitHeader(sb, model);
    EmitUsage(sb, model);
    EmitCommands(sb, model);
    EmitOptions(sb);

    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the help header with app name and description.
  /// </summary>
  private static void EmitHeader(StringBuilder sb, AppModel model)
  {
    if (model.Name is not null)
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"    terminal.WriteLine(\"{EscapeString(model.Name)}\");");
    }

    if (model.Description is not null)
    {
      sb.AppendLine(CultureInfo.InvariantCulture,
        $"    terminal.WriteLine(\"{EscapeString(model.Description)}\");");
      sb.AppendLine("    terminal.WriteLine();");
    }
  }

  /// <summary>
  /// Emits the usage line.
  /// </summary>
  private static void EmitUsage(StringBuilder sb, AppModel model)
  {
    string appName = model.Name ?? "app";
    sb.AppendLine(CultureInfo.InvariantCulture,
      $"    terminal.WriteLine(\"Usage: {EscapeString(appName)} [command] [options]\");");
    sb.AppendLine("    terminal.WriteLine();");
  }

  /// <summary>
  /// Emits the commands section.
  /// </summary>
  private static void EmitCommands(StringBuilder sb, AppModel model)
  {
    if (!model.HasRoutes)
    {
      return;
    }

    sb.AppendLine("    terminal.WriteLine(\"Commands:\");");

    foreach (RouteDefinition route in model.Routes)
    {
      EmitRouteHelp(sb, route);
    }

    sb.AppendLine("    terminal.WriteLine();");
  }

  /// <summary>
  /// Emits help text for a single route.
  /// </summary>
  private static void EmitRouteHelp(StringBuilder sb, RouteDefinition route)
  {
    // Build the pattern display (e.g., "deploy {env} [--force]")
    string pattern = BuildPatternDisplay(route);

    // Pad for alignment (assuming max 25 char pattern)
    string paddedPattern = pattern.PadRight(25);

    string description = route.Description ?? string.Empty;

    sb.AppendLine(CultureInfo.InvariantCulture,
      $"    terminal.WriteLine(\"  {EscapeString(paddedPattern)} {EscapeString(description)}\");");
  }

  /// <summary>
  /// Builds the display pattern for help text.
  /// </summary>
  private static string BuildPatternDisplay(RouteDefinition route)
  {
    StringBuilder pattern = new();

    // Add group prefix if present
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      pattern.Append(route.GroupPrefix);
      pattern.Append(' ');
    }

    // Add segments
    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          pattern.Append(literal.Value);
          pattern.Append(' ');
          break;

        case ParameterDefinition param:
          if (param.IsCatchAll)
          {
            pattern.Append(CultureInfo.InvariantCulture, $"{{*{param.Name}}} ");
          }
          else if (param.IsOptional)
          {
            pattern.Append(CultureInfo.InvariantCulture, $"[{param.Name}] ");
          }
          else
          {
            pattern.Append(CultureInfo.InvariantCulture, $"{{{param.Name}}} ");
          }

          break;

        case OptionDefinition option:
          string optionDisplay = (option.LongForm, option.ShortForm) switch
          {
            (not null, not null) => $"--{option.LongForm},-{option.ShortForm}",
            (not null, null) => $"--{option.LongForm}",
            (null, not null) => $"-{option.ShortForm}",
            _ => "[invalid option]"
          };

          if (option.ExpectsValue)
          {
            optionDisplay += $" {{{option.ParameterName ?? "value"}}}";
          }

          if (option.IsOptional)
          {
            pattern.Append(CultureInfo.InvariantCulture, $"[{optionDisplay}] ");
          }
          else
          {
            pattern.Append(CultureInfo.InvariantCulture, $"{optionDisplay} ");
          }

          break;
      }
    }

    return pattern.ToString().Trim();
  }

  /// <summary>
  /// Emits the global options section.
  /// </summary>
  private static void EmitOptions(StringBuilder sb)
  {
    sb.AppendLine("    terminal.WriteLine(\"Options:\");");
    sb.AppendLine("    terminal.WriteLine(\"  --help, -h             Show this help message\");");
    sb.AppendLine("    terminal.WriteLine(\"  --version              Show version information\");");
    sb.AppendLine("    terminal.WriteLine(\"  --capabilities         Show capabilities for AI tools\");");
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
