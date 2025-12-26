// IR builder that mirrors the NuruCoreAppBuilder DSL for semantic interpretation.
//
// This builder is used by the DslInterpreter to "execute" the DSL at design time,
// accumulating state that will be converted to an AppModel.
//
// Key design:
// - CRTP pattern matches NuruCoreAppBuilder<TSelf>
// - Method names mirror DSL methods exactly
// - Build() marks as built but doesn't finalize (for RunAsync intercept sites)
// - FinalizeModel() creates the actual AppModel

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// IR builder that mirrors the NuruCoreAppBuilder DSL for semantic interpretation.
/// Uses CRTP pattern to enable proper fluent chaining.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type for fluent returns.</typeparam>
public class IrAppBuilder<TSelf> where TSelf : IrAppBuilder<TSelf>
{
  // State fields mirroring AppModel properties
  private string? Name;
  private string? Description;
  private readonly List<RouteDefinition> Routes = [];
  private readonly List<InterceptSiteModel> InterceptSites = [];
  private bool IsBuilt;

  /// <summary>
  /// Sets the application name.
  /// Mirrors: NuruCoreAppBuilder.WithName()
  /// </summary>
  public TSelf WithName(string name)
  {
    Name = name;
    return (TSelf)this;
  }

  /// <summary>
  /// Sets the application description.
  /// Mirrors: NuruCoreAppBuilder.WithDescription()
  /// </summary>
  public TSelf WithDescription(string description)
  {
    Description = description;
    return (TSelf)this;
  }

  /// <summary>
  /// Begins mapping a new route.
  /// Mirrors: NuruCoreAppBuilder.Map()
  /// </summary>
  /// <param name="pattern">The route pattern string.</param>
  /// <param name="segments">The parsed segments from PatternStringExtractor.</param>
  /// <returns>An IrRouteBuilder for configuring the route.</returns>
  public IrRouteBuilder<TSelf> Map(string pattern, ImmutableArray<SegmentDefinition> segments)
  {
    return new IrRouteBuilder<TSelf>((TSelf)this, pattern, segments, RegisterRoute);
  }

  /// <summary>
  /// Adds an intercept site from a RunAsync() call.
  /// Called by interpreter when RunAsync() is encountered.
  /// </summary>
  public TSelf AddInterceptSite(InterceptSiteModel site)
  {
    InterceptSites.Add(site);
    return (TSelf)this;
  }

  /// <summary>
  /// Marks the builder as "built".
  /// Mirrors: NuruCoreAppBuilder.Build()
  /// Does not finalize - call FinalizeModel() after all RunAsync() sites are captured.
  /// </summary>
  public TSelf Build()
  {
    IsBuilt = true;
    return (TSelf)this;
  }

  /// <summary>
  /// Finalizes and returns the AppModel.
  /// Called by interpreter after all statements have been processed.
  /// </summary>
  /// <returns>The completed AppModel.</returns>
  /// <exception cref="InvalidOperationException">Thrown if Build() was not called.</exception>
  public AppModel FinalizeModel()
  {
    if (!IsBuilt)
    {
      throw new InvalidOperationException("Build() must be called before FinalizeModel().");
    }

    return new AppModel(
      Name: Name,
      Description: Description,
      AiPrompt: null,
      HasHelp: false,
      HelpOptions: null,
      HasRepl: false,
      ReplOptions: null,
      HasConfiguration: false,
      Routes: [.. Routes],
      Behaviors: [],
      Services: [],
      InterceptSites: [.. InterceptSites]);
  }

  /// <summary>
  /// Callback for IrRouteBuilder to register a completed route.
  /// </summary>
  private void RegisterRoute(RouteDefinition route) => Routes.Add(route);
}

/// <summary>
/// Non-generic convenience type for direct use.
/// </summary>
public sealed class IrAppBuilder : IrAppBuilder<IrAppBuilder> { }
