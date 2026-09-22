// Extracts NuruTelemetryOptions configuration from UseTelemetry() lambda expressions.
//
// Handles:
// - .UseTelemetry(options => { options.ServiceName = "my-cli"; })
// - .UseTelemetry(options => options.EnableTracing = false)
//
// Extracts:
// - ServiceName (string literal)
// - ServiceVersion (string literal)
// - EnableTracing / EnableMetrics / EnableLogging (bool)
// - OtlpEndpoint (string literal)

namespace TimeWarp.Nuru.Generators;

#region Purpose
// Pull NuruTelemetryOptions assignments out of UseTelemetry lambdas into TelemetryModel.
#endregion

/// <summary>
/// Extracts NuruTelemetryOptions configuration from UseTelemetry() lambda expressions.
/// </summary>
internal static class TelemetryOptionsExtractor
{
  /// <summary>
  /// Extracts telemetry configuration from a UseTelemetry() invocation.
  /// </summary>
  /// <returns>Configured TelemetryModel, or null if extraction fails.</returns>
  public static TelemetryModel? Extract
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

    TelemetryModel options = TelemetryModel.Default;

    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractFromLambda(lambda, options);
    }

    return null;
  }

  private static TelemetryModel ExtractFromLambda
  (
    LambdaExpressionSyntax lambda,
    TelemetryModel baseOptions
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

  private static TelemetryModel ExtractFromStatements
  (
    SyntaxList<StatementSyntax> statements,
    TelemetryModel baseOptions,
    string parameterName
  )
  {
    string? serviceName = baseOptions.ServiceName;
    string? serviceVersion = baseOptions.ServiceVersion;
    bool enableTracing = baseOptions.EnableTracing;
    bool enableMetrics = baseOptions.EnableMetrics;
    bool enableLogging = baseOptions.EnableLogging;
    string? otlpEndpoint = baseOptions.OtlpEndpoint;

    foreach (StatementSyntax statement in statements)
    {
      if (statement is not ExpressionStatementSyntax exprStmt)
        continue;

      if (exprStmt.Expression is not AssignmentExpressionSyntax assignment)
        continue;

      ApplyAssignment(
        assignment,
        parameterName,
        ref serviceName,
        ref serviceVersion,
        ref enableTracing,
        ref enableMetrics,
        ref enableLogging,
        ref otlpEndpoint);
    }

    return baseOptions with
    {
      ServiceName = serviceName,
      ServiceVersion = serviceVersion,
      EnableTracing = enableTracing,
      EnableMetrics = enableMetrics,
      EnableLogging = enableLogging,
      OtlpEndpoint = otlpEndpoint
    };
  }

  private static TelemetryModel ExtractFromExpression
  (
    ExpressionSyntax expression,
    TelemetryModel baseOptions,
    string parameterName
  )
  {
    string? serviceName = baseOptions.ServiceName;
    string? serviceVersion = baseOptions.ServiceVersion;
    bool enableTracing = baseOptions.EnableTracing;
    bool enableMetrics = baseOptions.EnableMetrics;
    bool enableLogging = baseOptions.EnableLogging;
    string? otlpEndpoint = baseOptions.OtlpEndpoint;

    if (expression is AssignmentExpressionSyntax assignment)
    {
      ApplyAssignment(
        assignment,
        parameterName,
        ref serviceName,
        ref serviceVersion,
        ref enableTracing,
        ref enableMetrics,
        ref enableLogging,
        ref otlpEndpoint);
    }

    return baseOptions with
    {
      ServiceName = serviceName,
      ServiceVersion = serviceVersion,
      EnableTracing = enableTracing,
      EnableMetrics = enableMetrics,
      EnableLogging = enableLogging,
      OtlpEndpoint = otlpEndpoint
    };
  }

  private static void ApplyAssignment
  (
    AssignmentExpressionSyntax assignment,
    string parameterName,
    ref string? serviceName,
    ref string? serviceVersion,
    ref bool enableTracing,
    ref bool enableMetrics,
    ref bool enableLogging,
    ref string? otlpEndpoint
  )
  {
    if (!IsParameterPropertyAssignment(assignment.Left, parameterName, out string? propertyName))
      return;

    switch (propertyName)
    {
      case "ServiceName" when TryExtractString(assignment.Right, out string name):
        serviceName = name;
        break;
      case "ServiceVersion" when TryExtractString(assignment.Right, out string version):
        serviceVersion = version;
        break;
      case "EnableTracing" when ExtractBool(assignment.Right) is bool tracing:
        enableTracing = tracing;
        break;
      case "EnableMetrics" when ExtractBool(assignment.Right) is bool metrics:
        enableMetrics = metrics;
        break;
      case "EnableLogging" when ExtractBool(assignment.Right) is bool logging:
        enableLogging = logging;
        break;
      case "OtlpEndpoint" when TryExtractString(assignment.Right, out string endpoint):
        otlpEndpoint = endpoint;
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
