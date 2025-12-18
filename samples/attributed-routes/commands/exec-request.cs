namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Execute command with catch-all parameter for arbitrary arguments.
/// </summary>
[NuruRoute("exec", Description = "Execute a command with arguments")]
public sealed class ExecRequest : IRequest
{
  [Parameter(IsCatchAll = true, Description = "Command and arguments to execute")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : IRequestHandler<ExecRequest>
  {
    public ValueTask<Unit> Handle(ExecRequest request, CancellationToken ct)
    {
      WriteLine($"Executing: {string.Join(" ", request.Args)}");
      return default;
    }
  }
}
