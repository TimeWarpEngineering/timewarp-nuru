// ═══════════════════════════════════════════════════════════════════════════════
// KUBECTL QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Kubectl get command with namespace and output format options.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("kubectl", Description = "Kubectl get command")]
public sealed class KubectlQuery : IQuery<Unit>
{
  [Parameter(Description = "Resource type")]
  public string Resource { get; set; } = string.Empty;

  [Option("namespace", "n", Description = "Target namespace")]
  public string? Ns { get; set; }

  [Option("output", "o", Description = "Output format")]
  public string? Format { get; set; }

  public sealed class Handler : IQueryHandler<KubectlQuery, Unit>
  {
    public ValueTask<Unit> Handle(KubectlQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Get {query.Resource} in namespace {query.Ns ?? "default"}");
      return default;
    }
  }
}
