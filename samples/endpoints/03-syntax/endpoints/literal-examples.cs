// ═══════════════════════════════════════════════════════════════════════════════
// LITERAL EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════════
// Plain text segments that must match exactly.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Simple status check endpoint.
/// </summary>
[NuruRoute("status", Description = "Check system status")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery query, CancellationToken ct)
    {
      Console.WriteLine("OK");
      return default;
    }
  }
}

/// <summary>
/// Git commit endpoint - demonstrates hyphenated literal.
/// </summary>
[NuruRoute("git-commit", Description = "Commit changes")]
public sealed class GitCommitCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<GitCommitCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitCommitCommand command, CancellationToken ct)
    {
      Console.WriteLine("Committing...");
      return default;
    }
  }
}

/// <summary>
/// Version endpoint.
/// </summary>
[NuruRoute("version", Description = "Show version information")]
public sealed class VersionQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<VersionQuery, string>
  {
    public ValueTask<string> Handle(VersionQuery query, CancellationToken ct)
    {
      return new ValueTask<string>("1.0.0");
    }
  }
}
