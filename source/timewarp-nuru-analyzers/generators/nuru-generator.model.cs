#region Purpose
// Generator model creation, validation, endpoint filtering and intercept site merging (NuruGenerator partial).
#endregion

namespace TimeWarp.Nuru.Generators;

using TimeWarp.Nuru.Validation;

public sealed partial class NuruGenerator : IIncrementalGenerator
{
  /// <summary>
  /// Creates a GeneratorModel from all AppModels, validates the combined model,
  /// and returns both the model and any diagnostics.
  /// </summary>
  private static GeneratorModelWithDiagnostics? CreateGeneratorModelWithValidation
  (
    ImmutableArray<ExtractionResult> extractionResults,
    ImmutableArray<RouteDefinition?> endpoints,
    ImmutableDictionary<string, Location> routeLocations,
    ImmutableArray<Diagnostic> endpointDiagnostics,
    AssemblyMetadata assemblyMetadata,
    string? generatedMediatorNamespace,
    bool hasConfiguration,
    Compilation compilation,
    CancellationToken cancellationToken
  )
  {
    _ = cancellationToken;
    bool hasGeneratedMediator = generatedMediatorNamespace is not null;

    List<Diagnostic> allDiagnostics = [];

    // Collect extraction diagnostics from all results
    foreach (ExtractionResult result in extractionResults)
    {
      allDiagnostics.AddRange(result.Diagnostics);
    }

    // Collect endpoint extraction diagnostics (e.g., NURU_A001 for invalid patterns)
    allDiagnostics.AddRange(endpointDiagnostics);

    // Deduplicate by BuildLocation - each Build() call is one unique app
    // BuildLocation is the source location of the Build() call, which uniquely identifies the app
    Dictionary<string, AppModel> uniqueApps = [];

    foreach (ExtractionResult result in extractionResults)
    {
      // An app with no entry-point call is still emitted when its Build() can be intercepted,
      // so an in-process host can read NuruApp.Capabilities without calling RunAsync().
      if (result.Model is null || (result.Model.InterceptSitesByMethod.Length == 0 && result.Model.BuildSite is null))
        continue;

      // Use BuildLocation as the deduplication key
      // Same Build() call = same app (should be rare since we extract from Build() directly)
      string key = result.Model.BuildLocation ?? "unknown";

      if (!uniqueApps.TryGetValue(key, out AppModel? existingApp))
      {
        // Help is always available. Configuration sources follow AddConfiguration()
        // on this app or a handler parameter detected by AddConfigurationLocator.
        AppModel enrichedModel = result.Model with
        {
          HasHelp = true,
          HasConfiguration = result.Model.HasConfiguration || hasConfiguration
        };
        uniqueApps[key] = enrichedModel;
      }
      else
      {
        // Merge intercept sites (shouldn't happen often with Build()-based extraction)
        EquatableArray<InterceptSiteGroup> mergedSites =
          MergeInterceptSites(existingApp.InterceptSitesByMethod, result.Model.InterceptSitesByMethod);
        uniqueApps[key] = existingApp with { InterceptSitesByMethod = mergedSites };
      }
    }

    // If no apps found, we can't generate an interceptor
    if (uniqueApps.Count == 0)
      return new GeneratorModelWithDiagnostics(null, [.. allDiagnostics]);

    // Collect user usings from all apps
    ImmutableArray<string> userUsings = [.. uniqueApps.Values
      .SelectMany(a => a.UserUsings)
      .Distinct()];

    // Collect endpoints (non-null only)
    ImmutableArray<RouteDefinition> allEndpoints = [.. endpoints
      .OfType<RouteDefinition>()
      .DistinctBy(route => route.Handler.FullTypeName ?? route.EffectivePattern)];

    // NURU_A003 fires only when apps are built in more than one source file
    int appFileCount = DiscoverEndpointsValidator.CountAppFiles(uniqueApps.Values);

    // Validate each app's combined routes (fluent + filtered endpoints)
    // This catches duplicate routes between fluent and endpoint definitions
    foreach (AppModel app in uniqueApps.Values)
    {
      // Filter endpoints based on app's discovery mode
      ImmutableArray<RouteDefinition> endpointsForApp = FilterEndpointsForApp(app, allEndpoints);

      allDiagnostics.AddRange(
        DiscoverEndpointsValidator.Validate(app, endpointsForApp.Length, appFileCount, compilation));

      // Combine this app's fluent routes with filtered endpoints
      ImmutableArray<RouteDefinition> combinedRoutes = [.. app.Routes.Concat(endpointsForApp)];

      ImmutableArray<Diagnostic> validationDiagnostics = ModelValidator.Validate(
        app with { Routes = combinedRoutes },
        routeLocations,
        app.ExtensionMethods,
        hasGeneratedMediator,
        compilation);

      allDiagnostics.AddRange(validationDiagnostics);
    }

    GeneratorModel model = new(
      Apps: [.. uniqueApps.Values],
      UserUsings: userUsings,
      Endpoints: allEndpoints,
      Version: assemblyMetadata.Version,
      CommitHash: assemblyMetadata.CommitHash,
      CommitDate: assemblyMetadata.CommitDate,
      GeneratedMediatorNamespace: generatedMediatorNamespace,
      ProblemRendererTypeName: EndpointExtractor.FindProblemRenderer(compilation));

    return new GeneratorModelWithDiagnostics(model, [.. allDiagnostics]);
  }

