namespace TimeWarp.Nuru.Completion;

using System;
using System.Diagnostics;
using System.IO;
using TimeWarp.Nuru;

/// <summary>
/// Extension methods for NuruAppBuilder to enable shell completion support.
/// </summary>
public static class NuruAppBuilderExtensions
{
  /// <summary>
  /// Enables shell completion by automatically registering the `--generate-completion {shell}` route.
  /// </summary>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="appName">
  /// Optional application name to use in generated scripts.
  /// If not provided, automatically detects the actual executable name at runtime.
  /// This ensures completion scripts match the published executable name.
  /// </param>
  /// <returns>The builder for fluent chaining.</returns>
  public static NuruAppBuilder EnableShellCompletion(
    this NuruAppBuilder builder,
    string? appName = null)
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Auto-detect app name at generation time (not at build time)
    // This is deferred until the --generate-completion route is actually called
    string GetEffectiveAppName()
    {
      if (appName is not null)
        return appName;

      // Try to get the actual process name (works for published executables)
      string? processPath = Environment.ProcessPath;
      if (processPath is not null)
      {
        string fileName = Path.GetFileNameWithoutExtension(processPath);
        if (!string.IsNullOrEmpty(fileName))
          return fileName;
      }

      // Fallback: try Process.GetCurrentProcess()
      using var currentProcess = Process.GetCurrentProcess();
      if (!string.IsNullOrEmpty(currentProcess.ProcessName))
        return currentProcess.ProcessName;

      // Final fallback
      return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "myapp";
    }

    // Register the --generate-completion route
    builder.AddRoute("--generate-completion {shell}", (string shell) =>
    {
      // Detect app name at runtime (when the command is actually executed)
      string detectedAppName = GetEffectiveAppName();

      var generator = new CompletionScriptGenerator();

      string script = shell.ToLowerInvariant() switch
      {
        "bash" => generator.GenerateBash(builder.EndpointCollection, detectedAppName),
        "zsh" => generator.GenerateZsh(builder.EndpointCollection, detectedAppName),
        "pwsh" or "powershell" => generator.GeneratePowerShell(builder.EndpointCollection, detectedAppName),
        "fish" => generator.GenerateFish(builder.EndpointCollection, detectedAppName),
        _ => throw new ArgumentException(
          $"Unknown shell: {shell}. Supported shells: bash, zsh, pwsh, fish",
          nameof(shell))
      };

      Console.WriteLine(script);
    });

    return builder;
  }
}
