namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Default route that shows usage when no arguments provided.
/// This is a Query (Q) - read-only, just displays information.
/// </summary>
[NuruRoute("", Description = "Show usage information")]
public sealed class DefaultRequest : IQuery<Unit>
{
  [Option("verbose", "v", Description = "Show verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IQueryHandler<DefaultRequest, Unit>
  {
    public ValueTask<Unit> Handle(DefaultRequest request, CancellationToken ct)
    {
      WriteLine("Attributed Routes Sample");
      WriteLine("========================");
      WriteLine();
      WriteLine("Commands:");
      WriteLine("  greet <name>              (Q) Greet someone");
      WriteLine("  deploy <env> [options]    (C) Deploy to environment");
      WriteLine("  config get <key>          (Q) Get config value");
      WriteLine("  config set <key> <value>  (I) Set config value");
      WriteLine("  goodbye, bye, cya         (C) Say goodbye and exit");
      WriteLine();
      WriteLine("Docker Commands:");
      WriteLine("  docker run <image>        (C) Run a container");
      WriteLine("  docker build <path>       (C) Build an image");
      WriteLine();
      WriteLine("Legend: (Q)uery (I)dempotent (C)ommand");
      WriteLine();
      if (request.Verbose)
      {
        WriteLine("Run 'attributed-routes --help' for detailed help.");
      }
      return default;
    }
  }
}
