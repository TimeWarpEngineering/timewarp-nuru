namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for semantic errors (validation issues in route patterns).
/// </summary>
internal static partial class DiagnosticDescriptors
{
  public static readonly DiagnosticDescriptor DuplicateParameterNames = new(
      id: "NURU_S001",
      title: "Duplicate parameter names in route",
      messageFormat: "Duplicate parameter name '{0}' found in route pattern",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Each parameter name must be unique within a route pattern.");

  public static readonly DiagnosticDescriptor ConflictingOptionalParameters = new(
      id: "NURU_S002",
      title: "Conflicting optional parameters",
      messageFormat: "Multiple consecutive optional parameters create ambiguity: {0}",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Having multiple consecutive optional parameters creates parsing ambiguity.");

  public static readonly DiagnosticDescriptor CatchAllNotAtEnd = new(
      id: "NURU_S003",
      title: "Catch-all parameter not at end of route",
      messageFormat: "Catch-all parameter '{0}' must be the last segment in the route (found '{1}' after it)",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Catch-all parameters (*) must appear as the last segment in a route pattern.");

  public static readonly DiagnosticDescriptor MixedCatchAllWithOptional = new(
      id: "NURU_S004",
      title: "Mixed catch-all with optional parameters",
      messageFormat: "Cannot mix optional parameters [{0}] with catch-all parameter '{1}' in the same route",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Routes cannot contain both optional parameters and catch-all parameters.");

  public static readonly DiagnosticDescriptor DuplicateOptionAlias = new(
      id: "NURU_S005",
      title: "Option with duplicate alias",
      messageFormat: "Option has duplicate short form '{0}' (conflicts with: {1})",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Options cannot have the same short form specified multiple times.");

  public static readonly DiagnosticDescriptor OptionalBeforeRequired = new(
      id: "NURU_S006",
      title: "Optional parameter before required parameter",
      messageFormat: "Optional parameter '{0}' appears before required parameter '{1}'",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Optional parameters must appear after all required parameters.");

  public static readonly DiagnosticDescriptor InvalidEndOfOptionsSeparator = new(
      id: "NURU_S007",
      title: "Invalid end-of-options separator",
      messageFormat: "Invalid use of end-of-options separator: {0}",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "The end-of-options separator '--' must be used correctly.");

  public static readonly DiagnosticDescriptor OptionsAfterEndOfOptionsSeparator = new(
      id: "NURU_S008",
      title: "Options after end-of-options separator",
      messageFormat: "Option '{0}' appears after end-of-options separator '--'",
      category: SemanticCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Options cannot appear after the end-of-options separator.");
}
