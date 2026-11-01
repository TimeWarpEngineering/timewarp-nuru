// ═══════════════════════════════════════════════════════════════════════════════
// LIST QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// List items (public, no auth required).

namespace PipelineFilteredAuth.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("list", Description = "List items (public, no auth required)")]
public sealed class ListQuery : IQuery<string[]>
{
  public sealed class Handler : IRequestHandler<ListQuery, string[]>
  {
    public ValueTask<string[]> Handle(ListQuery query, CancellationToken ct)
    {
      return ValueTask.FromResult<string[]>(["Item 1", "Item 2", "Item 3"]);
    }
  }
}
