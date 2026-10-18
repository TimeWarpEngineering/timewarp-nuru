#region Purpose
// Finds the route option that a command property binding reads, exact name first.
#endregion

#region Design
// --Quiet and --quiet are different options. An ignore-case match only applies to an
// option that no other binding claims by exact name. Command construction, the
// --json-args supplied flag, and the JSON slot assignment all use this one matcher,
// so they agree about which option (and which __supplied_ local) a property reads.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Matches a property binding to its <see cref="OptionDefinition"/>.
/// </summary>
internal static class OptionBindingMatcher
{
  internal static OptionDefinition? Match(RouteDefinition route, ParameterBinding param)
  {
    if (!IsOptionBinding(param))
      return null;

    foreach (OptionDefinition option in route.Options)
    {
      if (Matches(option, param, StringComparison.Ordinal))
        return option;
    }

    foreach (OptionDefinition option in route.Options)
    {
      if (Matches(option, param, StringComparison.OrdinalIgnoreCase) && !ClaimedByOther(route, option, param))
        return option;
    }

    return null;
  }

  private static bool ClaimedByOther(RouteDefinition route, OptionDefinition option, ParameterBinding param)
  {
    foreach (ParameterBinding other in route.Handler.Parameters)
    {
      if (!ReferenceEquals(other, param) && IsOptionBinding(other) && Matches(option, other, StringComparison.Ordinal))
        return true;
    }

    return false;
  }

  private static bool IsOptionBinding(ParameterBinding param) =>
    param.Source is BindingSource.Option or BindingSource.Flag;

  private static bool Matches(OptionDefinition option, ParameterBinding param, StringComparison comparison) =>
    string.Equals(option.LongForm, param.SourceName, comparison)
    || string.Equals(option.ShortForm, param.SourceName, comparison)
    || string.Equals(option.ParameterName, param.ParameterName, comparison);
}
