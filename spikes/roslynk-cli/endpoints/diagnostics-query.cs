#region Purpose
// Nuru route that calls Roslynk's get_diagnostics tool in-process.
#endregion

#region Design
// Option names match the tool's parameter names so one --json-args object binds without a rename.
// The route waits until the solution's first load finishes, because the tool method itself returns
// Indexing while that load is still in the background. A CLI command includes the load.
// includeAnalyzers is bool? because a non-nullable bool flag always starts false, so the tool's
// default of true would be lost. Omission stays null and the handler passes true.
#endregion

namespace RoslynkNuru.Endpoints;

using Morris.Roslynk.Features.Diagnostics.GetDiagnostics;
using Morris.Roslynk.Infrastructure.Lifecycle;
using RoslynkNuru.Services;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Compiler and analyzer diagnostics for one solution.
/// </summary>
[NuruRoute("diagnostics", Description = """
  SupportsPaging=Y

  Returns compiler/analyzer diagnostics.

  errors/warnings/infos/hidden count all content-filtered severities regardless of include flags.
  The body lists flagged severities; summaryOnly summarizes all if none are flagged, otherwise
  only flagged ones. Details: project > folder > file > severity; leaves: <id>,<line:col>,<message>.
  Summary: <id>,<count>,<project>,<path>,<line:col>,<message> per id, ordered by count descending then id.

  projectName/filePath/ids filter after compilation, reusing cached results; active filters echo
  filter=<parameter>:<value>. Empty filtered results do not prove the solution clean.
  Unknown project/file returns NotFound with candidates. Known matches without diagnostics return zeros.
  Private fixer-trigger IDs are omitted; fix the public rule instead (e.g. IDE0005).
  """)]
[NuruRouteExample("diagnostics ./app.slnx --includeErrors", Description = "Compiler errors for a solution")]
public sealed class DiagnosticsQuery : IQuery<string>
{
  [Parameter(Description = "Solution handle.")]
  public string SolutionId { get; set; } = string.Empty;

  [Option("includeErrors", Description = "List errors.")]
  public bool IncludeErrors { get; set; }

  [Option("includeWarnings", Description = "List warnings.")]
  public bool IncludeWarnings { get; set; }

  [Option("includeInfo", Description = "List info diagnostics.")]
  public bool IncludeInfo { get; set; }

  [Option("includeHidden", Description = "List hidden diagnostics.")]
  public bool IncludeHidden { get; set; }

  [Option("includeAnalyzers", Description = "Run analyzers; false is faster, compiler-only.")]
  public bool? IncludeAnalyzers { get; set; }

  [Option("projectName", Description = "Project filter: outline name with optional extension, case-insensitive; all loaded target frameworks.")]
  public string? ProjectName { get; set; }

  [Option("filePath", Description = "File filter (case-insensitive), including diagnostics mapped to Razor source.")]
  public string? FilePath { get; set; }

  [Option("ids", Description = """Diagnostic ids (case-insensitive), e.g. ["CS0246"]; null/empty matches all.""")]
  public string[]? Ids { get; set; }

  [Option("maxResults", Description = "Body-entry cap (ids with summaryOnly). Capped bodies report count=<listed>, truncated=Y.")]
  public int? MaxResults { get; set; }

  [Option("summaryOnly", Description = "Summarize by diagnostic id.")]
  public bool SummaryOnly { get; set; }

  [Option("startItem", Description = "First item (1-based).")]
  public int StartItem { get; set; } = 1;

  [Option("maxSize", Description = "Page character limit.")]
  public int MaxSize { get; set; }

  public sealed class Handler(GetDiagnosticsTool getDiagnosticsTool, InstanceRegistry instanceRegistry) : IQueryHandler<DiagnosticsQuery, string>
  {
    public async Task<string> Handle(DiagnosticsQuery query, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      string solutionId = Path.GetFullPath(query.SolutionId);
      string? fault = await SolutionLoad.FaultAfterReadyAsync(instanceRegistry, solutionId).ConfigureAwait(false);
      if (fault is not null)
      {
        return ToolResult.Finish($"error=Faulted\nerrorMessage={fault}");
      }

      string result = await getDiagnosticsTool.GetDiagnostics(
        solutionId,
        query.IncludeErrors,
        query.IncludeWarnings,
        query.IncludeInfo,
        query.IncludeHidden,
        query.IncludeAnalyzers ?? true,
        query.ProjectName,
        query.FilePath,
        query.Ids,
        query.MaxResults,
        query.SummaryOnly,
        query.StartItem,
        query.MaxSize).ConfigureAwait(false);
      return ToolResult.Finish(result);
    }
  }
}
