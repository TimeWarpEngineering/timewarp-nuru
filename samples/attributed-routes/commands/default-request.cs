namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Default route that shows usage when no arguments provided.
/// </summary>
[NuruRoute("", Description = "Show usage information")]
public sealed class DefaultRequest : IRequest
{
  [Option("verbose", "v", Description = "Show verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : IRequestHandler<DefaultRequest>
  {
    public ValueTask<Unit> Handle(DefaultRequest request, CancellationToken ct)
    {
      WriteLine("Attributed Routes Sample");
      WriteLine("========================");
      WriteLine();
      WriteLine("Commands:");
      WriteLine("  greet <name>              Greet someone");
      WriteLine("  deploy <env> [options]    Deploy to environment");
      WriteLine("  goodbye, bye, cya         Say goodbye and exit");
      WriteLine();
      WriteLine("Docker Commands:");
      WriteLine("  docker run <image>        Run a container");
      WriteLine("  docker build <path>       Build an image");
      WriteLine();
      if (request.Verbose)
      {
        WriteLine("Run 'attributed-routes --help' for detailed help.");
      }
      return default;
    }
  }
}
