namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Query: docker ps [--all]
/// Lists running Docker containers (or all containers with --all).
/// This is a Query (Q) - read-only, safe to retry.
/// </summary>
[NuruRoute("ps", Description = "List containers")]
public sealed class DockerPsQuery : DockerGroupBase, IQuery<Unit>
{
  [Option("all", "a", Description = "Show all containers (default shows just running)")]
  public bool All { get; set; }

  public sealed class Handler : IQueryHandler<DockerPsQuery, Unit>
  {
    public ValueTask<Unit> Handle(DockerPsQuery query, CancellationToken ct)
    {
      string scope = query.All ? "all" : "running";
      WriteLine($"Listing {scope} containers...");
      WriteLine("CONTAINER ID   IMAGE          STATUS");
      WriteLine("abc123         nginx:latest   Up 2 hours");
      if (query.All)
      {
        WriteLine("def456         redis:7        Exited (0) 1 day ago");
      }
      return default;
    }
  }
}
