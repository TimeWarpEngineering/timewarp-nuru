namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Docker build command - inherits 'docker' prefix and --debug option.
/// This is a Command (C) - creates an image.
/// </summary>
[NuruRoute("build", Description = "Build a Docker image")]
public sealed class DockerBuildRequest : DockerRequestBase, ICommand<Unit>
{
  [Parameter(Description = "Path to Dockerfile directory")]
  public string Path { get; set; } = ".";

  [Option("tag", "t", Description = "Tag for the image")]
  public string? Tag { get; set; }

  public sealed class Handler : ICommandHandler<DockerBuildRequest, Unit>
  {
    public ValueTask<Unit> Handle(DockerBuildRequest request, CancellationToken ct)
    {
      WriteLine($"Building image from: {request.Path}");
      WriteLine($"  Debug: {request.Debug}");
      WriteLine($"  Tag: {request.Tag ?? "(none)"}");
      return default;
    }
  }
}
