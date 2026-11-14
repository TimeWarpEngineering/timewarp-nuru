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
// resolved at run time (AOT and trim safe). Emitting a non-constant expression is not an option:
// it can name locals and members the generated code cannot reach.
// A value the extractor cannot read is skipped, but never in silence (task 533): an assignment to
// a known option, or an Add argument, that is not a constant reports NURU_T001 at the value, and a
// configure argument that is not a lambda reports NURU_T002. Statements of other shapes (an if,
// a local declaration) are still skipped without a diagnostic. ActivitySources / Meters ignore
// empty names and repeats.
#endregion

/// <summary>
/// Extracts NuruTelemetryOptions configuration from UseTelemetry() lambda expressions.
/// </summary>
internal static class TelemetryOptionsExtractor
{
  /// <summary>
  /// Extracts telemetry configuration from a UseTelemetry() invocation.
  /// </summary>
  /// <param name="invocation">The UseTelemetry(...) invocation.</param>
  /// <param name="semanticModel">Semantic model of the invocation's tree.</param>
  /// <param name="diagnostics">Receives NURU_T001 / NURU_T002 for values the extractor cannot read.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Configured TelemetryModel, or null if extraction fails.</returns>
  public static TelemetryModel? Extract
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    ICollection<Diagnostic> diagnostics,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractFromLambda(lambda, TelemetryModel.Default, semanticModel, diagnostics, cancellationToken);
    }

    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TelemetryConfigureNotLambda, configureExpression.GetLocation()));
    return null;
  }

  private static TelemetryModel ExtractFromLambda
  (
    LambdaExpressionSyntax lambda,
    TelemetryModel baseOptions,
    SemanticModel semanticModel,
    ICollection<Diagnostic> diagnostics,
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
            options = Apply(exprStmt.Expression, options, parameterName, semanticModel, diagnostics, cancellationToken);
        }

        break;
      case ExpressionSyntax expression:
        options = Apply(expression, options, parameterName, semanticModel, diagnostics, cancellationToken);
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
    ICollection<Diagnostic> diagnostics,
    CancellationToken cancellationToken
  )
  {
    return expression switch
    {
      AssignmentExpressionSyntax assignment
        => ApplyAssignment(assignment, options, parameterName, semanticModel, diagnostics, cancellationToken),
      InvocationExpressionSyntax invocation
        => ApplyAdd(invocation, options, parameterName, semanticModel, diagnostics, cancellationToken),
      _ => options
    };
  }

  private static TelemetryModel ApplyAssignment
  (
    AssignmentExpressionSyntax assignment,
    TelemetryModel options,
    string parameterName,
    SemanticModel semanticModel,
    ICollection<Diagnostic> diagnostics,
    CancellationToken cancellationToken
  )
  {
    if (!IsParameterProperty(assignment.Left, parameterName, out string? propertyName))
      return options;

    string? fallback = AssignmentFallback(propertyName);
    if (fallback is null)
      return options;

    Optional<object?> constant = semanticModel.GetConstantValue(assignment.Right, cancellationToken);
    if (!constant.HasValue)
    {
      ReportNotConstant(diagnostics, assignment.Right, propertyName!, fallback);
      return options;
    }

    return (propertyName, constant.Value) switch
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
  /// What applies when the generator cannot read an option, for the NURU_T001 message; null for a
  /// property the generator does not read at all.
  /// </summary>
  private static string? AssignmentFallback(string? propertyName) => propertyName switch
  {
    "OtlpEndpoint" => "The app reads OTEL_EXPORTER_OTLP_ENDPOINT instead",
    "ServiceName" => "The default service name (OTEL_SERVICE_NAME, then the assembly name) applies",
    "ServiceVersion" => "The default service version (OTEL_SERVICE_VERSION, then the assembly version) applies",
    "EnableTracing" or "EnableMetrics" or "EnableLogging" => "The default (true) applies",
    _ => null
  };

  private static void ReportNotConstant
  (
    ICollection<Diagnostic> diagnostics,
    ExpressionSyntax value,
    string optionName,
    string fallback
  ) =>
    diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.TelemetryOptionNotConstant, value.GetLocation(), optionName, fallback));

  /// <summary>
  /// Applies <c>options.ActivitySources.Add("name")</c> or <c>options.Meters.Add("name")</c>.
  /// </summary>
  private static TelemetryModel ApplyAdd
  (
    InvocationExpressionSyntax invocation,
    TelemetryModel options,
    string parameterName,
    SemanticModel semanticModel,
    ICollection<Diagnostic> diagnostics,
    CancellationToken cancellationToken
  )
  {
    if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "Add" } addAccess)
      return options;

    if (!IsParameterProperty(addAccess.Expression, parameterName, out string? propertyName))
      return options;

    if (propertyName is not ("ActivitySources" or "Meters"))
      return options;

    if (invocation.ArgumentList.Arguments.Count != 1)
      return options;

    ExpressionSyntax argument = invocation.ArgumentList.Arguments[0].Expression;
    Optional<object?> constant = semanticModel.GetConstantValue(argument, cancellationToken);
    if (!constant.HasValue)
    {
      ReportNotConstant(diagnostics, argument, $"{propertyName}.Add", "The name is not added");
      return options;
    }

    if (constant.Value is not string name || string.IsNullOrWhiteSpace(name))
      return options;

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
