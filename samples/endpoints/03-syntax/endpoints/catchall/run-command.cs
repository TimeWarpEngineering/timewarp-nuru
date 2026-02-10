// ═══════════════════════════════════════════════════════════════════════════════
// RUN COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Run a script with parameters using catch-all for script arguments.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("run", Description = "Run a script with parameters")]
public sealed class RunCommand : ICommand<Unit>
{
  [Parameter(Description = "Script to run")]
  public string Script { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Script parameters")]
  public string[] Params { get; set; } = [];

  public sealed class Handler : ICommandHandler<RunCommand, Unit>
  {
    public ValueTask<Unit> Handle(RunCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Running {command.Script} with params: {string.Join(" ", command.Params)}");
      return default;
    }
  }
}
