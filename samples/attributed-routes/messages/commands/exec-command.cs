namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Execute a command with arbitrary arguments.
/// This is a Command (C) - executes arbitrary commands with side effects.
/// </summary>
[NuruRoute("exec", Description = "Execute a command with arguments")]
public sealed class ExecCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Command and arguments to execute")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<ExecCommand, Unit>
  {
    public ValueTask<Unit> Handle(ExecCommand command, CancellationToken ct)
    {
      WriteLine($"Executing: {string.Join(" ", command.Args)}");
      return default;
    }
  }
}
