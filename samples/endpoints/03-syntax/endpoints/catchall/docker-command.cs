// ═══════════════════════════════════════════════════════════════════════════════
// DOCKER COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Run docker command with arbitrary arguments using catch-all parameter.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("docker", Description = "Run docker command with arbitrary arguments")]
public sealed class DockerCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Docker arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : IRequestHandler<DockerCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Docker args: {string.Join(" ", command.Args)}");
      return Unit.ValueTask;
    }
  }
}
