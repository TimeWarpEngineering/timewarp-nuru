namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Resolves the enum member names the emit stage needs, up front, into a value-equatable
/// <see cref="EnumInfo"/> set — so the emitters no longer need the live <see cref="Compilation"/>.
/// </summary>
/// <remarks>
/// This runs in a provider that is combined with the <c>CompilationProvider</c>, so it re-executes
/// on every edit; but its OUTPUT is equatable, so when the enum shapes are unchanged the emit stage
/// (which consumes this set, not the compilation) compares equal and is cached.
///
/// Correctness requirement: it must gather the UNION of every candidate type-name string that any
/// emitter might later resolve, because a MISSING enum silently drops <c>AllowedValues</c>/completion
/// values. The two downstream resolution sites are:
/// <list type="number">
///   <item><c>CompletionDataExtractor.ExtractEnumParameters</c> — <c>Handler.Parameters[*].ParameterTypeName</c>.</item>
///   <item><c>CapabilitiesEmitter.ExtractEnumValues</c> — <c>ParameterDefinition/OptionDefinition.ResolvedClrTypeName</c>
///     with a fallback to the matching handler parameter type (already covered by set 1).</item>
/// </list>
/// A superset is safe (extra entries are harmless).
/// </remarks>
internal static class EnumInfoExtractor
{
  public static EquatableArray<EnumInfo> Resolve(GeneratorModel? model, Compilation compilation, CancellationToken cancellationToken)
  {
    if (model is null)
      return [];

    HashSet<string> candidates = new(StringComparer.Ordinal);

    foreach (RouteDefinition route in EnumerateRoutes(model))
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (route.Handler is not null)
      {
        foreach (ParameterBinding param in route.Handler.Parameters)
          AddCandidate(candidates, param.ParameterTypeName);
      }

      foreach (ParameterDefinition param in route.Parameters)
        AddCandidate(candidates, param.ResolvedClrTypeName);

      foreach (OptionDefinition option in route.Options)
        AddCandidate(candidates, option.ResolvedClrTypeName);
    }

    if (candidates.Count == 0)
      return [];

    List<EnumInfo> result = [];
    foreach (string typeName in candidates)
    {
      cancellationToken.ThrowIfCancellationRequested();

      INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName(typeName);
      if (typeSymbol?.TypeKind != TypeKind.Enum)
        continue;

      EquatableArray<string> members =
      [
        .. typeSymbol.GetMembers()
          .OfType<IFieldSymbol>()
          .Where(f => f.HasConstantValue)
          .Select(f => f.Name)
      ];

      if (members.Length > 0)
        result.Add(new EnumInfo(typeName, members));
    }

    return [.. result];
  }

  /// <summary>
  /// Builds a lookup dictionary (normalized metadata name → member names) for the emitters.
  /// </summary>
  public static IReadOnlyDictionary<string, ImmutableArray<string>> BuildLookup(EquatableArray<EnumInfo> enumInfo)
  {
    Dictionary<string, ImmutableArray<string>> lookup = new(StringComparer.Ordinal);
    foreach (EnumInfo info in enumInfo)
      lookup[info.MetadataTypeName] = info.MemberNames;

    return lookup;
  }

  /// <summary>
  /// Normalizes a type name for metadata lookup: strips the <c>global::</c> prefix and a trailing
  /// nullable <c>?</c>. Must match the normalization the emitters apply to their lookup keys.
  /// </summary>
  public static string Normalize(string typeName)
  {
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];
    if (typeName.EndsWith('?'))
      typeName = typeName[..^1];

    return typeName;
  }

  private static void AddCandidate(HashSet<string> candidates, string? typeName)
  {
    if (string.IsNullOrEmpty(typeName))
      return;

    string normalized = Normalize(typeName!);
    if (normalized.Length > 0)
      candidates.Add(normalized);
  }

  private static IEnumerable<RouteDefinition> EnumerateRoutes(GeneratorModel model)
  {
    foreach (AppModel app in model.Apps)
    {
      foreach (RouteDefinition route in app.Routes)
        yield return route;
    }

    foreach (RouteDefinition endpoint in model.Endpoints)
      yield return endpoint;
  }
}
