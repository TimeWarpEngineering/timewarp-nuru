namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Idempotent Command: docker tag {source} {target}
/// Tags a Docker image. Safe to retry - applying the same tag multiple times has the same effect.
/// This is an Idempotent Command (I) - mutating but safe to retry.
/// Demonstrates ITerminal injection for testable output.
/// </summary>
[NuruRoute("tag", Description = "Create a tag TARGET_IMAGE that refers to SOURCE_IMAGE")]
public sealed class DockerTagCommand : DockerGroupBase, IIdempotentCommand<Unit>
{
  [Parameter(Order = 0, Description = "Source image name or ID")]
  public string Source { get; set; } = string.Empty;

  [Parameter(Order = 1, Description = "Target image name with optional tag")]
  public string Target { get; set; } = string.Empty;

  public sealed class Handler : IIdempotentCommandHandler<DockerTagCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(DockerTagCommand command, CancellationToken ct)
    {
      Terminal.WriteLine($"Tagging {command.Source} as {command.Target}");
      return default;
    }
  }
}
