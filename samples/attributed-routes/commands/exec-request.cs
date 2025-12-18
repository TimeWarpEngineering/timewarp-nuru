namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Execute command with catch-all parameter for arbitrary arguments.
/// This is a Command (C) - executes arbitrary commands with side effects.
/// </summary>
[NuruRoute("exec", Description = "Execute a command with arguments")]
public sealed class ExecRequest : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Command and arguments to execute")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<ExecRequest, Unit>
  {
    public ValueTask<Unit> Handle(ExecRequest request, CancellationToken ct)
    {
      WriteLine($"Executing: {string.Join(" ", request.Args)}");
      return default;
    }
  }
}
