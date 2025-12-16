namespace TimeWarp.Nuru;

using System;
using System.Diagnostics;
using System.IO;
using TimeWarp.Nuru;

/// <summary>
/// Extension methods for NuruAppBuilder to enable shell completion support.
/// </summary>
public static class NuruAppBuilderExtensions
{
  // ============================================================================
  // RouteConfigurator<TBuilder> overloads - preserve builder type in fluent chain
  // ============================================================================

  /// <summary>
  /// Enables static shell completion (generic RouteConfigurator overload for fluent chaining).
  /// </summary>
  /// <typeparam name="TBuilder">The builder type for proper fluent chaining.</typeparam>
  /// <param name="configurator">The RouteConfigurator from a Map() call.</param>
  /// <param name="appName">Optional application name for generated scripts.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static TBuilder EnableStaticCompletion<TBuilder>(
    this RouteConfigurator<TBuilder> configurator,
    string? appName = null)
    where TBuilder : NuruCoreAppBuilder
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.EnableStaticCompletion(appName);
  }

  /// <summary>
  /// Enables dynamic shell completion (generic RouteConfigurator overload for fluent chaining).
  /// </summary>
  /// <typeparam name="TBuilder">The builder type for proper fluent chaining.</typeparam>
  /// <param name="configurator">The RouteConfigurator from a Map() call.</param>
  /// <param name="appName">Optional application name for generated scripts.</param>
  /// <param name="configure">Optional action to configure completion sources.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static TBuilder EnableDynamicCompletion<TBuilder>(
    this RouteConfigurator<TBuilder> configurator,
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
    where TBuilder : NuruCoreAppBuilder
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.EnableDynamicCompletion(appName, configure);
  }

  // ============================================================================
  // RouteConfigurator overloads (non-generic) - backward compatibility
  // ============================================================================

  /// <summary>
  /// Enables static shell completion (RouteConfigurator overload for fluent chaining).
  /// </summary>
  /// <param name="configurator">The RouteConfigurator from a Map() call.</param>
  /// <param name="appName">Optional application name for generated scripts.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static NuruCoreAppBuilder EnableStaticCompletion(
    this RouteConfigurator configurator,
    string? appName = null)
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.EnableStaticCompletion(appName);
  }

  /// <summary>
  /// Enables dynamic shell completion (RouteConfigurator overload for fluent chaining).
  /// </summary>
  /// <param name="configurator">The RouteConfigurator from a Map() call.</param>
  /// <param name="appName">Optional application name for generated scripts.</param>
  /// <param name="configure">Optional action to configure completion sources.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static NuruCoreAppBuilder EnableDynamicCompletion(
    this RouteConfigurator configurator,
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.EnableDynamicCompletion(appName, configure);
  }

  // ============================================================================
  // NuruCoreAppBuilder extension methods
  // ============================================================================

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
  public static TBuilder EnableStaticCompletion<TBuilder>(
    this TBuilder builder,
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "Parameter kept for backward compatibility")]
    string? appName = null)
    where TBuilder : NuruCoreAppBuilder
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Auto-detect app name at generation time (not at build time)

    // Register the --generate-completion route
    builder.Map("--generate-completion {shell}", (string shell) =>
    {
      // Detect app name at runtime (when the command is actually executed)
      string detectedAppName = AppNameDetector.GetEffectiveAppName();

      CompletionScriptGenerator generator = new();

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
  public static TBuilder EnableDynamicCompletion<TBuilder>(
    this TBuilder builder,
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter", Justification = "Parameter kept for backward compatibility")]
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
    where TBuilder : NuruCoreAppBuilder
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Create and configure the registry
    CompletionSourceRegistry registry = new();
    configure?.Invoke(registry);

    // Register the __complete callback route
    builder.Map("__complete {index:int} {*words}", (int index, string[] words) =>
    {
      return DynamicCompletionHandler.HandleCompletion(index, words, registry, builder.EndpointCollection);
    });

    // Register the --generate-completion route with dynamic templates
    builder.Map("--generate-completion {shell}", (string shell) =>
    {
      string detectedAppName = AppNameDetector.GetEffectiveAppName();

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
    builder.Map("--install-completion {shell?}", (string? shell) =>
    {
      string detectedAppName = AppNameDetector.GetEffectiveAppName();
      InstallCompletionHandler.Install(detectedAppName, shell);
    });

    // Register the --install-completion --dry-run route for preview
    builder.Map("--install-completion --dry-run {shell?}", (string? shell) =>
    {
      string detectedAppName = AppNameDetector.GetEffectiveAppName();
      InstallCompletionHandler.Install(detectedAppName, shell, dryRun: true);
    });

    return builder;
  }
}