  /// <summary>
  /// Filters endpoints based on the app's discovery mode.
  /// </summary>
  /// <param name="app">The app model with discovery settings.</param>
  /// <param name="allEndpoints">All discovered endpoint classes.</param>
  /// <returns>Endpoints that should be included in this app.</returns>
  private static ImmutableArray<RouteDefinition> FilterEndpointsForApp
  (
    AppModel app,
    ImmutableArray<RouteDefinition> allEndpoints
  )
  {
    // Priority 1: FilterGroupTypeNames (DiscoverEndpoints(typeof(...)))
    // Must be checked BEFORE DiscoverEndpoints because both set DiscoverEndpoints=true
    if (!app.FilterGroupTypeNames.IsDefaultOrEmpty && app.FilterGroupTypeNames.Length > 0)
    {
      return
      [
        .. allEndpoints
          .Where(e => !e.IsExplicitOnly && MatchesFilterGroupType(e, app.FilterGroupTypeNames))
          .Select(e => StripGroupPrefixAboveMatchedType(e, app.FilterGroupTypeNames))
      ];
    }

    // Priority 2: Explicit Map<T>() calls
    if (!app.ExplicitEndpointTypes.IsDefaultOrEmpty && app.ExplicitEndpointTypes.Length > 0)
    {
      return
      [
        .. allEndpoints.Where
        (
          e => app.ExplicitEndpointTypes.Any
          (
            t => e.Handler.FullTypeName?.EndsWith(t, StringComparison.Ordinal) == true ||
                 e.Handler.FullTypeName == t
          )
        )
      ];
    }

    // Priority 3: DiscoverEndpoints() without filters - include all endpoints
    // A referenced endpoint found only through Map<T>() belongs to the app that maps it.
    if (app.DiscoverEndpoints)
      return [.. allEndpoints.Where(static e => !e.IsExplicitOnly)];

    // Default: no endpoints (test isolation)
    return [];
  }

  /// <summary>
  /// Checks if an endpoint's group type hierarchy matches any of the filter types.
  /// </summary>
  private static bool MatchesFilterGroupType
  (
    RouteDefinition endpoint,
    ImmutableArray<string> filterTypeNames
  )
  {
    // Ungrouped endpoints don't match group filters
    if (endpoint.GroupTypeHierarchy.IsDefaultOrEmpty)
      return false;

    return endpoint.GroupTypeHierarchy.Any(typeName =>
      filterTypeNames.Any(filterType =>
        string.Equals(filterType, typeName, StringComparison.OrdinalIgnoreCase)));
  }

