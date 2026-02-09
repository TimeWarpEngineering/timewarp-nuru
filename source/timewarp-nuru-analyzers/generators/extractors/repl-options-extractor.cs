// Extracts ReplOptions configuration from AddRepl() lambda expressions.
//
// Handles:
// - .AddRepl(options => { options.AutoStartWhenEmpty = true; })  - inline lambda with block
// - .AddRepl(options => options.AutoStartWhenEmpty = true)       - inline lambda with expression
//
// Extracts:
// - AutoStartWhenEmpty (bool)

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Extracts ReplOptions configuration from AddRepl() lambda expressions.
/// </summary>
internal static class ReplOptionsExtractor
{
  /// <summary>
  /// Extracts ReplOptions configuration from an AddRepl() invocation.
  /// </summary>
  /// <param name="invocation">The AddRepl() invocation.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Configured ReplModel, or null if extraction fails.</returns>
  public static ReplModel? Extract
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    // Start with defaults
    ReplModel options = ReplModel.Default;

    // Handle lambda expressions
    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractFromLambda(lambda, options, semanticModel, cancellationToken);
    }

    return null;
  }

  /// <summary>
  /// Extracts options from a lambda expression.
  /// </summary>
  private static ReplModel ExtractFromLambda
  (
    LambdaExpressionSyntax lambda,
    ReplModel baseOptions,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Get parameter name (usually "options" or "o")
    string? parameterName = lambda switch
    {
      SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
      ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count > 0
        => paren.ParameterList.Parameters[0].Identifier.Text,
      _ => null
    };

    if (parameterName is null)
      return baseOptions;

    // Extract assignments from the lambda body
    return lambda.Body switch
    {
      BlockSyntax block => ExtractFromBlock(block, baseOptions, parameterName),
      ExpressionSyntax expression => ExtractFromExpression(expression, baseOptions, parameterName),
      _ => baseOptions
    };
  }

  /// <summary>
  /// Extracts options from a block body.
  /// </summary>
  private static ReplModel ExtractFromBlock
  (
    BlockSyntax block,
    ReplModel baseOptions,
    string parameterName
  )
  {
    bool autoStartWhenEmpty = baseOptions.AutoStartWhenEmpty;

    foreach (StatementSyntax statement in block.Statements)
    {
      if (statement is not ExpressionStatementSyntax exprStmt)
        continue;

      if (exprStmt.Expression is not AssignmentExpressionSyntax assignment)
        continue;

      // Check if this is an assignment to the parameter (e.g., options.AutoStartWhenEmpty = true)
      if (!IsParameterPropertyAssignment(assignment.Left, parameterName, out string? propertyName))
        continue;

      // Extract the value
      object? value = ExtractConstantValue(assignment.Right);
      if (value is null)
        continue;

      // Update the appropriate property
      switch (propertyName)
      {
        case "AutoStartWhenEmpty" when value is bool boolValue:
          autoStartWhenEmpty = boolValue;
          break;
      }
    }

    return baseOptions with
    {
      AutoStartWhenEmpty = autoStartWhenEmpty
    };
  }

  /// <summary>
  /// Extracts options from an expression body.
  /// </summary>
  private static ReplModel ExtractFromExpression
  (
    ExpressionSyntax expression,
    ReplModel baseOptions,
    string parameterName
  )
  {
    bool autoStartWhenEmpty = baseOptions.AutoStartWhenEmpty;

    // Handle single assignment: options => options.AutoStartWhenEmpty = true
    if (expression is AssignmentExpressionSyntax assignment)
    {
      if (IsParameterPropertyAssignment(assignment.Left, parameterName, out string? propertyName))
      {
        object? value = ExtractConstantValue(assignment.Right);
        if (value is not null)
        {
          switch (propertyName)
          {
            case "AutoStartWhenEmpty" when value is bool boolValue:
              autoStartWhenEmpty = boolValue;
              break;
          }
        }
      }
    }

    return baseOptions with
    {
      AutoStartWhenEmpty = autoStartWhenEmpty
    };
  }

  /// <summary>
  /// Checks if an expression is a property access on the lambda parameter.
  /// e.g., options.AutoStartWhenEmpty where "options" is the parameter name.
  /// </summary>
  private static bool IsParameterPropertyAssignment
  (
    ExpressionSyntax expression,
    string parameterName,
    out string? propertyName
  )
  {
    propertyName = null;

    if (expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    // Check if the expression is accessing a property on the parameter
    if (memberAccess.Expression is not IdentifierNameSyntax identifier)
      return false;

    if (identifier.Identifier.Text != parameterName)
      return false;

    propertyName = memberAccess.Name.Identifier.Text;
    return true;
  }

  /// <summary>
  /// Extracts a constant value from an expression.
  /// Supports: true, false.
  /// </summary>
  private static object? ExtractConstantValue(ExpressionSyntax expression)
  {
    return expression switch
    {
      LiteralExpressionSyntax literal => literal.Token.Value,
      _ => null
    };
  }
}
