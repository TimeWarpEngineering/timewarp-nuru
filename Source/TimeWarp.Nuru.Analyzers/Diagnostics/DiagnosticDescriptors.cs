namespace TimeWarp.Nuru.Analyzers.Diagnostics;

internal static class DiagnosticDescriptors
{
  private const string Category = "RoutePattern";

  // Syntax Errors
  // Temporary debug diagnostic
  public static readonly DiagnosticDescriptor DebugRouteFound = new(
      id: "NURU_DEBUG",
      title: "Route pattern found",
      messageFormat: "Found route: '{0}'",
      category: "Debug",
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "Temporary diagnostic to verify route detection.");

  public static readonly DiagnosticDescriptor InvalidParameterSyntax = new(
      id: "NURU001",
      title: "Invalid parameter syntax",
      messageFormat: "Invalid parameter syntax '{0}' - use curly braces: {{{1}}}",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Parameters must use curly braces {} instead of angle brackets <>.");

  public static readonly DiagnosticDescriptor UnbalancedBraces = new(
      id: "NURU002",
      title: "Unbalanced braces in route pattern",
      messageFormat: "Unbalanced braces in route pattern: {0}",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Route patterns must have matching opening and closing braces.");

  public static readonly DiagnosticDescriptor InvalidOptionFormat = new(
      id: "NURU003",
      title: "Invalid option format",
      messageFormat: "Invalid option format '{0}' - options must start with '--' or '-'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Options must start with double dash (--) for long form or single dash (-) for short form.");

  public static readonly DiagnosticDescriptor InvalidTypeConstraint = new(
      id: "NURU004",
      title: "Invalid type constraint",
      messageFormat: "Invalid type constraint '{0}' - supported types: string, int, double, bool, DateTime, Guid, long, float, decimal",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Type constraints must be one of the supported types or an enum type.");

  // Semantic Validations
  public static readonly DiagnosticDescriptor CatchAllNotAtEnd = new(
      id: "NURU005",
      title: "Catch-all parameter not at end of route",
      messageFormat: "Catch-all parameter '{0}' must be the last segment in the route",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Catch-all parameters (*) must appear as the last segment in a route pattern.");

  public static readonly DiagnosticDescriptor DuplicateParameterNames = new(
      id: "NURU006",
      title: "Duplicate parameter names in route",
      messageFormat: "Duplicate parameter name '{0}' found in route pattern",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Each parameter name must be unique within a route pattern.");

  public static readonly DiagnosticDescriptor ConflictingOptionalParameters = new(
      id: "NURU007",
      title: "Conflicting optional parameters",
      messageFormat: "Multiple consecutive optional parameters create ambiguity: {0}",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Having multiple consecutive optional parameters creates parsing ambiguity.");

  public static readonly DiagnosticDescriptor MixedCatchAllWithOptional = new(
      id: "NURU008",
      title: "Mixed catch-all with optional parameters",
      messageFormat: "Cannot mix optional parameters with catch-all parameter in the same route",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Routes cannot contain both optional parameters and catch-all parameters.");

  public static readonly DiagnosticDescriptor DuplicateOptionAlias = new(
      id: "NURU009",
      title: "Option with duplicate alias",
      messageFormat: "Option has duplicate short form '{0}'",
      category: Category,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Options cannot have the same short form specified multiple times.");
}
