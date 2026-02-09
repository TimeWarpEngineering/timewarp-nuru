namespace Endpoints.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Default route that runs when no command is specified.
/// This demonstrates using [NuruRoute("")] as a fallback/welcome message.
/// For help, use the auto-generated --help flag instead.
/// </summary>
[NuruRoute("", Description = "Default action when no command provided")]
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
      Terminal.WriteLine("Welcome to the Endpoints Sample!");
      Terminal.WriteLine();
      Terminal.WriteLine("This is the default route - it runs when no command is specified.");
      Terminal.WriteLine();
      Terminal.WriteLine("Try these commands:");
      Terminal.WriteLine("  greet Alice          Say hello to someone");
      Terminal.WriteLine("  docker ps            List containers");
      Terminal.WriteLine("  --help               Show all available commands");
      Terminal.WriteLine();
      if (query.Verbose)
      {
        Terminal.WriteLine("Tip: Use --help for the complete auto-generated command list.");
      }
      return default;
    }
  }
}
