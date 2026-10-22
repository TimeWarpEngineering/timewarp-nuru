// Detects NURU_A003: unfiltered DiscoverEndpoints() in a compilation that builds apps in more
// than one source file.
//
// DiscoverEndpoints() adds every endpoint in the compilation to the app. In the Jaribu
// JARIBU_MULTI assembly every test file is one compilation, so each discovering app gets the
// endpoints of every other file and the emitted code grows with apps x endpoints (task 522).
//
// The trigger is "apps in more than one file", not "more than one app": a standalone test file
// builds many apps that share the endpoints declared in that file, which is intended, and a
// normal CLI builds one app. DiscoverEndpoints(typeof(Group)) is scoped and is not reported.
// An app that also calls Map<T>() takes only the mapped endpoints, so its discovery adds nothing.

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Reports NURU_A003 when an app discovers all endpoints and the compilation builds apps in
/// more than one source file.
/// </summary>
internal static class DiscoverEndpointsValidator
{
  /// <summary>
  /// Counts the distinct source files that hold a Build() call.
  /// </summary>
  /// <param name="apps">Every app in the compilation.</param>
  public static int CountAppFiles(IEnumerable<AppModel> apps)
  {
    ArgumentNullException.ThrowIfNull(apps);

    return apps
      .Select(static app => app.BuildFilePath)
      .Where(static path => !string.IsNullOrEmpty(path))
      .Distinct(StringComparer.Ordinal)
      .Count();
  }

  /// <summary>
  /// Validates one app's unfiltered DiscoverEndpoints() call.
  /// </summary>
  /// <param name="app">The app model.</param>
  /// <param name="endpointCount">Endpoints the app's discovery adds.</param>
  /// <param name="appFileCount">Distinct source files that build an app in this compilation.</param>
  /// <param name="compilation">Binds the location to its syntax tree so #pragma applies.</param>
  /// <returns>The NURU_A003 diagnostic, or none.</returns>
  public static ImmutableArray<Diagnostic> Validate(
    AppModel app,
    int endpointCount,
    int appFileCount,
    Compilation? compilation)
  {
    ArgumentNullException.ThrowIfNull(app);

    if (appFileCount < 2 || endpointCount == 0 || !app.DiscoverEndpoints)
      return [];

    if (!app.FilterGroupTypeNames.IsDefaultOrEmpty || !app.ExplicitEndpointTypes.IsDefaultOrEmpty)
      return [];

    Location location = app.DiscoverEndpointsLocation?.ToLocation(compilation) ?? Location.None;

    return
    [
      Diagnostic.Create(
        DiagnosticDescriptors.DiscoverEndpointsAcrossApps,
        location,
        endpointCount,
        appFileCount)
    ];
  }
}
