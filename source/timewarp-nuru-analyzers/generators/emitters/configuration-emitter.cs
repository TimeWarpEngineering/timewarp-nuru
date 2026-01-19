// Emits configuration loading code for AddConfiguration() DSL method.
// Generates code that builds IConfigurationRoot at runtime startup.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to build configuration from various sources.
/// Creates the configuration loading code for AddConfiguration() DSL method.
/// </summary>
internal static class ConfigurationEmitter
{
  /// <summary>
  /// Emits configuration building code at the start of RunAsync_Intercepted.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="indent">Number of spaces for indentation.</param>
  public static void Emit(StringBuilder sb, int indent = 4)
  {
    string ind = new(' ', indent);

    sb.AppendLine($"{ind}// ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine($"{ind}// CONFIGURATION (from AddConfiguration())");
    sb.AppendLine($"{ind}// ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine();

    EmitEnvironmentDetection(sb, ind);
    sb.AppendLine();

    EmitApplicationNameExtraction(sb, ind);
    sb.AppendLine();

    EmitConfigurationBuilder(sb, ind);
    sb.AppendLine();
  }

  /// <summary>
  /// Emits code to detect the current environment.
  /// </summary>
  private static void EmitEnvironmentDetection(StringBuilder sb, string ind)
  {
    sb.AppendLine($"{ind}// Determine environment");
    sb.AppendLine($"{ind}string __configBasePath = global::System.AppContext.BaseDirectory;");
    sb.AppendLine($"{ind}string __configEnv = global::System.Environment.GetEnvironmentVariable(\"DOTNET_ENVIRONMENT\")");
    sb.AppendLine($"{ind}  ?? global::System.Environment.GetEnvironmentVariable(\"ASPNETCORE_ENVIRONMENT\")");
    sb.AppendLine($"{ind}  ?? \"Production\";");
  }

  /// <summary>
  /// Emits code to extract and sanitize the application name.
  /// </summary>
  private static void EmitApplicationNameExtraction(StringBuilder sb, string ind)
  {
    sb.AppendLine($"{ind}// Get sanitized application name for app-specific config files");
    sb.AppendLine($"{ind}string? __configAppName = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;");
    sb.AppendLine($"{ind}if (!string.IsNullOrEmpty(__configAppName))");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __configAppName = __configAppName");
    sb.AppendLine($"{ind}    .Replace(global::System.IO.Path.DirectorySeparatorChar, '_')");
    sb.AppendLine($"{ind}    .Replace(global::System.IO.Path.AltDirectorySeparatorChar, '_');");
    sb.AppendLine($"{ind}}}");
  }

  /// <summary>
  /// Emits code to build the configuration from all sources.
  /// </summary>
  private static void EmitConfigurationBuilder(StringBuilder sb, string ind)
  {
    sb.AppendLine($"{ind}// Build configuration from multiple sources");
    sb.AppendLine($"{ind}global::Microsoft.Extensions.Configuration.IConfigurationBuilder __configBuilder =");
    sb.AppendLine($"{ind}  new global::Microsoft.Extensions.Configuration.ConfigurationBuilder()");
    sb.AppendLine($"{ind}    .SetBasePath(__configBasePath)");
    sb.AppendLine($"{ind}    .AddJsonFile(\"appsettings.json\", optional: true, reloadOnChange: false)");
    sb.AppendLine($"{ind}    .AddJsonFile($\"appsettings.{{__configEnv}}.json\", optional: true, reloadOnChange: false);");
    sb.AppendLine();

    // App-specific config files
    sb.AppendLine($"{ind}// Add application-specific settings files (.NET 10 convention)");
    sb.AppendLine($"{ind}if (!string.IsNullOrEmpty(__configAppName))");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __configBuilder");
    sb.AppendLine($"{ind}    .AddJsonFile($\"{{__configAppName}}.settings.json\", optional: true, reloadOnChange: false)");
    sb.AppendLine($"{ind}    .AddJsonFile($\"{{__configAppName}}.settings.{{__configEnv}}.json\", optional: true, reloadOnChange: false);");
    sb.AppendLine($"{ind}}}");
    sb.AppendLine();

    // User secrets (DEBUG only)
    sb.AppendLine($"{ind}// User secrets (Development/DEBUG only)");
    sb.AppendLine("#if DEBUG");
    sb.AppendLine($"{ind}if (__configEnv == \"Development\")");
    sb.AppendLine($"{ind}{{");
    sb.AppendLine($"{ind}  __configBuilder.AddUserSecrets(global::System.Reflection.Assembly.GetEntryAssembly()!, optional: true, reloadOnChange: false);");
    sb.AppendLine($"{ind}}}");
    sb.AppendLine("#endif");
    sb.AppendLine();

    // Environment variables and command line
    sb.AppendLine($"{ind}// Environment variables and command line arguments");
    sb.AppendLine($"{ind}__configBuilder.AddEnvironmentVariables();");
    sb.AppendLine($"{ind}__configBuilder.AddCommandLine(args);");
    sb.AppendLine();

    // Build final configuration
    sb.AppendLine($"{ind}// Build the configuration");
    sb.AppendLine($"{ind}global::Microsoft.Extensions.Configuration.IConfigurationRoot configuration = __configBuilder.Build();");
  }
}
