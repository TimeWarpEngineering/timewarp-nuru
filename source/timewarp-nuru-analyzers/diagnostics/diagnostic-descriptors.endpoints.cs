namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for endpoint validation errors.
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string EndpointsCategory = "Endpoints";

  public static readonly DiagnosticDescriptor InvalidNuruRoutePattern = new(
      id: "NURU_A001",
      title: "Invalid NuruRoute pattern",
      messageFormat: "Route pattern '{0}' is invalid. [NuruRoute] must contain only a single literal identifier (e.g., 'work') or empty string for root route. Use [NuruRouteGroup] for multi-word routes, [Parameter] for parameters, and [Option] for options.",
      category: EndpointsCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Route patterns in [NuruRoute] must be a single literal identifier (e.g., 'work', 'status') or an empty string for the root/default route. For multi-word routes like 'docker run', create a base class with [NuruRouteGroup(\"docker\")] and use [NuruRoute(\"run\")] on the derived class. Define parameters using [Parameter] attributes and options using [Option] attributes on class properties.");

  public static readonly DiagnosticDescriptor MultipleParametersRequireOrder = new(
      id: "NURU_A002",
      title: "Multiple parameters require explicit Order",
      messageFormat: "Parameter '{0}' requires an explicit Order value because the command has multiple parameters",
      category: EndpointsCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "When a command has multiple [Parameter] attributes, each must specify an Order value to ensure deterministic argument ordering. Use [Parameter(Order = 0, ...)] for the first parameter, [Parameter(Order = 1, ...)] for the second, etc.");

  public static readonly DiagnosticDescriptor DiscoverEndpointsAcrossApps = new(
      id: "NURU_A003",
      title: "DiscoverEndpoints() pulls in endpoints meant for other apps",
      messageFormat: "DiscoverEndpoints() adds all {0} endpoints in this compilation to this app, and this compilation builds Nuru apps in {1} source files. Map the endpoints this app needs with Map<TEndpoint>().",
      category: EndpointsCategory,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "DiscoverEndpoints() adds every [NuruRoute] endpoint in the compilation to the app. When one compilation builds apps in several source files (for example the Jaribu JARIBU_MULTI test assembly), each discovering app gets the endpoints of every other file, and the generator emits matcher, help and capabilities code for all of them per app. Use Map<TEndpoint>() for each endpoint the app needs, or DiscoverEndpoints(typeof(Group)) to scope discovery to a group. A test that exercises discovery itself can opt out with #pragma warning disable NURU_A003 around the call, or run standalone (one file per compilation). A compilation whose apps are all in one file never reports this.");
}
