// Emits version output generation code.
// Generates the PrintVersion method for --version flag handling.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to display version information.
/// Creates the PrintVersion method that displays the application version.
/// </summary>
internal static class VersionEmitter
{
  /// <summary>
  /// Emits the PrintVersion method for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="model">The application model containing metadata.</param>
  public static void Emit(StringBuilder sb, AppModel model)
  {
    sb.AppendLine("  private static void PrintVersion(ITerminal terminal)");
    sb.AppendLine("  {");

    // Emit app name if available
    if (model.Name is not null)
    {
      sb.AppendLine(
        $"    terminal.Write(\"{EmitterStringUtils.EscapeForStringLiteral(model.Name)} \");");
    }

    // Version is read from assembly at runtime
    // We emit code that reads the assembly version
    sb.AppendLine("    string? version = typeof(GeneratedInterceptor).Assembly");
    sb.AppendLine("      .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()");
    sb.AppendLine("      ?.InformationalVersion");
    sb.AppendLine("      ?? typeof(GeneratedInterceptor).Assembly.GetName().Version?.ToString()");
    sb.AppendLine("      ?? \"1.0.0\";");
    sb.AppendLine("    terminal.WriteLine(version);");

    sb.AppendLine("  }");
  }

}
