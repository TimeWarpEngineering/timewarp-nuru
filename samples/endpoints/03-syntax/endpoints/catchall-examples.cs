// ═══════════════════════════════════════════════════════════════════════════════
// CATCH-ALL PARAMETER EXAMPLES
// ═══════════════════════════════════════════════════════════════════════════════
// Capture all remaining arguments using [Parameter(IsCatchAll = true)].

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

/// <summary>
/// Run docker command with arbitrary arguments.
/// </summary>
[NuruRoute("docker", Description = "Run docker command with arbitrary arguments")]
public sealed class DockerCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Docker arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<DockerCommand, Unit>
  {
    public ValueTask<Unit> Handle(DockerCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Docker args: {string.Join(" ", command.Args)}");
      return default;
    }
  }
}

/// <summary>
/// Run a script with parameters.
/// </summary>
[NuruRoute("run", Description = "Run a script with parameters")]
public sealed class RunCommand : ICommand<Unit>
{
  [Parameter(Description = "Script to run")]
  public string Script { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Script parameters")]
  public string[] Params { get; set; } = [];

  public sealed class Handler : ICommandHandler<RunCommand, Unit>
  {
    public ValueTask<Unit> Handle(RunCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Running {command.Script} with params: {string.Join(" ", command.Params)}");
      return default;
    }
  }
}

/// <summary>
/// Tail a file with optional line count.
/// </summary>
[NuruRoute("tail", Description = "Tail a file")]
public sealed class TailCommand : ICommand<Unit>
{
  [Parameter(Description = "File to tail")]
  public string File { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Additional tail arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<TailCommand, Unit>
  {
    public ValueTask<Unit> Handle(TailCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Tailing {command.File} with {command.Args.Length} extra args");
      return default;
    }
  }
}

/// <summary>
/// Execute a command.
/// </summary>
[NuruRoute("exec", Description = "Execute a command")]
public sealed class ExecCommand : ICommand<Unit>
{
  [Parameter(Description = "Command to execute")]
  public string Command { get; set; } = string.Empty;

  [Parameter(IsCatchAll = true, Description = "Command arguments")]
  public string[] Args { get; set; } = [];

  public sealed class Handler : ICommandHandler<ExecCommand, Unit>
  {
    public ValueTask<Unit> Handle(ExecCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Executing {command.Command} {string.Join(" ", command.Args)}");
      return default;
    }
  }
}
