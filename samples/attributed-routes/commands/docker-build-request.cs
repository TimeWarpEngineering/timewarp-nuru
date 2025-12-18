namespace AttributedRoutes.Commands;

using TimeWarp.Nuru;
using Mediator;
using static System.Console;

/// <summary>
/// Docker build command - inherits 'docker' prefix and --debug option.
/// </summary>
[NuruRoute("build", Description = "Build a Docker image")]
public sealed class DockerBuildRequest : DockerRequestBase, IRequest
{
  [Parameter(Description = "Path to Dockerfile directory")]
  public string Path { get; set; } = ".";

  [Option("tag", "t", Description = "Tag for the image")]
  public string? Tag { get; set; }

  public sealed class Handler : IRequestHandler<DockerBuildRequest>
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
