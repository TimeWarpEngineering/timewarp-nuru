namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Command: docker run {image} [--detach]
/// Runs a Docker container from an image.
/// This is a Command (C) - mutating operation.
/// </summary>
[NuruRoute("run", Description = "Run a container from an image")]
public sealed class DockerRunCommand : DockerGroupBase, ICommand<Unit>
{
  [Parameter(Description = "Docker image to run")]
  public string Image { get; set; } = string.Empty;

  [Option("detach", "d", Description = "Run container in background")]
  public bool Detach { get; set; }

  public sealed class Handler : ICommandHandler<DockerRunCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerRunCommand command, CancellationToken ct)
    {
      string mode = command.Detach ? " (detached)" : "";
      WriteLine($"Running container from image: {command.Image}{mode}");
      return default;
    }
  }
}
