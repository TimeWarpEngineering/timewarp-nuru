#region Purpose
// Reports NURU_S010 when a fluent route's type constraint names no conversion the generator knows.
#endregion

#region Design
// A typed {name:T} or --opt {name:T} converts through one of: a built-in parser (TypeConversionMap),
// EnumTypeConverter<T> for an enum handler parameter bound to that segment, or a custom converter
// registered on the app (CustomConverterMatcher: target type, simple name, ConstraintAlias, or the
// "XConverter" convention). A constraint that names none of them used to fall through the emitters
// and produce CS0103 / CS0029 in generated code (task 584). This check reports the route instead.
// Only Delegate and Method routes are checked: the user writes their constraint strings. [NuruRoute]
// and Map<T> endpoints (HandlerKind.Command) derive constraints from property CLR types and bind
// through their own path.
#endregion

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Reports NURU_S010 for a fluent route constraint with no built-in, enum, or custom converter.
/// </summary>
internal static class UnknownTypeConstraintValidator
{
  public static ImmutableArray<Diagnostic> Validate(
    AppModel model,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    List<Diagnostic> diagnostics = [];
    EquatableArray<CustomConverterDefinition> converters = model.CustomConverters;

    foreach (RouteDefinition route in model.Routes)
    {
      if (route.Handler.HandlerKind == HandlerKind.Command)
        continue;

      foreach (ParameterDefinition parameter in route.Parameters)
      {
        if (IsKnown(parameter.TypeConstraint, [parameter.Name], route, converters))
          continue;

        diagnostics.Add(Create(route, routeLocations, $"Parameter '{parameter.Name}'", parameter.TypeConstraint!));
      }

      foreach (OptionDefinition option in route.Options)
      {
        if (option.IsFlag || option.IsJsonValue)
          continue;

        string?[] names = [option.LongForm, option.ShortForm, option.ParameterName];
        if (IsKnown(option.TypeConstraint, names, route, converters))
          continue;

        string display = option.LongFormWithPrefix ?? option.ShortFormWithPrefix!;
        diagnostics.Add(Create(route, routeLocations, $"Option '{display}'", option.TypeConstraint!));
      }
    }

    return [.. diagnostics];
  }

  private static bool IsKnown(
    string? typeConstraint,
    string?[] boundNames,
    RouteDefinition route,
    EquatableArray<CustomConverterDefinition> converters)
  {
    if (string.IsNullOrEmpty(typeConstraint))
      return true;

    string baseType = typeConstraint!.TrimEnd('?');
    if (TypeConversionMap.IsBuiltInType(baseType))
      return true;

    if (CustomConverterMatcher.Find(baseType, converters) is not null)
      return true;

    // EnumTypeConverter<T>: the emitters bind an enum handler parameter by its segment or option name.
    return route.Handler.Parameters.Any(p =>
      p.IsEnumType &&
      boundNames.Any(name => string.Equals(p.SourceName, name, StringComparison.OrdinalIgnoreCase)));
  }

  private static Diagnostic Create(
    RouteDefinition route,
    IReadOnlyDictionary<string, Location> routeLocations,
    string subject,
    string typeConstraint) =>
    Diagnostic.Create(
      DiagnosticDescriptors.UnknownTypeConstraint,
      RouteLocationLookup.FindOrNone(route, routeLocations),
      subject,
      typeConstraint.TrimEnd('?'),
      route.FullPattern);
}
