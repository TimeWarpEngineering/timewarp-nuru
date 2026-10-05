namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Reports NURU_R005 when a user option's long form is <c>json-args</c>.
/// </summary>
internal static class ReservedJsonArgsValidator
{
  public static ImmutableArray<Diagnostic> Validate(
    AppModel model,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    List<Diagnostic> diagnostics = [];

    foreach (RouteDefinition route in model.Routes)
    {
      foreach (OptionDefinition option in route.Options)
      {
        if (!string.Equals(option.LongForm, "json-args", StringComparison.Ordinal))
          continue;

        diagnostics.Add(Diagnostic.Create(
          DiagnosticDescriptors.ReservedJsonArgsOption,
          RouteLocationLookup.FindOrNone(route, routeLocations),
          route.FullPattern));
      }
    }

    return [.. diagnostics];
  }
}
