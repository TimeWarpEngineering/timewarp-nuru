namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Default route that shows usage when no arguments provided.
/// This is a Query (Q) - read-only, just displays information.
/// </summary>
[NuruRoute("", Description = "Show usage information")]
public sealed class DefaultQuery : IQuery<Unit>
{
  [Option("verbose", "v", Description = "Show verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IQueryHandler<DefaultQuery, Unit>
  {
    public ValueTask<Unit> Handle(DefaultQuery query, CancellationToken ct)
    {
      WriteLine("Attributed Routes Sample");
      WriteLine("========================");
      WriteLine();
      WriteLine("Queries (Q) - Read-only, safe to retry:");
      WriteLine("  greet <name>              Greet someone");
      WriteLine("  config get <key>          Get config value");
      WriteLine("  docker ps                 List containers");
      WriteLine();
      WriteLine("Commands (C) - Mutating, needs confirmation:");
      WriteLine("  deploy <env> [options]    Deploy to environment");
      WriteLine("  goodbye, bye, cya         Say goodbye and exit");
      WriteLine("  exec <args...>            Execute a command");
      WriteLine("  docker run <image>        Run a container");
      WriteLine("  docker build <path>       Build an image");
      WriteLine();
      WriteLine("Idempotent (I) - Mutating but safe to retry:");
      WriteLine("  config set <key> <value>  Set config value");
      WriteLine("  docker tag <image> <tag>  Tag an image");
      WriteLine();
      WriteLine("Unspecified ( ) - Not yet classified:");
      WriteLine("  ping                      Simple health check");
      WriteLine();
      if (query.Verbose)
      {
        WriteLine("Legend: (Q)uery (I)dempotent (C)ommand ( )Unspecified");
        WriteLine();
        WriteLine("Run 'attributed-routes --help' for detailed help.");
      }
      return default;
    }
  }
}
