// Extracts HelpOptions configuration from ConfigureHelp() lambda expressions.
//
// Handles:
// - .ConfigureHelp(options => { options.ShowPerCommandHelpRoutes = true; })
// - .ConfigureHelp(options => options.ShowCompletionRoutes = true)
//
// Extracts:
// - ShowPerCommandHelpRoutes (bool)
// - ShowReplCommandsInCli (bool)
// - ShowCompletionRoutes (bool)
// - ExcludePatterns (collection of string literals)

namespace TimeWarp.Nuru.Generators;

#region Purpose
// Pull HelpOptions assignments out of ConfigureHelp lambdas into HelpModel.
#endregion

/// <summary>
/// Extracts HelpOptions configuration from ConfigureHelp() lambda expressions.
/// </summary>
internal static class HelpOptionsExtractor
{
  /// <summary>
  /// Extracts HelpOptions configuration from a ConfigureHelp() invocation.
  /// </summary>
  /// <returns>Configured HelpModel, or null if extraction fails.</returns>
  public static HelpModel? Extract
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    _ = semanticModel;
    _ = cancellationToken;

    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    HelpModel options = HelpModel.Default;

    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractFromLambda(lambda, options);
    }

    return null;
  }

  private static HelpModel ExtractFromLambda
  (
    LambdaExpressionSyntax lambda,
    HelpModel baseOptions
  )
  {
    string? parameterName = lambda switch
    {
      SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
      ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count > 0
        => paren.ParameterList.Parameters[0].Identifier.Text,
      _ => null
    };

    if (parameterName is null)
      return baseOptions;

    return lambda.Body switch
    {
      BlockSyntax block => ExtractFromStatements(block.Statements, baseOptions, parameterName),
      ExpressionSyntax expression => ExtractFromExpression(expression, baseOptions, parameterName),
      _ => baseOptions
    };
  }

  private static HelpModel ExtractFromStatements
  (
    SyntaxList<StatementSyntax> statements,
    HelpModel baseOptions,
    string parameterName
  )
  {
    bool showPerCommandHelpRoutes = baseOptions.ShowPerCommandHelpRoutes;
    bool showReplCommandsInCli = baseOptions.ShowReplCommandsInCli;
    bool showCompletionRoutes = baseOptions.ShowCompletionRoutes;
    EquatableArray<string> excludePatterns = baseOptions.ExcludePatterns;

    foreach (StatementSyntax statement in statements)
    {
      if (statement is not ExpressionStatementSyntax exprStmt)
        continue;

      if (exprStmt.Expression is not AssignmentExpressionSyntax assignment)
        continue;

      ApplyAssignment(
        assignment,
        parameterName,
        ref showPerCommandHelpRoutes,
        ref showReplCommandsInCli,
        ref showCompletionRoutes,
        ref excludePatterns);
    }

    return baseOptions with
    {
      ShowPerCommandHelpRoutes = showPerCommandHelpRoutes,
      ShowReplCommandsInCli = showReplCommandsInCli,
      ShowCompletionRoutes = showCompletionRoutes,
      ExcludePatterns = excludePatterns
    };
  }

  private static HelpModel ExtractFromExpression
  (
    ExpressionSyntax expression,
    HelpModel baseOptions,
    string parameterName
  )
  {
    bool showPerCommandHelpRoutes = baseOptions.ShowPerCommandHelpRoutes;
    bool showReplCommandsInCli = baseOptions.ShowReplCommandsInCli;
    bool showCompletionRoutes = baseOptions.ShowCompletionRoutes;
    EquatableArray<string> excludePatterns = baseOptions.ExcludePatterns;

    if (expression is AssignmentExpressionSyntax assignment)
    {
      ApplyAssignment(
        assignment,
        parameterName,
        ref showPerCommandHelpRoutes,
        ref showReplCommandsInCli,
        ref showCompletionRoutes,
        ref excludePatterns);
    }

    return baseOptions with
    {
      ShowPerCommandHelpRoutes = showPerCommandHelpRoutes,
      ShowReplCommandsInCli = showReplCommandsInCli,
      ShowCompletionRoutes = showCompletionRoutes,
      ExcludePatterns = excludePatterns
    };
  }

  private static void ApplyAssignment
  (
    AssignmentExpressionSyntax assignment,
    string parameterName,
    ref bool showPerCommandHelpRoutes,
    ref bool showReplCommandsInCli,
    ref bool showCompletionRoutes,
    ref EquatableArray<string> excludePatterns
  )
  {
    if (!IsParameterPropertyAssignment(assignment.Left, parameterName, out string? propertyName))
      return;

    switch (propertyName)
    {
      case "ShowPerCommandHelpRoutes" when ExtractBool(assignment.Right) is bool boolValue:
        showPerCommandHelpRoutes = boolValue;
        break;
      case "ShowReplCommandsInCli" when ExtractBool(assignment.Right) is bool boolValue:
        showReplCommandsInCli = boolValue;
        break;
      case "ShowCompletionRoutes" when ExtractBool(assignment.Right) is bool boolValue:
        showCompletionRoutes = boolValue;
        break;
      case "ExcludePatterns":
        EquatableArray<string>? patterns = ExtractStringCollection(assignment.Right);
        if (patterns is not null)
        {
          excludePatterns = patterns.Value;
        }

        break;
    }
  }

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

    if (memberAccess.Expression is not IdentifierNameSyntax identifier)
      return false;

    if (identifier.Identifier.Text != parameterName)
      return false;

    propertyName = memberAccess.Name.Identifier.Text;
    return true;
  }

  private static bool? ExtractBool(ExpressionSyntax expression)
  {
    return expression is LiteralExpressionSyntax literal && literal.Token.Value is bool boolValue
      ? boolValue
      : null;
  }

  private static EquatableArray<string>? ExtractStringCollection(ExpressionSyntax expression)
  {
    List<string> patterns = [];

    switch (expression)
    {
      case CollectionExpressionSyntax collection:
        foreach (CollectionElementSyntax element in collection.Elements)
        {
          if (element is ExpressionElementSyntax expressionElement
            && TryExtractString(expressionElement.Expression, out string collectionValue))
          {
            patterns.Add(collectionValue);
          }
        }

        break;

      case ImplicitArrayCreationExpressionSyntax implicitArray when implicitArray.Initializer is not null:
        AddFromInitializer(implicitArray.Initializer, patterns);
        break;

      case ArrayCreationExpressionSyntax arrayCreation when arrayCreation.Initializer is not null:
        AddFromInitializer(arrayCreation.Initializer, patterns);
        break;

      case ObjectCreationExpressionSyntax objectCreation when objectCreation.Initializer is not null:
        AddFromInitializer(objectCreation.Initializer, patterns);
        break;

      default:
        return null;
    }

    return [.. patterns];
  }

  private static void AddFromInitializer(InitializerExpressionSyntax initializer, List<string> patterns)
  {
    foreach (ExpressionSyntax expression in initializer.Expressions)
    {
      if (TryExtractString(expression, out string value))
      {
        patterns.Add(value);
      }
    }
  }

  private static bool TryExtractString(ExpressionSyntax expression, out string value)
  {
    if (expression is LiteralExpressionSyntax literal && literal.Token.Value is string stringValue)
    {
      value = stringValue;
      return true;
    }

    value = "";
    return false;
  }
}
