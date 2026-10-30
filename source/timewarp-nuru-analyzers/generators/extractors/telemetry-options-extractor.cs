// Extracts NuruTelemetryOptions configuration from UseTelemetry() lambda expressions.
//
// Handles:
// - .UseTelemetry(options => { options.ServiceName = "my-cli"; })
// - .UseTelemetry(options => options.EnableTracing = false)
// - .UseTelemetry(options => options.ActivitySources.Add("TimeWarp.Taratibu"))
//
// Extracts:
// - ServiceName (constant string)
// - ServiceVersion (constant string)
// - EnableTracing / EnableMetrics / EnableLogging (bool)
// - OtlpEndpoint (constant string)
// - ActivitySources.Add / Meters.Add (constant string, task 561)

namespace TimeWarp.Nuru.Generators;

#region Purpose
// Pull NuruTelemetryOptions assignments out of UseTelemetry lambdas into TelemetryModel.
#endregion

#region Design
// Values must be compile-time constants (a literal or a const), read through
// SemanticModel.GetConstantValue. The generated code carries them as literals, so nothing is
// resolved at run time (AOT and trim safe). A statement the extractor cannot read is skipped.
// ActivitySources / Meters ignore empty names and repeats.
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
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractFromLambda(lambda, TelemetryModel.Default, semanticModel, cancellationToken);
    }

    return null;
  }

  private static TelemetryModel ExtractFromLambda
  (
    LambdaExpressionSyntax lambda,
    TelemetryModel baseOptions,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
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

    TelemetryModel options = baseOptions;
    switch (lambda.Body)
    {
      case BlockSyntax block:
        foreach (StatementSyntax statement in block.Statements)
        {
          if (statement is ExpressionStatementSyntax exprStmt)
            options = Apply(exprStmt.Expression, options, parameterName, semanticModel, cancellationToken);
        }

        break;
      case ExpressionSyntax expression:
        options = Apply(expression, options, parameterName, semanticModel, cancellationToken);
        break;
    }

    return options;
  }

  private static TelemetryModel Apply
  (
    ExpressionSyntax expression,
    TelemetryModel options,
    string parameterName,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    return expression switch
    {
      AssignmentExpressionSyntax assignment
        => ApplyAssignment(assignment, options, parameterName, semanticModel, cancellationToken),
      InvocationExpressionSyntax invocation
        => ApplyAdd(invocation, options, parameterName, semanticModel, cancellationToken),
      _ => options
    };
  }

  private static TelemetryModel ApplyAssignment
  (
    AssignmentExpressionSyntax assignment,
    TelemetryModel options,
    string parameterName,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    if (!IsParameterProperty(assignment.Left, parameterName, out string? propertyName))
      return options;

    object? value = semanticModel.GetConstantValue(assignment.Right, cancellationToken) is { HasValue: true } constant
      ? constant.Value
      : null;

    return (propertyName, value) switch
    {
      ("ServiceName", string name) => options with { ServiceName = name },
      ("ServiceVersion", string version) => options with { ServiceVersion = version },
      ("EnableTracing", bool tracing) => options with { EnableTracing = tracing },
      ("EnableMetrics", bool metrics) => options with { EnableMetrics = metrics },
      ("EnableLogging", bool logging) => options with { EnableLogging = logging },
      ("OtlpEndpoint", string endpoint) => options with { OtlpEndpoint = endpoint },
      _ => options
    };
  }

  /// <summary>
  /// Applies <c>options.ActivitySources.Add("name")</c> or <c>options.Meters.Add("name")</c>.
  /// </summary>
  private static TelemetryModel ApplyAdd
  (
    InvocationExpressionSyntax invocation,
    TelemetryModel options,
    string parameterName,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Add" } addAccess)
      return options;

    if (!IsParameterProperty(addAccess.Expression, parameterName, out string? propertyName))
      return options;

    if (invocation.ArgumentList.Arguments.Count != 1)
      return options;

    if (semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression, cancellationToken)
      is not { HasValue: true, Value: string name } || string.IsNullOrWhiteSpace(name))
    {
      return options;
    }

    return propertyName switch
    {
      "ActivitySources" => options with { ActivitySources = AddDistinct(options.ActivitySources, name) },
      "Meters" => options with { Meters = AddDistinct(options.Meters, name) },
      _ => options
    };
  }

  private static EquatableArray<string> AddDistinct(EquatableArray<string> names, string name) =>
    names.Contains(name, StringComparer.Ordinal) ? names : names.AddRange([name]);

  private static bool IsParameterProperty
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
}
