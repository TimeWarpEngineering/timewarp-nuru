namespace TimeWarp.Nuru;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Covariant return type overrides for fluent API support.
/// These overrides ensure that methods return NuruAppBuilder instead of NuruCoreAppBuilder
/// when called on a NuruAppBuilder instance, enabling proper fluent chaining.
/// </summary>
public partial class NuruAppBuilder
{
  #region Configuration Overrides

  /// <inheritdoc />
  public override NuruAppBuilder AddConfiguration(string[]? args = null)
  {
    base.AddConfiguration(args);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder AddDependencyInjection()
  {
    base.AddDependencyInjection();
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder ConfigureServices(Action<IServiceCollection> configure)
  {
    base.ConfigureServices(configure);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder ConfigureServices(Action<IServiceCollection, IConfiguration?> configure)
  {
    base.ConfigureServices(configure);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder UseLogging(ILoggerFactory loggerFactory)
  {
    base.UseLogging(loggerFactory);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder UseTerminal(ITerminal terminal)
  {
    base.UseTerminal(terminal);
    return this;
  }

  #endregion

  #region Route Overrides

  // Note: Map, MapDefault, and Map<T> now return EndpointBuilder from the base class.
  // EndpointBuilder provides implicit conversion back to the builder type and
  // supports fluent chaining via its own Map/MapMultiple methods.
  // These overrides are no longer needed since EndpointBuilder handles the chaining.

  /// <inheritdoc />
  public override NuruAppBuilder AddReplOptions(Action<ReplOptions>? configureOptions = null)
  {
    base.AddReplOptions(configureOptions);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder MapMultiple(string[] patterns, Delegate handler, string? description = null)
  {
    base.MapMultiple(patterns, handler, description);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder MapMultiple<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string[] patterns, string? description = null)
  {
    base.MapMultiple<TCommand>(patterns, description);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder MapMultiple<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string[] patterns, string? description = null)
  {
    base.MapMultiple<TCommand, TResponse>(patterns, description);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder AddTypeConverter(IRouteTypeConverter converter)
  {
    base.AddTypeConverter(converter);
    return this;
  }

  #endregion
}
