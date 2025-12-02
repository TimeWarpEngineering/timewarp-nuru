namespace TimeWarp.Nuru;

internal static partial class DiagnosticDescriptors
{
  // Categories for grouping diagnostics
  internal const string SyntaxCategory = "RoutePattern.Syntax";
  internal const string SemanticCategory = "RoutePattern.Semantic";

  // Debug diagnostic for development
  public static readonly DiagnosticDescriptor DebugRouteFound = new(
      id: "NURU_DEBUG",
      title: "Route pattern found",
      messageFormat: "Found route: '{0}'",
      category: "Debug",
      defaultSeverity: DiagnosticSeverity.Hidden,
      isEnabledByDefault: true,
      description: "Debug diagnostic to verify route detection during development.");
}
