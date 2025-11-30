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

  /// <inheritdoc />
  public override NuruAppBuilder MapDefault(Delegate handler, string? description = null)
  {
    base.MapDefault(handler, description);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder AddReplOptions(Action<ReplOptions>? configureOptions = null)
  {
    base.AddReplOptions(configureOptions);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder Map(string pattern, Delegate handler, string? description = null)
  {
    base.Map(pattern, handler, description);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand>(string pattern, string? description = null)
  {
    base.Map<TCommand>(pattern, description);
    return this;
  }

  /// <inheritdoc />
  public override NuruAppBuilder Map<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TCommand, TResponse>(string pattern, string? description = null)
  {
    base.Map<TCommand, TResponse>(pattern, description);
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
