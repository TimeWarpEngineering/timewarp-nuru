namespace TimeWarp.Nuru;

/// <summary>
/// Extension methods for NuruAppBuilder to enable shell completion support.
/// </summary>
public static class NuruAppBuilderCompletionExtensions
{
  // ============================================================================
  // EndpointBuilder<TBuilder> overloads - preserve builder type in fluent chain
  // ============================================================================

  /// <summary>
  /// Enables shell completion (generic EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <typeparam name="TBuilder">The builder type for proper fluent chaining.</typeparam>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <param name="appName">Optional application name for generated scripts.</param>
  /// <param name="configure">Optional action to configure completion sources.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static TBuilder EnableCompletion<TBuilder>(
    this EndpointBuilder<TBuilder> configurator,
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.EnableCompletion(appName, configure);
  }

  // ============================================================================
  // EndpointBuilder overloads (non-generic) - backward compatibility
  // ============================================================================

  /// <summary>
  /// Enables shell completion (EndpointBuilder overload for fluent chaining).
  /// </summary>
  /// <param name="configurator">The EndpointBuilder from a Map() call.</param>
  /// <param name="appName">Optional application name for generated scripts.</param>
  /// <param name="configure">Optional action to configure completion sources.</param>
  /// <returns>The underlying builder for chaining.</returns>
  public static NuruCoreAppBuilder EnableCompletion(
    this EndpointBuilder configurator,
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
  {
    ArgumentNullException.ThrowIfNull(configurator);
    return configurator.Builder.EnableCompletion(appName, configure);
  }

  // ============================================================================
  // NuruCoreAppBuilder extension methods
  // ============================================================================

  /// <summary>
  /// Enables shell completion that queries the application at Tab-press time.
  /// Registers the following routes:
  /// <list type="bullet">
  ///   <item><c>__complete {index:int} {*words}</c> - Callback route for shell scripts</item>
  ///   <item><c>--generate-completion {shell}</c> - Generates shell completion script</item>
  ///   <item><c>--install-completion {shell?}</c> - Installs completion to shell config</item>
  ///   <item><c>--install-completion --dry-run {shell?}</c> - Preview installation</item>
  /// </list>
  /// </summary>
  /// <param name="builder">The NuruAppBuilder instance.</param>
  /// <param name="appName">
  /// Optional application name to use in generated scripts.
  /// If not provided, automatically detects the actual executable name at runtime.
  /// </param>
  /// <param name="configure">
  /// Optional action to configure completion sources.
  /// Register completion sources for specific parameters or types to provide
  /// dynamic completions from databases, APIs, or other runtime sources.
  /// </param>
  /// <returns>The builder for fluent chaining.</returns>
  /// <example>
  /// Basic usage:
  /// <code>
  /// NuruApp.CreateBuilder(args)
  ///   .Map("deploy {env}").WithHandler((string env) => { }).Done()
  ///   .EnableCompletion()
  ///   .Build()
  ///   .RunAsync(args);
  /// </code>
  /// 
  /// With custom completion sources:
  /// <code>
  /// .EnableCompletion(configure: registry =>
  /// {
  ///   registry.RegisterForParameter("env", new MyEnvironmentCompletionSource());
  ///   registry.RegisterForType(typeof(DeployMode), new EnumCompletionSource&lt;DeployMode&gt;());
  /// })
  /// </code>
  /// </example>
  public static TBuilder EnableCompletion<TBuilder>(
    this TBuilder builder,
    string? appName = null,
    Action<CompletionSourceRegistry>? configure = null)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);

    // Store the configuration callback for runtime invocation.
    // The source generator emits code to call ConfigureCompletionRegistry() which
    // invokes this callback to populate app.CompletionSourceRegistry.
    if (configure is not null)
    {
      builder.CompletionRegistryConfiguration = configure;
    }

    _ = appName; // Will be used by source generator for script generation

    return builder;
  }
}
