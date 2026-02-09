// ═══════════════════════════════════════════════════════════════════════════════
// COMPLEX EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════════
// Real-world patterns combining multiple features.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Git commit with message and options.
/// </summary>
[NuruRoute("git", Description = "Git commit with message and options")]
public sealed class GitFullCommand : ICommand<Unit>
{
  [Parameter(Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  [Option("amend", "a", Description = "Amend previous commit")]
  public bool Amend { get; set; }

  [Option("no-verify", "n", Description = "Bypass pre-commit hooks")]
  public bool NoVerify { get; set; }

  public sealed class Handler : ICommandHandler<GitFullCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitFullCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Commit: {command.Message} (amend: {command.Amend}, no-verify: {command.NoVerify})");
      return default;
    }
  }
}

/// <summary>
/// Run docker container with environment variables and ports.
//  For repeatable options, use array types without IsRepeatable property.
/// </summary>
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

/// <summary>
/// Kubectl get command with namespace and output format options.
/// </summary>
[NuruRoute("kubectl", Description = "Kubectl get command")]
public sealed class KubectlQuery : IQuery<Unit>
{
  [Parameter(Description = "Resource type")]
  public string Resource { get; set; } = string.Empty;

  [Option("namespace", "n", Description = "Target namespace")]
  public string? Ns { get; set; }

  [Option("output", "o", Description = "Output format")]
  public string? Format { get; set; }

  public sealed class Handler : IQueryHandler<KubectlQuery, Unit>
  {
    public ValueTask<Unit> Handle(KubectlQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Get {query.Resource} in namespace {query.Ns ?? "default"}");
      return default;
    }
  }
}
