// ═══════════════════════════════════════════════════════════════════════════════
// VERSION QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Version endpoint - demonstrates literal route returning a value.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("version", Description = "Show version information")]
public sealed class VersionQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<VersionQuery, string>
  {
    public ValueTask<string> Handle(VersionQuery query, CancellationToken ct)
    {
      return new ValueTask<string>("1.0.0");
    }
  }
}
