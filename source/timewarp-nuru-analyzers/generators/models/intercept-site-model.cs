namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents the source location of a RunAsync() call site.
/// Used to generate the [InterceptsLocation] attribute.
/// </summary>
/// <remarks>
/// In .NET 10 / C# 14, interceptors use the new <see cref="InterceptableLocation"/> API.
/// The generated <c>[InterceptsLocation(version, data)]</c> attribute syntax is precomputed
/// at extraction time and stored as a plain string, so this model is a pure value record
/// (no captured <see cref="InterceptableLocation"/>/<c>SyntaxTree</c>). That is required for
/// the model to participate in incremental-generator caching — a captured Roslyn location
/// object defeats value equality and re-runs the emit stage on every keystroke (454-010).
/// </remarks>
/// <param name="AttributeSyntax">Precomputed <c>[InterceptsLocation(version, data)]</c> attribute source</param>
/// <param name="FilePath">Absolute path to the source file (for diagnostics)</param>
/// <param name="Line">1-based line number (for diagnostics)</param>
/// <param name="Column">1-based column number (for diagnostics)</param>
public sealed record InterceptSiteModel(
  string AttributeSyntax,
  string FilePath,
  int Line,
  int Column)
{
  /// <summary>
  /// Formats the intercept location for diagnostic messages.
  /// </summary>
  public override string ToString() => $"{FilePath}({Line},{Column})";

  /// <summary>
  /// Creates from a Roslyn InterceptableLocation, precomputing the attribute syntax so the
  /// model holds no live Roslyn objects.
  /// </summary>
  public static InterceptSiteModel FromInterceptableLocation(InterceptableLocation interceptableLocation, Location location)
  {
    ArgumentNullException.ThrowIfNull(interceptableLocation);
    ArgumentNullException.ThrowIfNull(location);
    FileLinePositionSpan lineSpan = location.GetLineSpan();
    return new InterceptSiteModel(
      AttributeSyntax: interceptableLocation.GetInterceptsLocationAttributeSyntax(),
      FilePath: lineSpan.Path,
      Line: lineSpan.StartLinePosition.Line + 1,
      Column: lineSpan.StartLinePosition.Character + 1);
  }
}

/// <summary>
/// Intercept sites for a single builder method (e.g. "RunAsync", "RunReplAsync"), grouped so
/// AppModel can carry the map as an equatable <see cref="EquatableArray{T}"/> of groups rather
/// than a reference-equality <c>ImmutableDictionary</c> (which defeats caching — 454-010).
/// </summary>
/// <param name="MethodName">The builder method the sites intercept.</param>
/// <param name="Sites">The intercept sites for that method.</param>
public sealed record InterceptSiteGroup(string MethodName, EquatableArray<InterceptSiteModel> Sites);

/// <summary>
/// Lookup helpers over the grouped intercept-site map (replacing ImmutableDictionary.TryGetValue).
/// </summary>
public static class InterceptSiteGroupExtensions
{
  /// <summary>Finds the sites for a builder method; mirrors <c>IDictionary.TryGetValue</c>.</summary>
  public static bool TryGetSites(this EquatableArray<InterceptSiteGroup> groups, string methodName, out EquatableArray<InterceptSiteModel> sites)
  {
    foreach (InterceptSiteGroup group in groups)
    {
      if (group.MethodName == methodName)
      {
        sites = group.Sites;
        return true;
      }
    }

    sites = [];
    return false;
  }
}
