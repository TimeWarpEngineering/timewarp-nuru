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
  /// Sets the AI prompt for --capabilities output.
  /// </summary>
  /// <param name="aiPrompt">The AI prompt text.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder WithAiPrompt(string aiPrompt);

  /// <summary>
  /// Enables help with default options.
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddHelp();

  /// <summary>
  /// Enables help with custom options.
  /// </summary>
  /// <param name="helpOptions">The configured help options.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddHelp(HelpModel helpOptions);

  /// <summary>
  /// Enables REPL with default options.
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddRepl();

  /// <summary>
  /// Enables REPL with custom options.
  /// </summary>
  /// <param name="replOptions">The configured REPL options.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddRepl(ReplModel replOptions);

  /// <summary>
  /// Enables configuration.
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddConfiguration();

  /// <summary>
  /// Enables the --check-updates route for GitHub version checking.
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddCheckUpdatesRoute();

  /// <summary>
  /// Adds a behavior (pipeline middleware).
  /// </summary>
  /// <param name="behavior">The behavior definition.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddBehavior(BehaviorDefinition behavior);

  /// <summary>
  /// Adds a service registration.
  /// </summary>
  /// <param name="service">The service definition.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddService(ServiceDefinition service);

  /// <summary>
  /// No-op for UseTerminal (runtime only).
  /// </summary>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder UseTerminal();

  /// <summary>
  /// Registers a custom type converter for code generation.
  /// </summary>
  /// <param name="converter">The converter definition.</param>
  /// <returns>This builder for chaining.</returns>
  IIrAppBuilder AddTypeConverter(CustomConverterDefinition converter);

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
