namespace TimeWarp.Nuru.Analyzers.Diagnostics;

/// <summary>
/// Diagnostic descriptors for parse errors (syntax issues in route patterns).
/// </summary>
internal static partial class DiagnosticDescriptors
{
  public static readonly DiagnosticDescriptor InvalidParameterSyntax = new(
      id: "NURU_P001",
      title: "Invalid parameter syntax",
      messageFormat: "Invalid parameter syntax '{0}' - use curly braces: {{{1}}}",
      category: SyntaxCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Parameters must use curly braces {} instead of angle brackets <>.");

  public static readonly DiagnosticDescriptor UnbalancedBraces = new(
      id: "NURU_P002",
      title: "Unbalanced braces in route pattern",
      messageFormat: "Unbalanced braces in route pattern: {0}",
      category: SyntaxCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Route patterns must have matching opening and closing braces.");

  public static readonly DiagnosticDescriptor InvalidOptionFormat = new(
      id: "NURU_P003",
      title: "Invalid option format",
      messageFormat: "Invalid option format '{0}' - options must start with '--' or '-'",
      category: SyntaxCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Options must start with double dash (--) for long form or single dash (-) for short form.");

  public static readonly DiagnosticDescriptor InvalidTypeConstraint = new(
      id: "NURU_P004",
      title: "Invalid type constraint",
      messageFormat: "Invalid type constraint '{0}' - supported types: string, int, double, bool, DateTime, Guid, long, decimal, TimeSpan",
      category: SyntaxCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Type constraints must be one of the supported types or an enum type.");

  public static readonly DiagnosticDescriptor InvalidCharacter = new(
      id: "NURU_P005",
      title: "Invalid character in route pattern",
      messageFormat: "Invalid character '{0}' in route pattern",
      category: SyntaxCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Route patterns must contain only valid characters.");

  public static readonly DiagnosticDescriptor UnexpectedToken = new(
      id: "NURU_P006",
      title: "Unexpected token in route pattern",
      messageFormat: "{0}, but found '{1}'",
      category: SyntaxCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "The parser encountered an unexpected token in the route pattern.");

  public static readonly DiagnosticDescriptor NullPattern = new(
      id: "NURU_P007",
      title: "Null route pattern",
      messageFormat: "Route pattern cannot be null",
      category: SyntaxCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "The route pattern must not be null.");
}