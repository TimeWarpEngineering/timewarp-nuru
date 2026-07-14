namespace TimeWarp.Nuru;

internal static partial class DiagnosticDescriptors
{
  // Categories for grouping diagnostics
  internal const string SyntaxCategory = "RoutePattern.Syntax";
  internal const string SemanticCategory = "RoutePattern.Semantic";

  // NOTE: NURU002 (ClosureNotAllowed) and NURU003 (MethodGroupNotSupported) diagnostics
  // will be added when we implement diagnostic reporting for handler generation.
  // For now, we silently skip handler generation in these cases.
}
