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
// --coverage (on by default under GitHub Actions through `dev workflow`) asks
// Jaribu's runfile coverage mode for a Cobertura report (task 574).

#region Design
// Coverage is Jaribu's job (timewarp-jaribu 060): the runner's RunAllTests
// relaunches itself under dotnet-coverage when JARIBU_COVERAGE=1. This command
// only sets the environment. The module filter replaces Jaribu's default
// excludes with one rule that drops every assembly whose file name does not
// start with TimeWarp.Nuru, so the multi-mode runner assembly (which compiles
// every test file) and the per-file standalone runners stay out of the line rate.
#endregion

namespace DevCli;

/// <summary>
/// Run the CI test suite.
/// </summary>
[NuruRoute("test", Description = "Run the CI test suite")]
public sealed class TestCommand : ICommand<Unit>
{
  [Option("phase", "p", Description = "Test phase: all (default), multi, or standalone")]
  public string? Phase { get; set; }

  [Option("coverage", Description = "Collect a Cobertura report in artifacts/coverage through Jaribu runfile coverage (needs dotnet-coverage)")]
  public bool Coverage { get; set; }

  /// <summary>Directory, relative to the repository root, that receives coverage.cobertura.xml.</summary>
  internal const string CoverageResultsDirectory = "artifacts/coverage";

  /// <summary>
  /// Jaribu module-exclude regex: every assembly whose file name does not start with TimeWarp.Nuru.
  /// </summary>
  internal const string CoverageModuleFilter = @"(?i)[/\\](?!timewarp\.nuru)[^/\\]*\.(dll|exe)$";

  internal sealed class Handler : IRequestHandler<TestCommand, Unit>
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
      ShellBuilder runner = Shell.Builder("dotnet")
        .WithArguments(testRunner)
        .WithWorkingDirectory(repoRoot)
        .WithEnvironmentVariable("NURU_CI_PHASE", phase)
        .WithNoValidation();

      if (command.Coverage)
      {
        string resultsDirectory = Path.Combine(repoRoot, CoverageResultsDirectory);
        Terminal.WriteLine($"Coverage: Cobertura report in {resultsDirectory}");
        runner = runner
          .WithEnvironmentVariable(RunfileCoverageVariables.Enable, "1")
          .WithEnvironmentVariable(RunfileCoverageVariables.Results, resultsDirectory)
          .WithEnvironmentVariable(RunfileCoverageVariables.ModuleFilter, CoverageModuleFilter);
      }

      int exitCode = await runner.RunAsync();

      if (exitCode != 0)
      {
        throw new InvalidOperationException($"Tests failed with exit code {exitCode}");
      }

      Terminal.WriteLine("\nTests completed successfully!");
      return Unit.Value;
    }
  }
}

/// <summary>
/// Environment variables read by Jaribu's runfile coverage mode
/// (<c>TimeWarp.Jaribu.RunfileCoverageOptions</c>; the dev CLI does not reference Jaribu).
/// </summary>
internal static class RunfileCoverageVariables
{
  internal const string Enable = "JARIBU_COVERAGE";
  internal const string Results = "JARIBU_COVERAGE_RESULTS";
  internal const string ModuleFilter = "JARIBU_COVERAGE_MODULE_FILTER";
}
