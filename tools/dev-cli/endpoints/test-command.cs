#region Purpose
// Dev CLI command for TimeWarp.Nuru development workflow
#endregion

// ═══════════════════════════════════════════════════════════════════════════════
// TEST COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Runs the fast CI test suite.
// Migrated from runfiles/test.cs to endpoints pattern.
// --phase multi runs only the multi-mode assembly; --phase standalone runs only
// the files that cannot run in it. CI runs the two as parallel jobs (task 538).

namespace DevCli;

/// <summary>
/// Run the CI test suite.
/// </summary>
[NuruRoute("test", Description = "Run the CI test suite")]
public sealed class TestCommand : ICommand<Unit>
{
  [Option("phase", "p", Description = "Test phase: all (default), multi, or standalone")]
  public string? Phase { get; set; }

  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(TestCommand command, CancellationToken ct)
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

      string phase = string.IsNullOrEmpty(command.Phase) ? "all" : command.Phase;
      string testRunner = phase switch
      {
        "all" or "multi" => Path.Combine(repoRoot, "tests", "ci-tests", "run-ci-tests.cs"),
        "standalone" => Path.Combine(repoRoot, "tests", "ci-standalone", "run-standalone-tests.cs"),
        _ => throw new ArgumentException($"Unknown test phase: {phase}. Valid options: all, multi, standalone")
      };

      if (!File.Exists(testRunner))
      {
        throw new FileNotFoundException($"Test runner not found: {testRunner}");
      }

      Terminal.WriteLine($"Running CI test suite (phase: {phase})...");
      Terminal.WriteLine($"Working from: {repoRoot}");

      // run-ci-tests.cs runs the standalone phase after the multi-mode phase
      // unless NURU_CI_PHASE=multi.
      int exitCode = await Shell.Builder("dotnet")
        .WithArguments(testRunner)
        .WithWorkingDirectory(repoRoot)
        .WithEnvironmentVariable("NURU_CI_PHASE", phase)
        .WithNoValidation()
        .RunAsync();

      if (exitCode != 0)
      {
        throw new InvalidOperationException($"Tests failed with exit code {exitCode}");
      }

      Terminal.WriteLine("\nTests completed successfully!");
      return Unit.Value;
    }
  }
}
