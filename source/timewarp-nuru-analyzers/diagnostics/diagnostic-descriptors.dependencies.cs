namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for dependency-related errors (missing packages).
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string DependencyCategory = "Dependencies";

  public static readonly DiagnosticDescriptor MissingMediatorPackages = new(
    id: "NURU_D001",
    title: "Mediator packages required for Map<TCommand>",
    messageFormat: "Map<{0}> requires Mediator packages. Run: dotnet add package Mediator.Abstractions && dotnet add package Mediator.SourceGenerator.",
    category: DependencyCategory,
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true,
    description: "The Map<TCommand> pattern requires Mediator.Abstractions and Mediator.SourceGenerator packages.");
}
