#region Purpose
// Reports NURU_S010 when a fluent route's type constraint names no conversion the generator knows,
// and tells the emitters which values to declare without a conversion.
#endregion

#region Design
// A typed {name:T} or --opt {name:T} converts through one of: a built-in parser (TypeConversionMap),
// EnumTypeConverter<T> for an enum handler parameter bound to that segment, or a custom converter
// registered on the app (CustomConverterMatcher: target type, simple name, ConstraintAlias, or the
// "XConverter" convention). A constraint that names none of them used to fall through the emitters
// and produce CS0103 / CS0029 in generated code (task 584). This check reports the route instead.
// List values convert less: a typed catch-all {*name:T} has built-in element parsers only, and a
// repeated option adds EnumTypeConverter<T> but has no custom converter path.
// Only Delegate and Method routes are checked: the user writes their constraint strings. [NuruRoute]
// and Map<T> endpoints (HandlerKind.Command) derive constraints from property CLR types and bind
// through their own path.
// IsUnresolved is the same rule for one value. The route matcher and the --json-args binder call it
// and declare the value as default (the handler's type) instead of a string, so the generated code
// still compiles and the only error the build reports is NURU_S010 (review M1, task 584).
#endregion

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Reports NURU_S010 for a fluent route constraint with no built-in, enum, or custom converter.
/// </summary>
internal static class UnknownTypeConstraintValidator
{
  private const string ScalarReason =
    "is not a built-in type, an enum handler parameter, or a converter registered with AddTypeConverter() (by target type name or ConstraintAlias)";

  private const string CatchAllReason =
    "is not a built-in type; a typed catch-all converts built-in element types only";

  private const string RepeatedOptionReason =
    "is not a built-in type or an enum handler parameter; a repeated option does not convert through a custom converter";

  public static ImmutableArray<Diagnostic> Validate(
    AppModel model,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    List<Diagnostic> diagnostics = [];
    EquatableArray<CustomConverterDefinition> converters = model.CustomConverters;

    foreach (RouteDefinition route in model.Routes)
    {
      foreach (ParameterDefinition parameter in route.Parameters)
      {
        if (!IsUnresolved(route, parameter, converters))
          continue;

        (string subject, string reason) = parameter.IsCatchAll
          ? ($"Catch-all parameter '{parameter.Name}'", CatchAllReason)
          : ($"Parameter '{parameter.Name}'", ScalarReason);
        diagnostics.Add(Create(route, routeLocations, subject, parameter.TypeConstraint!, reason));
      }

      foreach (OptionDefinition option in route.Options)
      {
        if (!IsUnresolved(route, option, converters))
          continue;

        string display = option.LongFormWithPrefix ?? option.ShortFormWithPrefix!;
        string reason = option.IsRepeated ? RepeatedOptionReason : ScalarReason;
        diagnostics.Add(Create(route, routeLocations, $"Option '{display}'", option.TypeConstraint!, reason));
      }
    }

    return [.. diagnostics];
  }

  /// <summary>
  /// True when NURU_S010 reports this route parameter: the route is fluent and its constraint has no conversion.
  /// </summary>
  internal static bool IsUnresolved(
    RouteDefinition route,
    ParameterDefinition parameter,
    IEnumerable<CustomConverterDefinition>? converters)
  {
    if (route.Handler.HandlerKind == HandlerKind.Command)
      return false;

    if (parameter.IsCatchAll)
      return !IsKnownListElement(parameter.TypeConstraint, allowEnum: false, [], route);

    return !IsKnown(parameter.TypeConstraint, [parameter.Name], route, converters);
  }

  /// <summary>
  /// True when NURU_S010 reports this value option: the route is fluent and its constraint has no conversion.
  /// </summary>
  internal static bool IsUnresolved(
    RouteDefinition route,
    OptionDefinition option,
    IEnumerable<CustomConverterDefinition>? converters)
  {
    if (route.Handler.HandlerKind == HandlerKind.Command || option.IsFlag || option.IsJsonValue)
      return false;

    string?[] names = [option.LongForm, option.ShortForm, option.ParameterName];
    return option.IsRepeated
      ? !IsKnownListElement(option.TypeConstraint, allowEnum: true, names, route)
      : !IsKnown(option.TypeConstraint, names, route, converters);
  }

  private static bool IsKnown(
    string? typeConstraint,
    string?[] boundNames,
    RouteDefinition route,
    IEnumerable<CustomConverterDefinition>? converters)
  {
    if (string.IsNullOrEmpty(typeConstraint))
      return true;

    string baseType = typeConstraint!.TrimEnd('?');
    if (TypeConversionMap.IsBuiltInType(baseType))
      return true;

    if (CustomConverterMatcher.Find(baseType, converters) is not null)
      return true;

    return IsEnumBound(boundNames, route);
  }

  // Catch-all and repeated-option arrays: string, a built-in TryParse element, or (repeated options) an enum.
  private static bool IsKnownListElement(string? typeConstraint, bool allowEnum, string?[] boundNames, RouteDefinition route)
  {
    if (string.IsNullOrEmpty(typeConstraint) || TypeConversionMap.IsStringConstraint(typeConstraint))
      return true;

    string baseType = typeConstraint!.TrimEnd('?');
    if (TypeConversionMap.GetBuiltInTryConversion(baseType, "__value", "__parsed") is not null)
      return true;

    return allowEnum && IsEnumBound(boundNames, route);
  }

  // EnumTypeConverter<T>: the emitters bind an enum handler parameter by its segment or option name.
  private static bool IsEnumBound(string?[] boundNames, RouteDefinition route) =>
    route.Handler.Parameters.Any(p =>
      p.IsEnumType &&
      boundNames.Any(name => string.Equals(p.SourceName, name, StringComparison.OrdinalIgnoreCase)));

  private static Diagnostic Create(
    RouteDefinition route,
    IReadOnlyDictionary<string, Location> routeLocations,
    string subject,
    string typeConstraint,
    string reason) =>
    Diagnostic.Create(
      DiagnosticDescriptors.UnknownTypeConstraint,
      RouteLocationLookup.FindOrNone(route, routeLocations),
      subject,
      typeConstraint.TrimEnd('?'),
      route.FullPattern,
      reason);
}
