#region Purpose
// Compile-time warning reporting for unintercepted RunAsync and logger configuration (NuruGenerator partial).
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

public sealed partial class NuruGenerator : IIncrementalGenerator
{
  /// <summary>
  /// Reports NURU_I001 for each NuruApp.RunAsync() call that no app's interceptor covers, so the
  /// "RunAsync was not intercepted" failure shows at compile time instead of at runtime (task 547).
  /// </summary>
  /// <remarks>
  /// A call in the same <see cref="RunAsyncLocator.EntryPointScope"/> as a generator error is not
  /// reported: that error is why its app has no interceptor. A call elsewhere is still reported, even
  /// when another app in the compilation failed.
  /// </remarks>
  private static void ReportRunAsyncNotIntercepted
  (
    SourceProductionContext ctx,
    ImmutableArray<RunAsyncCall> calls,
    GeneratorModelWithDiagnostics? modelWithDiags
  )
  {
    if (calls.IsEmpty)
      return;

    HashSet<Location> errorScopes = [];
    foreach (Diagnostic diagnostic in modelWithDiags?.Diagnostics ?? [])
    {
      if (diagnostic.Severity != DiagnosticSeverity.Error || diagnostic.Location.SourceTree is not SyntaxTree tree)
        continue;

      RoslynSyntaxNode node = tree.GetRoot(ctx.CancellationToken).FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
      errorScopes.Add(RunAsyncLocator.EntryPointScope(node).GetLocation());
    }

    HashSet<InterceptSiteModel> interceptedSites = [];
    if (modelWithDiags?.Model is { } model)
    {
      foreach (AppModel app in model.Apps)
      {
        if (app.InterceptSitesByMethod.TryGetSites("RunAsync", out EquatableArray<InterceptSiteModel> sites))
          interceptedSites.UnionWith(sites);
      }
    }

    foreach (RunAsyncCall call in calls)
    {
      if (call.Site is not null && interceptedSites.Contains(call.Site))
        continue;

      if (errorScopes.Contains(call.Scope))
        continue;

      ctx.ReportDiagnostic(Diagnostic.Create(
        DiagnosticDescriptors.RunAsyncNotIntercepted,
        call.Location,
        call.ReceiverText));
    }
  }

  /// <summary>
  /// Reports NURU_H007 warnings when ILogger&lt;T&gt; is injected but neither AddLogging() nor
  /// telemetry logging is configured. Each logger category warns once.
  /// </summary>
  private static void ReportLoggerWithoutConfigurationWarnings(SourceProductionContext ctx, GeneratorModel model)
  {
    // Logging is configured by AddLogging or by telemetry logging, the same two sources
    // InterceptorEmitter picks the logger factory from.
    bool hasLoggingConfigured = model.Apps.Any(a =>
      a.HasLogging || (a.HasTelemetry && (a.TelemetryOptions?.EnableLogging ?? true)));
    if (hasLoggingConfigured)
      return; // No warning needed - logging is configured

    // Behavior constructors, delegate/method handler parameters (task 541), endpoint handler
    // constructors and registered service constructors (task 545) all get a NullLogger without
    // logging, so all of them warn.
    IEnumerable<string> loggerSiteTypes = model.AllBehaviors
      .SelectMany(behavior => behavior.ConstructorDependencies)
      .Concat(model.AllRoutes.SelectMany(route =>
        route.Handler.ServiceParameters.Concat(route.Handler.ConstructorDependencies)))
      .Select(dep => dep.ParameterTypeName)
      .Concat(model.AllServices.SelectMany(service =>
        service.ConstructorParameters.Select(parameter => parameter.TypeName)
          .Concat(service.ConstructorDependencyTypes)));

    // A route can appear both in an app's routes and in the endpoint list
    HashSet<string> reportedCategories = new(StringComparer.Ordinal);
    foreach (string typeName in loggerSiteTypes)
    {
      if (!LoggerExpressions.IsLoggingServiceType(typeName)
        || LoggerExpressions.IsLoggerFactoryType(typeName))
      {
        continue;
      }

      string category = LoggerExpressions.GetCategoryTypeName(typeName);
      if (!reportedCategories.Add(category))
        continue;

      ctx.ReportDiagnostic(Diagnostic.Create(
        DiagnosticDescriptors.LoggerInjectedWithoutConfiguration,
        Location.None, // No specific location available from the model
        category));
    }
  }
}
