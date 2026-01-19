// Extracts completion data from route definitions.
// Shared between ReplEmitter and CompletionEmitter.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Extracts completion-relevant data from route definitions.
/// Used by both REPL and shell completion emitters.
/// </summary>
internal static class CompletionDataExtractor
{
  /// <summary>
  /// Extracts command prefixes from route definitions.
  /// A command prefix is the sequence of leading literal segments.
  /// </summary>
  public static List<string> ExtractCommandPrefixes(IEnumerable<RouteDefinition> routes)
  {
    HashSet<string> prefixes = new(StringComparer.OrdinalIgnoreCase);

    foreach (RouteDefinition route in routes)
    {
      StringBuilder prefix = new();

      foreach (LiteralDefinition literal in route.Literals)
      {
        if (prefix.Length > 0)
          prefix.Append(' ');
        prefix.Append(literal.Value);
      }

      if (prefix.Length > 0)
        prefixes.Add(prefix.ToString());
    }

    return [.. prefixes];
  }

  /// <summary>
  /// Extracts option information from route definitions.
  /// </summary>
  public static List<OptionInfo> ExtractOptions(IEnumerable<RouteDefinition> routes)
  {
    List<OptionInfo> options = [];

    foreach (RouteDefinition route in routes)
    {
      foreach (OptionDefinition opt in route.Options)
      {
        options.Add(new OptionInfo(opt.LongForm, opt.ShortForm, opt.Description, opt.ExpectsValue, opt.TypeConstraint));
      }
    }

    return options;
  }

  /// <summary>
  /// Extracts route-specific options with their command prefix for context-aware completions.
  /// </summary>
  public static List<RouteOptionInfo> ExtractRouteOptions(IEnumerable<RouteDefinition> routes)
  {
    List<RouteOptionInfo> routeOptions = [];

    foreach (RouteDefinition route in routes)
    {
      if (!route.Options.Any())
        continue;

      // Get the command prefix for this route
      string cmdPrefix = string.Join(" ", route.Literals.Select(l => l.Value));
      if (string.IsNullOrEmpty(cmdPrefix))
        continue;

      List<OptionInfo> options = [];
      foreach (OptionDefinition opt in route.Options)
      {
        options.Add(new OptionInfo(opt.LongForm, opt.ShortForm, opt.Description, opt.ExpectsValue, opt.TypeConstraint));
      }

      routeOptions.Add(new RouteOptionInfo(cmdPrefix, options));
    }

    return routeOptions;
  }

  /// <summary>
  /// Extracts parameter information from route definitions.
  /// </summary>
  public static List<ParameterInfo> ExtractParameters(IEnumerable<RouteDefinition> routes)
  {
    List<ParameterInfo> parameters = [];

    foreach (RouteDefinition route in routes)
    {
      // Get the command prefix for context
      string cmdPrefix = string.Join(" ", route.Literals.Select(l => l.Value));

      foreach (ParameterDefinition param in route.Parameters)
      {
        parameters.Add(new ParameterInfo(param.Name, param.TypeConstraint, param.Description, cmdPrefix));
      }
    }

    return parameters;
  }

  /// <summary>
  /// Extracts enum parameter information from route definitions using Roslyn compilation.
  /// </summary>
  public static List<EnumParameterInfo> ExtractEnumParameters(IEnumerable<RouteDefinition> routes, Compilation compilation)
  {
    List<EnumParameterInfo> result = [];

    foreach (RouteDefinition route in routes)
    {
      if (route.Handler is null)
        continue;

      // Get the command prefix for this route
      string cmdPrefix = string.Join(" ", route.Literals.Select(l => l.Value));
      if (string.IsNullOrEmpty(cmdPrefix))
        continue;

      // Track position for parameters from the route
      int position = 0;
      foreach (ParameterBinding param in route.Handler.Parameters.Where(p => p.Source == BindingSource.Parameter))
      {
        // Get the type symbol from the parameter type name
        string typeName = param.ParameterTypeName;

        // Remove global:: prefix and nullable suffix for lookup
        if (typeName.StartsWith("global::", StringComparison.Ordinal))
          typeName = typeName[8..];
        if (typeName.EndsWith('?'))
          typeName = typeName[..^1];

        INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName(typeName);

        if (typeSymbol?.TypeKind == TypeKind.Enum)
        {
          // Extract enum member names
          List<string> values =
          [
            .. typeSymbol.GetMembers()
              .OfType<IFieldSymbol>()
              .Where(f => f.HasConstantValue)
              .Select(f => f.Name)
          ];

          if (values.Count > 0)
          {
            result.Add(new EnumParameterInfo(cmdPrefix, position, param.ParameterName, values));
          }
        }

        position++;
      }
    }

    return result;
  }

  /// <summary>
  /// Information about an option for completion generation.
  /// </summary>
  public sealed record OptionInfo(string? LongForm, string? ShortForm, string? Description, bool ExpectsValue, string? TypeConstraint);

  /// <summary>
  /// Information about a parameter for completion generation.
  /// </summary>
  public sealed record ParameterInfo(string Name, string? TypeConstraint, string? Description, string CommandPrefix);

  /// <summary>
  /// Information about an enum parameter for position-aware completion generation.
  /// </summary>
  public sealed record EnumParameterInfo(string CommandPrefix, int Position, string ParameterName, List<string> Values);

  /// <summary>
  /// Information about route-specific options for context-aware completion generation.
  /// </summary>
  public sealed record RouteOptionInfo(string CommandPrefix, List<OptionInfo> Options);
}
