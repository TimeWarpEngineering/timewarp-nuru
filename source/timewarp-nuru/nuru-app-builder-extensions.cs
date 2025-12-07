namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods that auto-wire all Nuru extensions to an existing builder.
/// </summary>
public static class NuruAppBuilderExtensions
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

    return builder
      .UseTelemetry(options.ConfigureTelemetry ?? (_ => { }))
      .AddReplSupport(options.ConfigureRepl)
      .EnableDynamicCompletion(configure: options.ConfigureCompletion)
      .AddInteractiveRoute(options.InteractiveRoutePatterns);
  }
}
