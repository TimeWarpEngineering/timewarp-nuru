// ═══════════════════════════════════════════════════════════════════════════════
// FORMAT COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Check or fix code formatting using dotnet format.

namespace DevCli.Commands;

/// <summary>
/// Check or fix code formatting.
/// </summary>
[NuruRoute("format", Description = "Check or fix code formatting")]
internal sealed class FormatCommand : ICommand<Unit>
{
  [Option("fix", "f", Description = "Fix formatting issues instead of just checking")]
  public bool Fix { get; set; }

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler : ICommandHandler<FormatCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(FormatCommand command, CancellationToken ct)
    {
      // Get repo root using Git.FindRoot
      string? repoRoot = Git.FindRoot();

      if (repoRoot is null)
      {
        throw new InvalidOperationException("Could not find git repository root (.git not found)");
      }

      // Verify we're in the right place
      if (!File.Exists(Path.Combine(repoRoot, "timewarp-nuru.slnx")))
      {
        throw new InvalidOperationException("Could not find repository root (timewarp-nuru.slnx not found)");
      }

      string solutionPath = Path.Combine(repoRoot, "timewarp-nuru.slnx");

      if (command.Fix)
      {
        Terminal.WriteLine("Fixing code formatting...");
      }
      else
      {
        Terminal.WriteLine("Checking code formatting...");
      }

      List<string> args = ["format", solutionPath, "--severity", "warn", "--exclude", "**/benchmarks/**"];

      if (!command.Fix)
      {
        args.Add("--verify-no-changes");
      }

      CommandResult formatResult = Shell.Builder("dotnet")
        .WithArguments([.. args])
        .Build();

      if (command.Verbose)
      {
        Terminal.WriteLine(formatResult.ToCommandString());
      }

      int exitCode = await formatResult.RunAsync();

      if (exitCode != 0)
      {
        if (command.Fix)
        {
          throw new InvalidOperationException("Format failed!");
        }
        else
        {
          throw new InvalidOperationException("Code style violations found! Run 'dev format --fix' to fix them.");
        }
      }

      Terminal.WriteLine(command.Fix ? "✅ Formatting fixed!" : "✅ Code style check passed!");
      return Unit.Value;
    }
  }
}
