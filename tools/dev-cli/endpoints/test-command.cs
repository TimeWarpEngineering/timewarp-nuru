// ═══════════════════════════════════════════════════════════════════════════════
// TEST COMMAND
// ═══════════════════════════════════════════════════════════════════════════════
// Runs the fast CI test suite.
// Migrated from runfiles/test.cs to endpoints pattern.

namespace DevCli.Commands;

/// <summary>
/// Run the CI test suite.
/// </summary>
[NuruRoute("test", Description = "Run the CI test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TestCommand command, CancellationToken ct)
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

      string testRunner = Path.Combine(repoRoot, "tests", "ci-tests", "run-ci-tests.cs");

      if (!File.Exists(testRunner))
      {
        throw new FileNotFoundException($"Test runner not found: {testRunner}");
      }

      Terminal.WriteLine("Running CI test suite...");
      Terminal.WriteLine($"Working from: {repoRoot}");

      // Run the CI test suite
      int exitCode = await Shell.Builder("dotnet")
        .WithArguments(testRunner)
        .WithWorkingDirectory(repoRoot)
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
