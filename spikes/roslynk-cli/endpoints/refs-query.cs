#region Purpose
// Nuru route that calls Roslynk's find_references tool in-process.
#endregion

#region Design
// The route literal is refs, the name from the spike brief. Positional capability names are the
// property names lowercased (solutionid, symbolname). Option long forms keep the tool's names.
#endregion

namespace RoslynkNuru.Endpoints;

using Morris.Roslynk.Features.References.FindReferences;
using Morris.Roslynk.Infrastructure.Lifecycle;
using RoslynkNuru.Services;
using TimeWarp.Mediator;
using TimeWarp.Nuru;

/// <summary>
/// Semantic references for one symbol.
/// </summary>
[NuruRoute("refs", Description = """
  SupportsPaging=Y

  Finds semantic references, including partial classes/code-behind.
  Body: project > folder > file > namespace > type > member. Shared declarations appear once;
  type locations appear only when the declaration references the target; collapsed references
  use pipe-delimited loc lists.

  Combine with get_callers/get_callees/find_implementations/get_type_hierarchy via multi_query for
  usage/impact analysis.
  """)]
[NuruRouteExample("refs ./app.slnx TimeWarp.Nuru.NuruApp", Description = "References to a type")]
public sealed class RefsQuery : IQuery<string>
{
  [Parameter(Order = 0, Description = "Solution handle.")]
  public string SolutionId { get; set; } = string.Empty;

  [Parameter(Order = 1, Description = "Symbol name.")]
  public string SymbolName { get; set; } = string.Empty;

  [Option("maxResults", Description = "Reference-location cap.")]
  public int? MaxResults { get; set; }

  [Option("startItem", Description = "First item (1-based).")]
  public int StartItem { get; set; } = 1;

  [Option("maxSize", Description = "Page character limit.")]
  public int MaxSize { get; set; }

  public sealed class Handler(FindReferencesTool findReferencesTool, InstanceRegistry instanceRegistry) : IQueryHandler<RefsQuery, string>
  {
    public async Task<string> Handle(RefsQuery query, CancellationToken cancellationToken)
    {
      string solutionId = Path.GetFullPath(query.SolutionId);
      string? fault = await SolutionLoad.FaultAfterReadyAsync(instanceRegistry, solutionId).ConfigureAwait(false);
      if (fault is not null)
      {
        return ToolResult.Finish($"error=Faulted\nerrorMessage={fault}");
      }

      string result = await findReferencesTool.FindReferences(
        solutionId,
        query.SymbolName,
        query.MaxResults,
        query.StartItem,
        query.MaxSize,
        cancellationToken).ConfigureAwait(false);
      return ToolResult.Finish(result);
    }
  }
}
