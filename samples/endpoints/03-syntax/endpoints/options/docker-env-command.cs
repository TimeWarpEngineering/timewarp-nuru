// ═══════════════════════════════════════════════════════════════════════════════
// DOCKER-ENV COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Docker command with environment variables using array options.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("docker-env", Description = "Docker command with environment variables")]
public sealed class DockerEnvCommand : ICommand<Unit>
{
  [Option("env", "e", Description = "Environment variables")]
  public string[] Var { get; set; } = [];

  public sealed class Handler : ICommandHandler<DockerEnvCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerEnvCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Environment variables: {string.Join(", ", command.Var)}");
      return default;
    }
  }
}
