namespace TimeWarp.Nuru;

/// <summary>
/// Pattern building methods for constructing route pattern strings and inferring message types.
/// </summary>
public partial class NuruAttributedRouteGenerator
{
  private static string BuildPatternString(AttributedRouteInfo route)
  {
    List<string> parts = [];

    // Group prefix
    if (!string.IsNullOrEmpty(route.GroupPrefix))
      parts.Add(route.GroupPrefix);

    // Pattern (single literal)
    if (!string.IsNullOrEmpty(route.Pattern))
      parts.Add(route.Pattern);

    // Add parameters from [Parameter] attributes
    foreach (ParameterInfo param in route.Parameters)
    {
      if (param.IsCatchAll)
        parts.Add($"<*{param.Name}>");
      else if (param.IsOptional)
        parts.Add($"[{param.Name}]");
      else
        parts.Add($"<{param.Name}>");
    }

    // Group options
    foreach (GroupOptionInfo opt in route.GroupOptions)
    {
      parts.Add(BuildOptionPatternPart(opt.LongForm, opt.ShortForm, opt.PropertyName, opt.IsFlag, opt.IsValueOptional));
    }

    // Options
    foreach (OptionInfo opt in route.Options)
    {
      parts.Add(BuildOptionPatternPart(opt.LongForm, opt.ShortForm, opt.PropertyName, opt.IsFlag, opt.IsValueOptional));
    }

    return string.Join(" ", parts);
  }

  private static string BuildAliasPatternString(AttributedRouteInfo route, string alias)
  {
    List<string> parts = [];

    // Alias replaces the pattern (but keeps group prefix if any)
    if (!string.IsNullOrEmpty(route.GroupPrefix))
      parts.Add(route.GroupPrefix);

    parts.Add(alias);

    // Add parameters from [Parameter] attributes (same as main route)
    foreach (ParameterInfo param in route.Parameters)
    {
      if (param.IsCatchAll)
        parts.Add($"<*{param.Name}>");
      else if (param.IsOptional)
        parts.Add($"[{param.Name}]");
      else
        parts.Add($"<{param.Name}>");
    }

    // Group options
    foreach (GroupOptionInfo opt in route.GroupOptions)
    {
      parts.Add(BuildOptionPatternPart(opt.LongForm, opt.ShortForm, opt.PropertyName, opt.IsFlag, opt.IsValueOptional));
    }

    // Options
    foreach (OptionInfo opt in route.Options)
    {
      parts.Add(BuildOptionPatternPart(opt.LongForm, opt.ShortForm, opt.PropertyName, opt.IsFlag, opt.IsValueOptional));
    }

    return string.Join(" ", parts);
  }

  private static string BuildOptionPatternPart(string longForm, string? shortForm, string propertyName,
    bool isFlag, bool isValueOptional)
  {
    string optPart = shortForm is not null
      ? $"--{longForm},-{shortForm}"
      : $"--{longForm}";

    if (isFlag)
    {
      // Boolean flags are always optional - add ? to match help route pattern style
      optPart += "?";
    }
    else
    {
      string paramName = ToCamelCase(propertyName);
      optPart += isValueOptional ? $" {{{paramName}?}}" : $" {{{paramName}}}";
    }

    return optPart;
  }

  /// <summary>
  /// Infers the MessageType from the interfaces implemented by the class.
  /// </summary>
  /// <remarks>
  /// Detection priority:
  /// 1. IQuery&lt;T&gt; → Query
  /// 2. ICommand&lt;T&gt; + IIdempotent → IdempotentCommand
  /// 3. ICommand&lt;T&gt; → Command
  /// 4. IRequest&lt;T&gt; or none → Unspecified
  /// </remarks>
  private static string InferMessageType(INamedTypeSymbol classSymbol)
  {
    bool implementsQuery = false;
    bool implementsCommand = false;
    bool implementsIdempotent = false;

    foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
    {
      string name = iface.Name;
      string fullName = iface.OriginalDefinition.ToDisplayString();

      // Check for IQuery<T> (generic interface with one type argument)
      if (name == "IQuery" && iface.IsGenericType && iface.TypeArguments.Length == 1)
      {
        implementsQuery = true;
      }
      // Check for ICommand<T> (generic interface with one type argument)
      else if (name == "ICommand" && iface.IsGenericType && iface.TypeArguments.Length == 1)
      {
        implementsCommand = true;
      }
      // Check for IIdempotent marker interface
      else if (fullName == "TimeWarp.Nuru.IIdempotent")
      {
        implementsIdempotent = true;
      }
    }

    // Apply priority: Query > IdempotentCommand > Command > Unspecified
    if (implementsQuery)
      return "Query";

    if (implementsCommand)
      return implementsIdempotent ? "IdempotentCommand" : "Command";

    // If none of the above, it's Unspecified (likely IRequest<T> or no mediator interface)
    return "Unspecified";
  }
}
