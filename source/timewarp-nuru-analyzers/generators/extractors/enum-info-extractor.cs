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
///   <item><c>CapabilitiesEmitter.ExtractEnumValues</c> — <c>ParameterDefinition/OptionDefinition.ResolvedClrTypeName</c>,
///     then the matching handler parameter type when that does not resolve (already covered by set 1).</item>
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

      INamedTypeSymbol? typeSymbol = ResolveEnumSymbol(compilation, typeName);
      if (typeSymbol is null)
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
  /// Normalizes a type name for metadata lookup: strips the <c>global::</c> prefix, trailing
  /// nullable <c>?</c>, array suffixes (<c>[]</c>), and collection wrappers
  /// (<c>IEnumerable&lt;T&gt;</c>/<c>IList&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>) so
  /// <c>MyEnum[]</c> and <c>IEnumerable&lt;MyEnum&gt;</c> resolve to the element enum.
  /// Must match the normalization the emitters apply to their lookup keys.
  /// </summary>
  public static string Normalize(string typeName)
  {
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];

    while (true)
    {
      if (typeName.EndsWith('?'))
      {
        typeName = typeName[..^1];
        continue;
      }

      if (typeName.EndsWith("[]", StringComparison.Ordinal))
      {
        typeName = typeName[..^2];
        continue;
      }

      if (TryUnwrapCollectionTypeArgument(typeName, out string elementType))
      {
        typeName = elementType;
        if (typeName.StartsWith("global::", StringComparison.Ordinal))
          typeName = typeName[8..];
        continue;
      }

      break;
    }

    return typeName;
  }

  /// <summary>
  /// If <paramref name="typeName"/> is IEnumerable&lt;T&gt;, IList&lt;T&gt;, or ICollection&lt;T&gt;,
  /// returns the type argument T.
  /// </summary>
  private static bool TryUnwrapCollectionTypeArgument(string typeName, out string elementType)
  {
    elementType = "";

    string[] prefixes =
    [
      "System.Collections.Generic.IEnumerable<",
      "System.Collections.Generic.IList<",
      "System.Collections.Generic.ICollection<",
      "IEnumerable<",
      "IList<",
      "ICollection<"
    ];

    foreach (string prefix in prefixes)
    {
      if (typeName.StartsWith(prefix, StringComparison.Ordinal) && typeName.EndsWith('>'))
      {
        elementType = typeName[prefix.Length..^1];
        return elementType.Length > 0;
      }
    }

    return false;
  }

  /// <summary>
  /// Resolves a normalized C# type name (as produced by <c>FullyQualifiedFormat</c>, minus
  /// <c>global::</c>) to its enum symbol. A nested type's display name (<c>NS.Outer.Inner</c>,
  /// <c>NS.Outer&lt;int&gt;.Inner</c>) is not its metadata name (<c>NS.Outer+Inner</c>,
  /// <c>NS.Outer`1+Inner</c>), so <see cref="Compilation.GetTypeByMetadataName"/> misses it; the
  /// fallback walks the namespace and type members of <see cref="Compilation.GlobalNamespace"/>
  /// segment by segment, matching each type segment by name and generic arity.
  /// </summary>
  private static INamedTypeSymbol? ResolveEnumSymbol(Compilation compilation, string typeName)
  {
    INamedTypeSymbol? byMetadata = compilation.GetTypeByMetadataName(typeName);
    if (byMetadata is not null)
      return byMetadata.TypeKind == TypeKind.Enum ? byMetadata : null;

    List<(string Name, int Arity)>? segments = SplitTypeName(typeName);
    if (segments is null || segments.Count < 2)
      return null;

    return FindEnum(compilation.GlobalNamespace, segments, 0);
  }

  private static INamedTypeSymbol? FindEnum(INamespaceOrTypeSymbol container, List<(string Name, int Arity)> segments, int index)
  {
    (string name, int arity) = segments[index];
    bool isLast = index == segments.Count - 1;

    foreach (ISymbol member in container.GetMembers(name))
    {
      if (member is INamedTypeSymbol type && type.Arity == arity)
      {
        if (isLast)
        {
          if (type.TypeKind == TypeKind.Enum)
            return type;
        }
        else if (FindEnum(type, segments, index + 1) is INamedTypeSymbol nested)
        {
          return nested;
        }
      }
      else if (!isLast && arity == 0 && member is INamespaceSymbol ns
        && FindEnum(ns, segments, index + 1) is INamedTypeSymbol inNamespace)
      {
        return inNamespace;
      }
    }

    return null;
  }

  /// <summary>
  /// Splits <c>NS.Outer&lt;A.B, C&gt;.Inner</c> on top-level dots into (name, generic arity)
  /// segments: <c>(NS, 0), (Outer, 2), (Inner, 0)</c>. Returns null for a malformed name.
  /// </summary>
  private static List<(string Name, int Arity)>? SplitTypeName(string typeName)
  {
    List<(string Name, int Arity)> segments = [];
    int depth = 0;
    int arity = 0;
    int nameEnd = -1;
    int start = 0;

    for (int i = 0; i <= typeName.Length; i++)
    {
      char c = i < typeName.Length ? typeName[i] : '.';
      switch (c)
      {
        case '<' or '(' or '[':
          if (depth == 0 && c == '<')
          {
            nameEnd = i;
            arity = 1;
          }

          depth++;
          break;
        case '>' or ')' or ']':
          depth--;
          if (depth < 0)
            return null;
          break;
        case ',' when depth == 1 && nameEnd >= 0:
          arity++;
          break;
        case '.' when depth == 0:
          string name = typeName[start..(nameEnd >= 0 ? nameEnd : i)].Trim();
          if (name.StartsWith('@'))
            name = name[1..];
          if (name.Length == 0)
            return null;

          segments.Add((name, nameEnd >= 0 ? arity : 0));
          start = i + 1;
          nameEnd = -1;
          arity = 0;
          break;
      }
    }

    return depth == 0 ? segments : null;
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
