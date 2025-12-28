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

    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// CONFIGURATION (from AddConfiguration())");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// ═══════════════════════════════════════════════════════════════════════════════");
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
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// Determine environment");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}string __configBasePath = global::System.AppContext.BaseDirectory;");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}string __configEnv = global::System.Environment.GetEnvironmentVariable(\"DOTNET_ENVIRONMENT\")");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}  ?? global::System.Environment.GetEnvironmentVariable(\"ASPNETCORE_ENVIRONMENT\")");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}  ?? \"Production\";");
  }

  /// <summary>
  /// Emits code to extract and sanitize the application name.
  /// </summary>
  private static void EmitApplicationNameExtraction(StringBuilder sb, string ind)
  {
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// Get sanitized application name for app-specific config files");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}string? __configAppName = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}if (!string.IsNullOrEmpty(__configAppName))");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}{{");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}  __configAppName = __configAppName");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}    .Replace(global::System.IO.Path.DirectorySeparatorChar, '_')");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}    .Replace(global::System.IO.Path.AltDirectorySeparatorChar, '_');");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}}}");
  }

  /// <summary>
  /// Emits code to build the configuration from all sources.
  /// </summary>
  private static void EmitConfigurationBuilder(StringBuilder sb, string ind)
  {
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// Build configuration from multiple sources");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}global::Microsoft.Extensions.Configuration.IConfigurationBuilder __configBuilder =");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}  new global::Microsoft.Extensions.Configuration.ConfigurationBuilder()");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}    .SetBasePath(__configBasePath)");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}    .AddJsonFile(\"appsettings.json\", optional: true, reloadOnChange: false)");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}    .AddJsonFile($\"appsettings.{{__configEnv}}.json\", optional: true, reloadOnChange: false);");
    sb.AppendLine();

    // App-specific config files
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// Add application-specific settings files (.NET 10 convention)");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}if (!string.IsNullOrEmpty(__configAppName))");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}{{");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}  __configBuilder");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}    .AddJsonFile($\"{{__configAppName}}.settings.json\", optional: true, reloadOnChange: false)");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}    .AddJsonFile($\"{{__configAppName}}.settings.{{__configEnv}}.json\", optional: true, reloadOnChange: false);");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}}}");
    sb.AppendLine();

    // User secrets (DEBUG only)
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// User secrets (Development/DEBUG only)");
    sb.AppendLine("#if DEBUG");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}if (__configEnv == \"Development\")");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}{{");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}  __configBuilder.AddUserSecrets(global::System.Reflection.Assembly.GetEntryAssembly()!, optional: true, reloadOnChange: false);");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}}}");
    sb.AppendLine("#endif");
    sb.AppendLine();

    // Environment variables and command line
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// Environment variables and command line arguments");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}__configBuilder.AddEnvironmentVariables();");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}__configBuilder.AddCommandLine(args);");
    sb.AppendLine();

    // Build final configuration
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}// Build the configuration");
    sb.AppendLine(CultureInfo.InvariantCulture, $"{ind}global::Microsoft.Extensions.Configuration.IConfigurationRoot configuration = __configBuilder.Build();");
  }
}
