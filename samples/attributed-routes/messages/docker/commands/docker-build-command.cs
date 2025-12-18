namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Command: docker build {path} [--tag {tag}] [--no-cache]
/// Builds a Docker image from a Dockerfile.
/// This is a Command (C) - mutating operation.
/// </summary>
[NuruRoute("build", Description = "Build an image from a Dockerfile")]
public sealed class DockerBuildCommand : DockerGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Path to Dockerfile or build context")]
  public string Path { get; set; } = string.Empty;

  [Option("tag", "t", Description = "Name and optionally a tag in 'name:tag' format")]
  public string? Tag { get; set; }

  [Option("no-cache", null, Description = "Do not use cache when building")]
  public bool NoCache { get; set; }

  public sealed class Handler : ICommandHandler<DockerBuildCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerBuildCommand command, CancellationToken ct)
    {
      string tagInfo = command.Tag != null ? $" -t {command.Tag}" : "";
      string cacheInfo = command.NoCache ? " --no-cache" : "";
      WriteLine($"Building image from: {command.Path}{tagInfo}{cacheInfo}");
      return default;
    }
  }
}
