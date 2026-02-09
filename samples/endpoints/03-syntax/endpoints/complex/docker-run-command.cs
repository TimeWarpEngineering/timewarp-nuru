// ═══════════════════════════════════════════════════════════════════════════════
// DOCKER-RUN COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Run docker container with environment variables and ports.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("docker-run", Description = "Run docker container")]
public sealed class DockerRunCommand : ICommand<Unit>
{
  [Option("env", "e", Description = "Environment variables")]
  public string[] E { get; set; } = [];

  [Option("port", "p", Description = "Port mappings")]
  public string[] Port { get; set; } = [];

  [Parameter(Description = "Container image")]
  public string Image { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Container command")]
  public string[] Cmd { get; set; } = [];

  public sealed class Handler : ICommandHandler<DockerRunCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerRunCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Running {command.Image} with {command.E.Length} env vars, {command.Port.Length} ports");
      return default;
    }
  }
}
