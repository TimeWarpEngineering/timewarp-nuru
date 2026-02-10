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
  /// <param name="methodSuffix">Suffix for method name (e.g., "_0" for multi-app assemblies).</param>
  public static void Emit(StringBuilder sb, AppModel model, string methodSuffix = "")
  {
    sb.AppendLine($"  private static void PrintHelp{methodSuffix}(ITerminal terminal)");
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
      sb.AppendLine(
        $"    terminal.WriteLine(\"{EscapeString(model.Name)}\");");
    }

    if (model.Description is not null)
    {
      sb.AppendLine(
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
    sb.AppendLine(
      $"    terminal.WriteLine(\"Usage: {EscapeString(appName)} [command] [options]\");");
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

    sb.AppendLine("    terminal.WriteLine(\"Commands:\");");
    sb.AppendLine("    terminal.WriteTable(table => table");
    sb.AppendLine("      .AddColumn(\"Command\")");
    sb.AppendLine("      .AddColumn(\"Description\")");

    foreach (RouteDefinition route in model.Routes)
    {
      string pattern = HelpPatternHelper.BuildPatternDisplay(route);
      string description = route.Description ?? "";
      sb.AppendLine($"      .AddRow(\"{EscapeString(pattern)}\", \"{EscapeString(description)}\")");
    }

    sb.AppendLine("    );");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the global options section as a table.
  /// </summary>
  private static void EmitOptions(StringBuilder sb)
  {
    sb.AppendLine("    terminal.WriteLine(\"Options:\");");
    sb.AppendLine("    terminal.WriteTable(table => table");
    sb.AppendLine("      .AddColumn(\"Option\")");
    sb.AppendLine("      .AddColumn(\"Description\")");
    sb.AppendLine("      .AddRow(\"--help, -h\", \"Show this help message\")");
    sb.AppendLine("      .AddRow(\"--version\", \"Show version information\")");
    sb.AppendLine("      .AddRow(\"--capabilities\", \"Show capabilities for AI tools\")");
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
