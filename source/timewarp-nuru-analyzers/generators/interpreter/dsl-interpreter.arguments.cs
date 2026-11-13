#region Purpose
// Invocation argument extraction helpers for the DSL interpreter.
#endregion

namespace TimeWarp.Nuru.Generators;

public sealed partial class DslInterpreter
{
  /// <summary>
  /// Gets the method name from an invocation expression.
  /// </summary>
  private static string? GetMethodName(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      _ => null
    };
  }

  /// <summary>
  /// Extracts the first string argument from a method invocation.
  /// </summary>
  private static string? ExtractStringArgument(InvocationExpressionSyntax invocation) =>
    ExtractStringArgumentAt(invocation, 0);

  /// <summary>
  /// Extracts a string argument from a method invocation, at the given position.
  /// </summary>
  /// <param name="invocation">The method invocation.</param>
  /// <param name="index">
  /// The positional slot to fall back to when no argument is named <paramref name="parameterName"/>
  /// (or <paramref name="parameterName"/> is null). Matches plain <c>args.Arguments[index]</c>
  /// indexing - existing single-string callers (WithDescription/WithAlias/WithName, via
  /// <see cref="ExtractStringArgument"/>) rely on this to resolve a lone named argument
  /// (e.g. <c>.WithDescription(description: "x")</c>), which always sits at index 0 regardless
  /// of naming.
  /// </param>
  /// <param name="parameterName">
  /// When non-null, an argument whose <c>NameColon</c> matches this name is preferred over the
  /// positional slot, wherever it appears in the argument list. This is what makes reordered
  /// named arguments resolve correctly (e.g. <c>.WithExample(description: "d", command: "c")</c>
  /// must bind "c" to command even though it's the second argument syntactically).
  /// </param>
  /// <returns>The string value, or null when absent or not a string literal.</returns>
  private static string? ExtractStringArgumentAt(InvocationExpressionSyntax invocation, int index, string? parameterName = null)
  {
    ArgumentSyntax? argument = ResolveArgumentAt(invocation, index, parameterName);
    return argument is null ? null : ExtractLiteralStringValue(argument.Expression);
  }

  /// <summary>
  /// Resolves the <see cref="ArgumentSyntax"/> at the given position from a method invocation,
  /// same name/position rules as <see cref="ExtractStringArgumentAt"/> (see its docs), but
  /// returns the argument itself rather than its literal string value - callers that need to
  /// distinguish "argument absent" from "argument present but not a string literal" (e.g.
  /// WithExample's command, which must throw on the latter but tolerate the former differently)
  /// use this directly instead of the value-only helper.
  /// </summary>
  private static ArgumentSyntax? ResolveArgumentAt(InvocationExpressionSyntax invocation, int index, string? parameterName = null)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null)
      return null;

    if (parameterName is not null)
    {
      foreach (ArgumentSyntax candidate in args.Arguments)
      {
        if (candidate.NameColon?.Name.Identifier.Text == parameterName)
          return candidate;
      }
    }

    return args.Arguments.Count <= index ? null : args.Arguments[index];
  }

  /// <summary>
  /// Extracts the string value from an expression when it is a string literal, null otherwise.
  /// </summary>
  private static string? ExtractLiteralStringValue(ExpressionSyntax expression) =>
    expression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };

  /// <summary>
  /// Creates a diagnostic from an exception that occurred during interpretation.
  /// Used as a fallback for not-yet-converted exception throws.
  /// </summary>
  private static Diagnostic CreateDiagnosticFromException(Exception exception, Location location)
  {
    // Use NURU_S999 as a general extraction error diagnostic
    // This is a fallback - ideally all throws should be converted to proper diagnostics
    DiagnosticDescriptor descriptor = new(
      id: "NURU_S999",
      title: "DSL Interpretation Error",
      messageFormat: "{0}",
      category: "TimeWarp.Nuru.Semantic",
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "An error occurred while interpreting the DSL code.");

    return Diagnostic.Create(descriptor, location, exception.Message);
  }
}
