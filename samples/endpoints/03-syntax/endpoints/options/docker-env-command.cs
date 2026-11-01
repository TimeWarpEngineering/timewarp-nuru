// ═══════════════════════════════════════════════════════════════════════════════
// DOCKER-ENV COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Docker command with environment variables using array options.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("docker-env", Description = "Docker command with environment variables")]
public sealed class DockerEnvCommand : ICommand<Unit>
{
  [Option("env", "e", Description = "Environment variables")]
  public string[] Var { get; set; } = [];

  public sealed class Handler : IRequestHandler<DockerEnvCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerEnvCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Environment variables: {string.Join(", ", command.Var)}");
      return Unit.ValueTask;
    }
  }
}
