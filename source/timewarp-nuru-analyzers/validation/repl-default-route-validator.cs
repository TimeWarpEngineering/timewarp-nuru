// Detects NURU_R004: REPL AutoStartWhenEmpty makes a top-level default route unreachable.
//
// EmitInteractiveFlag checks routeArgs.Length == 0 and starts the REPL before user routes
// are matched. A top-level "" route is the route that would have handled that empty list.
// A "" route inside a group still requires its prefix, so it does not conflict.

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Reports NURU_R004 when an app enables REPL <c>AutoStartWhenEmpty</c> and also
/// registers a top-level default route.
/// </summary>
internal static class ReplDefaultRouteValidator
{
  /// <summary>
  /// Validates that AutoStartWhenEmpty is not combined with a top-level default route.
  /// </summary>
  /// <param name="model">The app model, including combined fluent and endpoint routes.</param>
  /// <param name="routeLocations">Map from effective route pattern to source location.</param>
  /// <returns>Diagnostics for each conflicting top-level default route.</returns>
  public static ImmutableArray<Diagnostic> Validate(
    AppModel model,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(routeLocations);

    if (!model.HasRepl || model.ReplOptions is not { AutoStartWhenEmpty: true })
      return [];

    List<Diagnostic> diagnostics = [];

    foreach (RouteDefinition route in model.Routes)
    {
      if (!IsTopLevelDefaultRoute(route))
        continue;

      Location location = routeLocations.TryGetValue(route.EffectivePattern, out Location? loc)
        ? loc
        : Location.None;

      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.ReplAutoStartConflictsWithDefaultRoute,
        location));
    }

    return [.. diagnostics];
  }

  /// <summary>
  /// A top-level default route is pattern <c>""</c> with no group prefix.
  /// <c>""</c> inside a group keeps <see cref="RouteDefinition.GroupPrefix"/> and does not conflict.
  /// </summary>
  private static bool IsTopLevelDefaultRoute(RouteDefinition route) =>
    string.IsNullOrEmpty(route.GroupPrefix) && string.IsNullOrEmpty(route.OriginalPattern);
}
