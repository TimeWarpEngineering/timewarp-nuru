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
  /// Enables static shell completion by automatically registering the `--generate-completion {shell}` route.
  /// Static completion generates pre-computed scripts based on registered routes.
  /// </summary>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="appName">
  /// Optional application name to use in generated scripts.
  /// If not provided, automatically detects the actual executable name at runtime.
  /// This ensures completion scripts match the published executable name.
  /// </param>
  /// <returns>The builder for fluent chaining.</returns>
  public static NuruAppBuilder EnableStaticCompletion(
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

  /// <summary>
  /// Enables dynamic shell completion that queries the application at Tab-press time.
  /// This is mutually exclusive with EnableStaticCompletion() - use one or the other.
  /// </summary>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="appName">
  /// Optional application name to use in generated scripts.
  /// If not provided, automatically detects the actual executable name at runtime.
  /// </param>
  /// <param name="configure">
  /// Optional action to configure completion sources.
  /// Register completion sources for specific parameters or types.
  /// </param>
  /// <returns>The builder for fluent chaining.</returns>
  public static NuruAppBuilder EnableDynamicCompletion(
    this NuruAppBuilder builder,
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Create and configure the registry
    var registry = new CompletionSourceRegistry();
    configure?.Invoke(registry);

    // Register the __complete callback route
    builder.AddRoute("__complete {index:int} {*words}", (int index, string[] words) =>
    {
      return DynamicCompletionHandler.HandleCompletion(index, words, registry, builder.EndpointCollection);
    });

    // Auto-detect app name helper (same as EnableStaticCompletion)
    string GetEffectiveAppName()
    {
      if (appName is not null)
        return appName;

      string? processPath = Environment.ProcessPath;
      if (processPath is not null)
      {
        string fileName = Path.GetFileNameWithoutExtension(processPath);
        if (!string.IsNullOrEmpty(fileName))
          return fileName;
      }

      using var currentProcess = Process.GetCurrentProcess();
      if (!string.IsNullOrEmpty(currentProcess.ProcessName))
        return currentProcess.ProcessName;

      return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "myapp";
    }

    // Register the --generate-completion route with dynamic templates
    builder.AddRoute("--generate-completion {shell}", (string shell) =>
    {
      string detectedAppName = GetEffectiveAppName();

      string script = shell.ToLowerInvariant() switch
      {
        "bash" => DynamicCompletionScriptGenerator.GenerateBash(detectedAppName),
        "zsh" => DynamicCompletionScriptGenerator.GenerateZsh(detectedAppName),
        "pwsh" or "powershell" => DynamicCompletionScriptGenerator.GeneratePowerShell(detectedAppName),
        "fish" => DynamicCompletionScriptGenerator.GenerateFish(detectedAppName),
        _ => throw new ArgumentException(
          $"Unknown shell: {shell}. Supported shells: bash, zsh, pwsh, fish",
          nameof(shell))
      };

      Console.WriteLine(script);
    });

    // Register the --install-completion route for automatic installation
    builder.AddRoute("--install-completion {shell?}", (string? shell) =>
    {
      string detectedAppName = GetEffectiveAppName();
      InstallCompletionHandler.Install(detectedAppName, shell);
    });

    // Register the --install-completion --dry-run route for preview
    builder.AddRoute("--install-completion --dry-run {shell?}", (string? shell) =>
    {
      string detectedAppName = GetEffectiveAppName();
      InstallCompletionHandler.Install(detectedAppName, shell, dryRun: true);
    });

    return builder;
  }
}

