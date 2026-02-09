// ═══════════════════════════════════════════════════════════════════════════════
// LIST QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// List items (public, no auth required).

namespace PipelineFilteredAuth.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("list", Description = "List items (public, no auth required)")]
public sealed class ListQuery : IQuery<string[]>
{
  public sealed class Handler : IQueryHandler<ListQuery, string[]>
  {
    public ValueTask<string[]> Handle(ListQuery query, CancellationToken ct)
    {
      return new ValueTask<string[]>(["Item 1", "Item 2", "Item 3"]);
    }
  }
}
