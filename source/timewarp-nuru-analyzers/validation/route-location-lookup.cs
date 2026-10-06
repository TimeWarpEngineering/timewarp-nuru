namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Resolves the source location of a route's pattern string for diagnostics.
/// </summary>
/// <remarks>
/// Fluent <c>Map("...")</c> locations are keyed by that string literal, which is
/// <see cref="RouteDefinition.OriginalPattern"/>. <see cref="RouteDefinition.EffectivePattern"/>
/// can differ (group prefix, descriptions, <c>{tag?:int}</c> vs <c>{tag:int?}</c>), so fluent
/// lookup tries the source forms first.
/// <c>[NuruRoute]</c> locations are keyed by <see cref="RouteDefinition.EffectivePattern"/>
/// because the attribute is a single literal and property segments are appended. Trying
/// <see cref="RouteDefinition.OriginalPattern"/> first selects a fluent <c>Map</c> of that
/// same literal (for example <c>Map("")</c> beside <c>[NuruRoute("")]</c> with <c>{name?}</c>).
/// </remarks>
internal static class RouteLocationLookup
{
  public static Location? Find(RouteDefinition route, IReadOnlyDictionary<string, Location> routeLocations)
  {
    if (route.Handler.HandlerKind == HandlerKind.Command)
    {
      return routeLocations.TryGetValue(route.EffectivePattern, out Location? endpointLocation)
        ? endpointLocation
        : null;
    }

    if (routeLocations.TryGetValue(route.OriginalPattern, out Location? original))
      return original;

    if (routeLocations.TryGetValue(route.FullPattern, out Location? full))
      return full;

    if (routeLocations.TryGetValue(route.EffectivePattern, out Location? effective))
      return effective;

    return null;
  }

  public static Location FindOrNone(RouteDefinition route, IReadOnlyDictionary<string, Location> routeLocations) =>
    Find(route, routeLocations) ?? Location.None;
}
