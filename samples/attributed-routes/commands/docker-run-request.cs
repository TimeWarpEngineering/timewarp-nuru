namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Docker run command - inherits 'docker' prefix and --debug option.
/// This is a Command (C) - creates/starts a container.
/// </summary>
[NuruRoute("run", Description = "Run a Docker container")]
public sealed class DockerRunRequest : DockerRequestBase, ICommand<Unit>
{
  [Parameter(Description = "Image name to run")]
  public string Image { get; set; } = string.Empty;

  [Option("detach", "d", Description = "Run in background")]
  public bool Detach { get; set; }

  public sealed class Handler : ICommandHandler<DockerRunRequest, Unit>
  {
    public ValueTask<Unit> Handle(DockerRunRequest request, CancellationToken ct)
    {
      WriteLine($"Running container: {request.Image}");
      WriteLine($"  Debug: {request.Debug}");
      WriteLine($"  Detach: {request.Detach}");
      return default;
    }
  }
}
