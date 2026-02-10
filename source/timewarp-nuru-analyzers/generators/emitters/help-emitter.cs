// Emits help text generation code from route definitions.
// Generates the PrintHelp method for --help flag handling.

namespace TimeWarp.Nuru.Generators;

using System.Linq;
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
  /// <param name="methodSuffix">Suffix for method name (e.g., "_0" for multi-app assemblies).</param>
  public static void Emit(StringBuilder sb, AppModel model, string methodSuffix = "")
  {
    sb.AppendLine($"  private static void PrintHelp{methodSuffix}(ITerminal terminal)");
    sb.AppendLine("  {");

    // Emit runtime code to get app name with fallback to assembly name
    sb.AppendLine("    // Get app name: explicit > assembly name > \"app\"");
    if (model.Name is not null)
    {
      sb.AppendLine($"    string __appName = \"{EscapeString(model.Name)}\";");
    }
    else
    {
      sb.AppendLine("    string __appName = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? \"app\";");
    }

    EmitHeader(sb, model);
    EmitUsage(sb);
    EmitOptions(sb);          // ← OPTIONS first (Aspire style)
    EmitCommands(sb, model);  // ← Then COMMANDS

    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the help header with app name and description.
  /// </summary>
  private static void EmitHeader(StringBuilder sb, AppModel model)
  {
    // App name with version in cyan bold
    string version = model.Version ?? "1.0.0";
    sb.AppendLine($"    terminal.WriteLine($\"  {{__appName}} v{version}\".BrightCyan().Bold());");

    // App description with "Description:" header and indented value
    if (model.Description is not null)
    {
      sb.AppendLine("    terminal.WriteLine(\"Description:\".Gray());");
      sb.AppendLine($"    terminal.WriteLine($\"  {EscapeString(model.Description)}\".Gray());");
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
    if (!model.HasRoutes)
    {
      return;
    }

    // Group routes by GroupPrefix
    IEnumerable<IGrouping<string, RouteDefinition>> groups = model.Routes
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

      sb.AppendLine($"    terminal.WriteLine(\"{EscapeString(categoryName)}:\".Cyan().Bold());");

      // Table with command names only (not full patterns)
      sb.AppendLine("    terminal.WriteTable(table => table");
      sb.AppendLine("      .AddColumn(\"Command\")");
      sb.AppendLine("      .AddColumn(\"Description\")");

      foreach (RouteDefinition route in group)
      {
        string commandName = GetCommandName(route);
        string description = route.Description ?? "";
        sb.AppendLine($"      .AddRow(\"{EscapeString(commandName)}\", \"{EscapeString(description)}\")");
      }

      sb.AppendLine("      .HideHeaders()         // ← Remove headers");
      sb.AppendLine("    );");
      firstGroup = false;
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
  private static void EmitOptions(StringBuilder sb)
  {
    sb.AppendLine("    terminal.WriteLine();");
    sb.AppendLine("    terminal.WriteLine(\"Options:\".Cyan().Bold());");
    sb.AppendLine("    terminal.WriteTable(table => table");
    sb.AppendLine("      .AddColumn(\"Option\")");
    sb.AppendLine("      .AddColumn(\"Description\")");
    sb.AppendLine("      .AddRow(\"--help, -h\", \"Show this help message\")");
    sb.AppendLine("      .AddRow(\"--version\", \"Show version information\")");
    sb.AppendLine("      .AddRow(\"--capabilities\", \"Show capabilities for AI tools\")");
    sb.AppendLine("      .HideHeaders()         // ← Remove headers");
    sb.AppendLine("    );");
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