  /// <summary>
  /// Computes the stripped group prefix for an endpoint.
  /// Strips prefixes from types above the matched filter type.
  /// </summary>
  private static RouteDefinition StripGroupPrefixAboveMatchedType
  (
    RouteDefinition endpoint,
    ImmutableArray<string> filterTypeNames
  )
  {
    if (endpoint.GroupTypeHierarchy.IsDefaultOrEmpty || string.IsNullOrEmpty(endpoint.GroupPrefix))
    {
      // No group info - no changes needed
      return endpoint;
    }

    // Find the matched index in the type hierarchy
    // Type hierarchy is in root-to-leaf order (most general to most specific)
    int? matchedIndex = null;
    for (int i = 0; i < endpoint.GroupTypeHierarchy.Length; i++)
    {
      string typeName = endpoint.GroupTypeHierarchy[i];
      if (filterTypeNames.Any(filterType =>
        string.Equals(filterType, typeName, StringComparison.OrdinalIgnoreCase)))
      {
        matchedIndex = i;
        break;
      }
    }

    if (matchedIndex is null)
    {
      // No match - shouldn't happen since we already filtered, but be safe
      return endpoint;
    }

    // Split the current prefix into individual components
    // Prefix is built from root-to-leaf order, so we can slice by index
    string[] allPrefixes = endpoint.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // The number of non-empty prefixes in the hierarchy should match the length
    // We keep prefixes from matched index onwards
    if (matchedIndex.Value >= allPrefixes.Length)
    {
      // Matched type has no prefix, use empty prefix
      return endpoint with { GroupPrefix = null };
    }

    // For root groups (matchedIndex == 0), also strip the first prefix segment
    // This enables creating subset editions from root groups with the root prefix stripped
    int startIndex = matchedIndex.Value == 0
      ? matchedIndex.Value + 1 // Strip root prefix
      : matchedIndex.Value; // Keep matched type's prefix
    string[] effectivePrefixes = allPrefixes[startIndex..];
    string? effectivePrefix = effectivePrefixes.Length > 0
      ? string.Join(" ", effectivePrefixes)
      : null;

    return endpoint with { GroupPrefix = effectivePrefix };
  }

  /// <summary>
  /// Merges two intercept site dictionaries.
  /// </summary>
  private static EquatableArray<InterceptSiteGroup> MergeInterceptSites(
    EquatableArray<InterceptSiteGroup> a,
    EquatableArray<InterceptSiteGroup> b)
  {
    Dictionary<string, EquatableArray<InterceptSiteModel>> merged = [];
    foreach (InterceptSiteGroup group in a)
      merged[group.MethodName] = group.Sites;

    foreach (InterceptSiteGroup group in b)
    {
      merged[group.MethodName] = merged.TryGetValue(group.MethodName, out EquatableArray<InterceptSiteModel> existing)
        ? existing.AddRange(group.Sites)
        : group.Sites;
    }

    return [.. merged.Select(kvp => new InterceptSiteGroup(kvp.Key, kvp.Value))];
  }

  /// <summary>
  /// Route pattern with its source location for error reporting.
  /// Optionally includes the RouteDefinition for endpoints.
  /// </summary>
  private sealed record RouteWithLocation(string Pattern, Location Location, RouteDefinition? Route);

  /// <summary>
  /// Endpoint extraction result with location and diagnostics.
  /// Used to collect both routes and their extraction diagnostics.
  /// </summary>
  private sealed record EndpointWithLocationAndDiagnostics(
    string? Pattern,
    Location Location,
    RouteDefinition? Route,
    ImmutableArray<Diagnostic> Diagnostics);

  /// <summary>
  /// Generator model with collected diagnostics from extraction and validation.
  /// </summary>
  private sealed record GeneratorModelWithDiagnostics(GeneratorModel? Model, ImmutableArray<Diagnostic> Diagnostics);
}
