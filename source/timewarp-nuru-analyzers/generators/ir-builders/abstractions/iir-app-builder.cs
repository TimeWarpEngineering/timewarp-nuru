// Marker interface for app-level IR builder with finalization capabilities.
//
// Only implemented by IrAppBuilder. Provides access to app-level operations
// that don't exist on group builders (Build, FinalizeModel, AddInterceptSite).

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// App-level builder with finalization capabilities.
/// Only implemented by IrAppBuilder.
/// </summary>
public interface IIrAppBuilder : IIrRouteSource
{
  /// <summary>
  /// Sets the variable name for debugging/identification.
  /// </summary>
  /// <param name="variableName">The variable name from the source code.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder SetVariableName(string variableName);

  /// <summary>
  /// Marks the builder as built.
  /// Must be called before FinalizeModel().
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder Build();

  /// <summary>
  /// Sets the application name.
  /// </summary>
  /// <param name="name">The application name.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder WithName(string name);

  /// <summary>
  /// Sets the application description.
  /// </summary>
  /// <param name="description">The application description.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder WithDescription(string description);

  /// <summary>
  /// Adds an intercept site from a RunAsync() call.
  /// </summary>
  /// <param name="site">The intercept site model.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddInterceptSite(InterceptSiteModel site);

  /// <summary>
  /// Finalizes and returns the AppModel.
  /// Must be called after Build().
  /// </summary>
  /// <returns>The completed AppModel.</returns>
  AppModel FinalizeModel();
}
