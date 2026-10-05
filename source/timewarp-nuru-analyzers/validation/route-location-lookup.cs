namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Resolves the source location of a route's pattern string for diagnostics.
/// </summary>
/// <remarks>
/// Location keys are the literal pattern strings from source (<c>Map("...")</c> or the
/// <c>[NuruRoute]</c> attribute). <see cref="RouteDefinition.EffectivePattern"/> is rebuilt
/// from segments, so it can differ from the source literal (descriptions, <c>{tag?:int}</c>
/// vs <c>{tag:int?}</c>). Try the source forms first, then the effective pattern.
/// </remarks>
internal static class RouteLocationLookup
{
  public static Location? Find(RouteDefinition route, IReadOnlyDictionary<string, Location> routeLocations)
  {
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
