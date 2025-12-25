namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods that auto-wire all Nuru extensions to an existing builder.
/// </summary>
/// <remarks>
/// This partial class is split across multiple files:
/// <list type="bullet">
/// <item><description><c>nuru-app-builder-extensions.cs</c> - Core extension wiring (<see cref="UseAllExtensions"/>)</description></item>
/// <item><description><c>nuru-app-builder-extensions.version.cs</c> - Version route handler</description></item>
/// <item><description><c>nuru-app-builder-extensions.updates.cs</c> - Update checking and GitHub API integration</description></item>
/// </list>
/// Related utility classes:
/// <list type="bullet">
/// <item><description><see cref="SemVerComparer"/> - Semantic version comparison utility</description></item>
/// </list>
/// </remarks>
public static partial class NuruAppBuilderExtensions
{
  /// <summary>
  /// Adds all Nuru extensions to the builder: Telemetry, Logging, REPL, Shell Completion, and Interactive routes.
  /// This is called automatically by <see cref="NuruApp.CreateBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder to extend.</param>
  /// <param name="options">Optional configuration for extensions. If null, uses defaults.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// This method adds:
  /// <list type="bullet">
  /// <item>OpenTelemetry integration via <c>UseTelemetry()</c></item>
  /// <item>REPL mode support via <c>AddReplSupport()</c></item>
  /// <item>Dynamic shell completion via <c>EnableDynamicCompletion()</c> (routes: <c>--generate-completion</c>, <c>__complete</c>, <c>--install-completion</c>)</item>
  /// <item>Interactive route (<c>--interactive</c>, <c>-i</c>) via <c>AddInteractiveRoute()</c></item>
  /// </list>
  /// Logging is already configured by <c>UseTelemetry()</c>.
  /// </remarks>
  public static NuruAppBuilder UseAllExtensions(this NuruAppBuilder builder, NuruAppOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(builder);
    options ??= new NuruAppOptions();

    // Configure help options if provided
    if (options.ConfigureHelp is not null)
    {
      builder.ConfigureHelp(options.ConfigureHelp);
    }

    // Add telemetry unless disabled
    if (!options.DisableTelemetry)
    {
      builder.UseTelemetry(options.ConfigureTelemetry ?? (_ => { }));
    }

    // Add REPL support unless disabled
    if (!options.DisableRepl)
    {
      // TODO Update REPL package to support V2 source generator
      // builder.AddReplSupport(options.ConfigureRepl);
    }

    // Add dynamic shell completion unless disabled
    if (!options.DisableCompletion)
    {
      builder.EnableDynamicCompletion(configure: options.ConfigureCompletion);
    }

    // Add interactive route unless disabled
    if (!options.DisableInteractiveRoute)
    {
      // TODO Update REPL package to support V2 source generator
      // builder.AddInteractiveRoute(options.InteractiveRoutePatterns);
    }

    // Add version route unless disabled
    if (!options.DisableVersionRoute)
    {
      builder.AddVersionRoute();
    }

    // Add check-updates route unless disabled
    if (!options.DisableCheckUpdatesRoute)
    {
      builder.AddCheckUpdatesRoute();
    }

    // Add capabilities route unless disabled
    if (!options.DisableCapabilitiesRoute)
    {
      // builder.AddCapabilitiesRoute();
    }

    return builder;
  }
}
