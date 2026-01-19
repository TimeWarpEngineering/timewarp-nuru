namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Default route that shows usage when no arguments provided.
/// This is a Query (Q) - read-only, just displays information.
/// Demonstrates ITerminal injection for testable output.
/// </summary>
[NuruRoute("", Description = "Show usage information")]
public sealed class DefaultQuery : IQuery<Unit>
{
  [Option("verbose", "v", Description = "Show verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IQueryHandler<DefaultQuery, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(DefaultQuery query, CancellationToken ct)
    {
      Terminal.WriteLine("Attributed Routes Sample");
      Terminal.WriteLine("========================");
      Terminal.WriteLine();
      Terminal.WriteLine("Queries (Q) - Read-only, safe to retry:");
      Terminal.WriteLine("  greet <name>              Greet someone");
      Terminal.WriteLine("  config get <key>          Get config value");
      Terminal.WriteLine("  docker ps                 List containers");
      Terminal.WriteLine();
      Terminal.WriteLine("Commands (C) - Mutating, needs confirmation:");
      Terminal.WriteLine("  deploy <env> [options]    Deploy to environment");
      Terminal.WriteLine("  goodbye, bye, cya         Say goodbye and exit");
      Terminal.WriteLine("  exec <args...>            Execute a command");
      Terminal.WriteLine("  docker run <image>        Run a container");
      Terminal.WriteLine("  docker build <path>       Build an image");
      Terminal.WriteLine();
      Terminal.WriteLine("Idempotent (I) - Mutating but safe to retry:");
      Terminal.WriteLine("  config set <key> <value>  Set config value");
      Terminal.WriteLine("  docker tag <image> <tag>  Tag an image");
      Terminal.WriteLine();
      Terminal.WriteLine("Unspecified ( ) - Not yet classified:");
      Terminal.WriteLine("  ping                      Simple health check");
      Terminal.WriteLine();
      if (query.Verbose)
      {
        Terminal.WriteLine("Legend: (Q)uery (I)dempotent (C)ommand ( )Unspecified");
        Terminal.WriteLine();
        Terminal.WriteLine("Run 'attributed-routes --help' for detailed help.");
      }
      return default;
    }
  }
}
