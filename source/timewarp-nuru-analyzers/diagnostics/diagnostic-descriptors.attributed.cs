namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for attributed route validation errors.
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string AttributedRoutesCategory = "AttributedRoutes";

  public static readonly DiagnosticDescriptor MultiWordPatternRequiresGroup = new(
      id: "NURU_A001",
      title: "Multi-word route pattern requires [NuruRouteGroup]",
      messageFormat: "Route pattern '{0}' contains multiple literals. Use [NuruRouteGroup] on a base class for multi-word routes.",
      category: AttributedRoutesCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Route patterns in [NuruRoute] must be a single word. For multi-word routes like 'config set', create a base class with [NuruRouteGroup(\"config\")] and use [NuruRoute(\"set\")] on the derived class.");

  public static readonly DiagnosticDescriptor MultipleParametersRequireOrder = new(
      id: "NURU_A002",
      title: "Multiple parameters require explicit Order",
      messageFormat: "Parameter '{0}' requires an explicit Order value because the command has multiple parameters",
      category: AttributedRoutesCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "When a command has multiple [Parameter] attributes, each must specify an Order value to ensure deterministic argument ordering. Use [Parameter(Order = 0, ...)] for the first parameter, [Parameter(Order = 1, ...)] for the second, etc.");
}
