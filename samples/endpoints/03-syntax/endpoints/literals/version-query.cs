// ═══════════════════════════════════════════════════════════════════════════════
// VERSION QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Version endpoint - demonstrates literal route returning a value.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("version", Description = "Show version information")]
public sealed class VersionQuery : IQuery<string>
{
  public sealed class Handler : IRequestHandler<VersionQuery, string>
  {
    public ValueTask<string> Handle(VersionQuery query, CancellationToken ct)
    {
      return ValueTask.FromResult<string>("1.0.0");
    }
  }
}
